# PDF to JPG Converter

A clean, fast Windows desktop app that converts PDF files to JPEG images. Built with WPF on .NET 8, powered by the PDFium rendering engine.

![App Icon](Icon%20PDF2JPG.png)

---

## Features

- **Drag & drop** PDF files directly onto the app, or use the file picker
- **Batch conversion** — add multiple PDFs and convert them all at once
- **High-quality output** — configurable DPI (72–1200) and JPEG quality (10–100%)
- **Color modes** — Color, Grayscale, or Black & White output
- **Landscape rotation** — automatically rotates wide pages 90° CCW to portrait
- **Flexible output location** — save to a custom folder or next to each source PDF
- **Subfolder per PDF** — optionally group pages into a named subfolder
- **Dark / Light theme** — toggle with a single click
- **Frameless modern UI** — rounded corners, minimal chrome

---

## Output file naming

Pages are saved as `<PDFName>_001.jpg`, `<PDFName>_002.jpg`, etc. in the chosen output directory.

---

## Requirements

- Windows 10 or later (x64)
- No installation required when using the portable build

---

## Installation

Download the latest installer from the [Releases](../../releases) page and run `PdfToJpgConverter_Setup_v*.exe`.

The installer:
- Places the app in `%LocalAppData%\PDF to JPG Converter` (no admin rights needed)
- Optionally creates a desktop shortcut
- Includes an uninstaller

---

## Building from source

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained publish (win-x64)
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

The PDFium native DLLs (`x64/pdfium.dll`, `x86/pdfium.dll`, `icudt.dll`) are pulled from the `PDFium.Net.SDK` NuGet package and copied automatically during build.

---

## Settings

Open the settings panel via the gear icon (top-right). Available options:

| Setting | Description |
|---|---|
| Color mode | Color / Grayscale / B&W output |
| Output DPI | Resolution of exported images (default 300) |
| JPEG Quality | Compression quality, 10–100% (default 95%) |
| Rotate landscape | Auto-rotates wide pages to portrait |
| Create subfolder per PDF | Groups pages into a named subfolder |
| Default export folder | Folder used when "Custom folder" mode is active |

---

## Tech stack

| Component | Details |
|---|---|
| UI framework | WPF (.NET 8, `net8.0-windows`) |
| PDF rendering | [PDFium.Net.SDK](https://www.nuget.org/packages/PDFium.Net.SDK/) (Patagames, v4.6.x) |
| Image processing | `System.Drawing.Common` |
| Installer | [Inno Setup](https://jrsoftware.org/isinfo.php) |

---

## License

This project is provided as-is for personal use. The PDFium library is subject to its own [BSD-style license](https://pdfium.googlesource.com/pdfium/+/refs/heads/main/LICENSE).

---

*PDF to JPG Converter v1.1 — by HMZ*
