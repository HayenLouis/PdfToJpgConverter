# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build commands

```bash
dotnet build                  # Debug build
dotnet build -c Release       # Release build
dotnet run                    # Run (Debug)
dotnet run -c Release         # Run (Release)
```

To scaffold a fresh WPF project use `dotnet new wpf --framework net8.0` — the framework flag does **not** accept `net8.0-windows`; the TFM is set to `net8.0-windows` automatically in the generated `.csproj`.

There are no tests in this project.

## Architecture

Single-window WPF app (.NET 8, `net8.0-windows`). All logic lives in two file pairs:

- **`App.xaml` / `App.xaml.cs`** — application entry point; owns the dynamic resource palette. `App.ApplyDarkTheme()` and `App.ApplyLightTheme()` overwrite `SolidColorBrush` entries in `Application.Current.Resources` at runtime — all UI elements bind to these via `{DynamicResource}`.
- **`MainWindow.xaml` / `MainWindow.xaml.cs`** — the entire UI and conversion pipeline.

### Conversion pipeline (`MainWindow.xaml.cs`)

`ConvertBtn_Click` is `async void`; the heavy work runs on `Task.Run`. Flow:

1. First pass over all files to count total pages (drives the progress bar maximum).
2. Second pass: for each file, open `PdfDocument`, iterate pages, call `RenderPageToJpeg`, report progress via `IProgress<(int done, int total, string msg)>`.

`RenderPageToJpeg` (static):
- `page.Width` / `page.Height` are in **points** (1/72 inch). Convert to pixels: `(int)Math.Round(points / 72.0 * dpi)`.
- Checks `page.Width > page.Height` to detect landscape. If landscape, swap pixel dimensions and use `PageRotate.Rotate270` (= 90° CCW). Portrait pages use `PageRotate.Normal`.
- Fills a `PdfBitmap` with `FS_COLOR.White`, renders via `page.Render(...)`, casts `pdfBmp.Image` to `System.Drawing.Bitmap`, saves as JPEG at quality 95 via `ImageCodecInfo`.
- Render flags used: `RenderFlags.FPDF_LCD_TEXT | RenderFlags.FPDF_PRINTING`.

Output filename pattern: `<pdfname>_page_XXXX.jpg` written **flat** into the target directory (no subdirectory is created).

### Output location

Two modes toggled by `_saveNextToSource` (bool field), driven by the segmented pill control (`SegCustom` / `SegNextTo` Borders) in the UI. `ApplySegmentState()` updates both the visual state of the pill and the visibility of the folder-picker row. The active segment gets `AccentBrush` background; the inactive one gets `Transparent`.

- **Custom folder** — `_outputFolder`, default is `MyPictures`, picked via `Microsoft.Win32.OpenFolderDialog` (available natively in .NET 8 WPF — no WinForms reference needed).
- **Next to PDF** — `Path.GetDirectoryName(file)` for each source file individually.

### PDFium.Net.SDK — correct API (Patagames, v4.6.2704)

The package ID is `PDFium.Net.SDK` (Patagames). Key namespaces and types confirmed by DLL inspection:

| Type | Namespace |
|---|---|
| `PdfCommon` | `Patagames.Pdf` |
| `FS_COLOR` | `Patagames.Pdf` |
| `PdfDocument`, `PdfPage`, `PdfBitmap` | `Patagames.Pdf.Net` |
| `PageRotate`, `RenderFlags` | `Patagames.Pdf.Enums` |

Critical API details:
- **Color**: use `FS_COLOR.White` (static property). The struct `FS_RGBA_STRUCT` does **not** exist in this package — using it causes a compile error.
- **`PdfBitmap.FillRect`** signature: `FillRect(int left, int top, int width, int height, FS_COLOR color)`.
- **`PdfBitmap.Image`** returns `System.Drawing.Image`, **not** `Bitmap` — an explicit cast `(Bitmap)pdfBmp.Image` is required before calling `Bitmap.Save(...)`.
- **`PageRotate` enum**: `Rotate90` = 90° clockwise, `Rotate270` = 90° counterclockwise.
- **`page.Render`** signature: `Render(PdfBitmap bitmap, int startX, int startY, int sizeX, int sizeY, PageRotate rotate, RenderFlags flags)`.
- `PdfCommon.Initialize()` must be called once before any PDFium type is used (called in the `MainWindow` constructor).
- License: `PdfCommon.SetLicense(...)` must be called **before** `Initialize()`. Without a license key, rendered images carry a trial watermark.

### PDFium native DLLs

`PDFium.Net.SDK` targets .NET Framework, so its `content/` folder is **not** auto-copied by the .NET 8 build system. The `.csproj` has explicit `<Content>` items that copy `x64/pdfium.dll`, `x86/pdfium.dll`, and `icudt.dll` to the output directory. The paths are hardcoded to version `4.6.2704` — if the package version is bumped these must be updated:

```xml
<Content Include="$(NuGetPackageRoot)pdfium.net.sdk\4.6.2704\content\x64\pdfium.dll">
  <Link>x64\pdfium.dll</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### JPEG encoding with quality control

`System.Drawing.Bitmap.Save(path, ImageFormat.Jpeg)` uses a low default quality. Always use the `ImageCodecInfo` overload:

```csharp
var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
using var ep = new EncoderParameters(1);
ep.Param[0] = new EncoderParameter(Encoder.Quality, 95L);  // note: long, not int
bmp.Save(path, codec, ep);
```

### Theme system

All colors are `{DynamicResource}` references to keys defined in `App.xaml` (e.g. `AccentBrush`, `WindowBg`, `TextPrimary`, `TextSecondary`, `TextMuted`, `BorderBrush`, `ProgressBg`, etc.). `App.ApplyDarkTheme()` / `App.ApplyLightTheme()` replace the brush objects in-place; the UI updates automatically. Always use `{DynamicResource}` for any new styled element — never hardcode colors.

### WPF compatibility notes

- `StackPanel.Spacing` does **not** exist in WPF (it is WinUI 3 only) — use `Margin` on child elements instead.
- The window is frameless (`WindowStyle="None"`, `AllowsTransparency="True"`); dragging is handled by `TitleBar_MouseDown` calling `DragMove()`. Rounded corners come from the root `Border` with `CornerRadius="14"`.
- Custom control templates are used for all buttons (`AccentButton`, `GhostButton`, `OutlineButton`, `TitleBarBtn`, `CloseBtn`) and for the scrollbar/scrollviewer to achieve the minimal thin-track look.
- Dashed borders on the drop zone are implemented via a tiled `DrawingBrush` on `Border.BorderBrush` — WPF `Border` does not support `BorderDashStyle` natively.
- The progress bar is a manual `Width`-animated `Border` inside a track `Border`, not a WPF `ProgressBar` control. Track width is captured in `_trackWidth` via a `SizeChanged` event handler (`ProgressTrack_SizeChanged`).
