using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Patagames.Pdf;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;

namespace PdfToJpgConverter;

public enum ColorMode { Color, Grayscale, BlackAndWhite }

// ── Persistent settings ───────────────────────────────────────────────────────
internal class AppSettings
{
    public bool   IsDark          { get; set; } = true;
    public int    Dpi             { get; set; } = 300;
    public long   Quality         { get; set; } = 95;
    public bool   RotateLandscape { get; set; } = true;
    public bool   SubfolderPerPdf { get; set; } = false;
    public string ColorMode       { get; set; } = "Color";
    public string OutputFolder    { get; set; } = "";
    public bool   SaveNextToSource{ get; set; } = false;
}

// ── Model ────────────────────────────────────────────────────────────────────
public record PdfFileEntry(string FullPath)
{
    public string FileName  => Path.GetFileName(FullPath);
    public string Directory => Path.GetDirectoryName(FullPath) ?? string.Empty;
}

// ── Main window ───────────────────────────────────────────────────────────────
public partial class MainWindow : Window
{
    // Settings persistence
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PdfToJpgConverter");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    // State
    private readonly ObservableCollection<PdfFileEntry> _files = [];
    private string _outputFolder     = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    private bool   _isDark           = true;
    private bool   _converting       = false;
    private bool   _saveNextToSource = false;
    private double _trackWidth       = 0;

    // Settings
    private int       _dpi             = 300;
    private long      _quality         = 95;
    private bool      _rotateLandscape = true;
    private bool      _subfolderPerPdf = false;
    private ColorMode _colorMode       = ColorMode.Color;
    private bool      _settingsOpen    = false;

    public MainWindow()
    {
        InitializeComponent();
        PdfCommon.Initialize();

        LoadSettings();

        FileListBox.ItemsSource = _files;
        OutputPathText.Text     = _outputFolder;
        ApplySegmentState();
        InitSettings();
    }

    // ── Settings persistence ───────────────────────────────────────────────
    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return;
            var json = File.ReadAllText(SettingsFile);
            var s    = JsonSerializer.Deserialize<AppSettings>(json);
            if (s is null) return;

            _isDark           = s.IsDark;
            _dpi              = s.Dpi;
            _quality          = s.Quality;
            _rotateLandscape  = s.RotateLandscape;
            _subfolderPerPdf  = s.SubfolderPerPdf;
            _saveNextToSource = s.SaveNextToSource;
            _colorMode        = Enum.TryParse<ColorMode>(s.ColorMode, out var cm) ? cm : ColorMode.Color;

            if (!string.IsNullOrWhiteSpace(s.OutputFolder) && Directory.Exists(s.OutputFolder))
                _outputFolder = s.OutputFolder;

            if (_isDark) App.ApplyDarkTheme(); else App.ApplyLightTheme();
        }
        catch { /* ignore corrupt settings */ }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var s = new AppSettings
            {
                IsDark           = _isDark,
                Dpi              = _dpi,
                Quality          = _quality,
                RotateLandscape  = _rotateLandscape,
                SubfolderPerPdf  = _subfolderPerPdf,
                ColorMode        = _colorMode.ToString(),
                OutputFolder     = _outputFolder,
                SaveNextToSource = _saveNextToSource,
            };
            File.WriteAllText(SettingsFile,
                JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* ignore write failures */ }
    }

    // ── Settings init ──────────────────────────────────────────────────────
    private void InitSettings()
    {
        DpiBox.Text         = _dpi.ToString();
        QualitySlider.Value = _quality;
        UpdateQualityLabel();
        SettingsOutputPath.Text = _outputFolder;

        ApplyToggle(RotateToggleBorder,    RotateThumb,    _rotateLandscape);
        ApplyToggle(SubfolderToggleBorder, SubfolderThumb, _subfolderPerPdf);
        ApplyToggle(ThemeToggleBorder,     ThemeThumb,     _isDark);
        ApplyColorModeState();
    }

    // ── Toggle helpers ─────────────────────────────────────────────────────
    private void ApplyToggle(System.Windows.Controls.Border border,
                              System.Windows.Controls.Border thumb, bool on)
    {
        if (on)
        {
            border.Background         = (System.Windows.Media.Brush)Application.Current.Resources["AccentBrush"];
            thumb.HorizontalAlignment = HorizontalAlignment.Right;
            thumb.Margin              = new Thickness(0, 0, 3, 0);
        }
        else
        {
            border.Background         = (System.Windows.Media.Brush)Application.Current.Resources["BorderBrush"];
            thumb.HorizontalAlignment = HorizontalAlignment.Left;
            thumb.Margin              = new Thickness(3, 0, 0, 0);
        }
    }

    // ── Color mode ─────────────────────────────────────────────────────────
    private void ApplyColorModeState()
    {
        var accent     = (System.Windows.Media.Brush)Application.Current.Resources["AccentBrush"];
        var secondary  = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"];
        var transparent= System.Windows.Media.Brushes.Transparent;

        CmColorSeg.Background = _colorMode == ColorMode.Color          ? accent : transparent;
        CmColorText.Foreground = _colorMode == ColorMode.Color          ? System.Windows.Media.Brushes.White : secondary;
        CmGraySeg.Background   = _colorMode == ColorMode.Grayscale      ? accent : transparent;
        CmGrayText.Foreground  = _colorMode == ColorMode.Grayscale      ? System.Windows.Media.Brushes.White : secondary;
        CmBWSeg.Background     = _colorMode == ColorMode.BlackAndWhite  ? accent : transparent;
        CmBWText.Foreground    = _colorMode == ColorMode.BlackAndWhite  ? System.Windows.Media.Brushes.White : secondary;
    }

    private void ColorModeColor_Click(object sender, MouseButtonEventArgs e)
    { _colorMode = ColorMode.Color;         ApplyColorModeState(); SaveSettings(); }

    private void ColorModeGray_Click(object sender, MouseButtonEventArgs e)
    { _colorMode = ColorMode.Grayscale;     ApplyColorModeState(); SaveSettings(); }

    private void ColorModeBW_Click(object sender, MouseButtonEventArgs e)
    { _colorMode = ColorMode.BlackAndWhite; ApplyColorModeState(); SaveSettings(); }

    // ── DPI ────────────────────────────────────────────────────────────────
    private void DpiBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void DpiBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(DpiBox.Text, out int val) && val >= 36 && val <= 1200)
            _dpi = val;
        else
        {
            _dpi = 300;
            DpiBox.Text = "300";
        }
        SaveSettings();
    }

    private void DpiPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int val))
        {
            _dpi = val;
            DpiBox.Text = tag;
            SaveSettings();
        }
    }

    // ── Quality slider ─────────────────────────────────────────────────────
    private void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _quality = (long)e.NewValue;
        if (QualityLabel is not null) UpdateQualityLabel();
        SaveSettings();
    }

    private void UpdateQualityLabel() =>
        QualityLabel.Text = $"{(int)QualitySlider.Value}%";

    // ── Rotate / Subfolder toggles ─────────────────────────────────────────
    private void RotateToggle_Click(object sender, MouseButtonEventArgs e)
    {
        _rotateLandscape = !_rotateLandscape;
        ApplyToggle(RotateToggleBorder, RotateThumb, _rotateLandscape);
        SaveSettings();
    }

    private void SubfolderToggle_Click(object sender, MouseButtonEventArgs e)
    {
        _subfolderPerPdf = !_subfolderPerPdf;
        ApplyToggle(SubfolderToggleBorder, SubfolderThumb, _subfolderPerPdf);
        SaveSettings();
    }

    // ── Settings browse (default export folder) ────────────────────────────
    private void SettingsBrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title            = "Select default export folder",
            InitialDirectory = _outputFolder
        };
        if (dlg.ShowDialog() == true)
        {
            _outputFolder           = dlg.FolderName;
            SettingsOutputPath.Text = _outputFolder;
            OutputPathText.Text     = _outputFolder;
            SaveSettings();
        }
    }

    // ── Settings panel toggle ──────────────────────────────────────────────
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        _settingsOpen            = !_settingsOpen;
        MainScroll.Visibility    = _settingsOpen ? Visibility.Collapsed : Visibility.Visible;
        SettingsScroll.Visibility= _settingsOpen ? Visibility.Visible   : Visibility.Collapsed;
    }

    // ── Title bar ─────────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // ── Theme toggle ──────────────────────────────────────────────────────
    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        if (_isDark) App.ApplyDarkTheme(); else App.ApplyLightTheme();

        ApplyToggle(ThemeToggleBorder,     ThemeThumb,     _isDark);
        ApplyToggle(RotateToggleBorder,    RotateThumb,    _rotateLandscape);
        ApplyToggle(SubfolderToggleBorder, SubfolderThumb, _subfolderPerPdf);
        ApplyColorModeState();
        ApplySegmentState();
        SaveSettings();
    }

    // ── Drop zone ─────────────────────────────────────────────────────────
    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        bool valid = e.Data.GetDataPresent(DataFormats.FileDrop) && GetDroppedPdfs(e).Any();
        e.Effects = valid ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
        if (valid) DropZone.BorderThickness = new Thickness(2);
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e) =>
        DropZone.BorderThickness = new Thickness(1);

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        DropZone.BorderThickness = new Thickness(1);
        foreach (var path in GetDroppedPdfs(e)) AddFile(path);
    }

    private void DropZone_Click(object sender, MouseButtonEventArgs e)
    {
        if (_converting) return;
        var dlg = new OpenFileDialog
        {
            Title       = "Select PDF files",
            Filter      = "PDF files (*.pdf)|*.pdf",
            Multiselect = true
        };
        if (dlg.ShowDialog() == true)
            foreach (var f in dlg.FileNames) AddFile(f);
    }

    private static IEnumerable<string> GetDroppedPdfs(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) yield break;
        foreach (var f in (string[])e.Data.GetData(DataFormats.FileDrop))
            if (f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) yield return f;
    }

    // ── File list helpers ─────────────────────────────────────────────────
    private void AddFile(string path)
    {
        if (_files.Any(f => f.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))) return;
        _files.Add(new PdfFileEntry(path));
        RefreshListUI();
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string path) return;
        var entry = _files.FirstOrDefault(f => f.FullPath == path);
        if (entry is not null) { _files.Remove(entry); RefreshListUI(); }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        _files.Clear();
        RefreshListUI();
    }

    private void RefreshListUI()
    {
        bool has = _files.Count > 0;
        FileListBorder.Visibility = has ? Visibility.Visible   : Visibility.Collapsed;
        EmptyHint.Visibility      = has ? Visibility.Collapsed : Visibility.Visible;
        ClearAllBtn.Visibility    = has ? Visibility.Visible   : Visibility.Collapsed;
        FileBadge.Visibility      = has ? Visibility.Visible   : Visibility.Collapsed;
        FileBadgeText.Text        = _files.Count.ToString();
    }

    // ── Output folder (main screen) ────────────────────────────────────────
    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title            = "Select output folder",
            InitialDirectory = _outputFolder
        };
        if (dlg.ShowDialog() == true)
        {
            _outputFolder           = dlg.FolderName;
            OutputPathText.Text     = _outputFolder;
            SettingsOutputPath.Text = _outputFolder;
            SaveSettings();
        }
    }

    // ── Output location segments ──────────────────────────────────────────
    private void SegCustom_Click(object sender, MouseButtonEventArgs e)
    { _saveNextToSource = false; ApplySegmentState(); SaveSettings(); }

    private void SegNextTo_Click(object sender, MouseButtonEventArgs e)
    { _saveNextToSource = true;  ApplySegmentState(); SaveSettings(); }

    private void ApplySegmentState()
    {
        var accent      = (System.Windows.Media.Brush)Application.Current.Resources["AccentBrush"];
        var secondary   = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"];
        var transparent = System.Windows.Media.Brushes.Transparent;

        if (!_saveNextToSource)
        {
            SegCustom.Background      = accent;
            SegCustomText.Foreground  = System.Windows.Media.Brushes.White;
            SegNextTo.Background      = transparent;
            SegNextToText.Foreground  = secondary;
            CustomFolderRow.Visibility= Visibility.Visible;
            NextToPdfHint.Visibility  = Visibility.Collapsed;
        }
        else
        {
            SegNextTo.Background      = accent;
            SegNextToText.Foreground  = System.Windows.Media.Brushes.White;
            SegCustom.Background      = transparent;
            SegCustomText.Foreground  = secondary;
            CustomFolderRow.Visibility= Visibility.Collapsed;
            NextToPdfHint.Visibility  = Visibility.Visible;
        }
    }

    // ── Progress track resize ─────────────────────────────────────────────
    private void ProgressTrack_SizeChanged(object sender, SizeChangedEventArgs e) =>
        _trackWidth = e.NewSize.Width;

    // ── Convert ───────────────────────────────────────────────────────────
    private async void ConvertBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
        {
            MessageBox.Show("Add at least one PDF file before converting.",
                "No files", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetConvertingState(true);

        var files         = _files.Select(f => f.FullPath).ToList();
        var outputFolder  = _outputFolder;
        bool nextToSource = _saveNextToSource;
        bool subfolder    = _subfolderPerPdf;
        int  dpi          = _dpi;
        long quality      = _quality;
        bool rotate       = _rotateLandscape;
        var  colorMode    = _colorMode;
        int total = 0, done = 0;

        var progress = new Progress<(int done, int total, string msg)>(p =>
            UpdateProgress(p.done, p.total, p.msg));

        try
        {
            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    using var doc = PdfDocument.Load(file);
                    total += doc.Pages.Count;
                }

                foreach (var file in files)
                {
                    var name    = SanitizeFileName(Path.GetFileNameWithoutExtension(file));
                    var baseDir = nextToSource ? Path.GetDirectoryName(file)! : outputFolder;
                    var saveDir = subfolder ? Path.Combine(baseDir, name) : baseDir;

                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);

                    using var doc = PdfDocument.Load(file);
                    int pageCount = doc.Pages.Count;

                    for (int i = 0; i < pageCount; i++)
                    {
                        using var page = doc.Pages[i];
                        string dest = Path.Combine(saveDir, $"{name}_{i + 1:D3}.jpg");
                        RenderPageToJpeg(page, dest, dpi, quality, rotate, colorMode);
                        done++;
                        ((IProgress<(int, int, string)>)progress)
                            .Report((done, total, $"{name}  —  page {i + 1} / {pageCount}"));
                    }
                }
            });

            UpdateProgress(total, total, $"Done — {total} page(s) converted.");
            ConvertBtnIcon.Text  = "✓";
            ConvertBtnLabel.Text = "Convert to JPG";

            if (!nextToSource)
            {
                System.Diagnostics.Process.Start("explorer.exe", outputFolder);
            }
        }
        catch (Exception ex)
        {
            UpdateProgress(0, 1, "Error during conversion.");
            MessageBox.Show($"Conversion failed:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetConvertingState(false);
            ConvertBtnIcon.Text  = "⚡";
            ConvertBtnLabel.Text = "Convert to JPG";
        }
    }

    private void SetConvertingState(bool on)
    {
        _converting                = on;
        ConvertBtn.IsEnabled       = !on;
        ProgressSection.Visibility = Visibility.Visible;
        if (on) { ConvertBtnLabel.Text = "Converting…"; ConvertBtnIcon.Text = "⏳"; }
    }

    private void UpdateProgress(int done, int total, string message)
    {
        StatusText.Text  = message;
        ProgressPct.Text = total > 0 ? $"{done * 100 / total}%" : "0%";
        double ratio = total > 0 ? (double)done / total : 0;
        double w     = _trackWidth > 0 ? _trackWidth : ProgressTrack.ActualWidth;
        ProgressFill.Width = Math.Max(0, w * ratio);
    }

    // ── PDF rendering ─────────────────────────────────────────────────────
    private static void RenderPageToJpeg(PdfPage page, string dest,
                                          int dpi, long quality,
                                          bool rotateLandscape, ColorMode colorMode)
    {
        float ptW = page.Width;
        float ptH = page.Height;
        bool landscape = ptW > ptH;
        int pixW, pixH;
        PageRotate rotation;

        if (landscape && rotateLandscape)
        {
            pixW     = (int)Math.Round(ptH / 72.0 * dpi);
            pixH     = (int)Math.Round(ptW / 72.0 * dpi);
            rotation = PageRotate.Rotate270;
        }
        else
        {
            pixW     = (int)Math.Round(ptW / 72.0 * dpi);
            pixH     = (int)Math.Round(ptH / 72.0 * dpi);
            rotation = PageRotate.Normal;
        }

        using var pdfBmp = new PdfBitmap(pixW, pixH, true);
        pdfBmp.FillRect(0, 0, pixW, pixH, FS_COLOR.White);
        page.Render(pdfBmp, 0, 0, pixW, pixH, rotation,
            RenderFlags.FPDF_LCD_TEXT | RenderFlags.FPDF_PRINTING);

        var rendered = (Bitmap)pdfBmp.Image;
        var final    = ApplyColorMode(rendered, colorMode);
        try
        {
            SaveJpeg(final, dest, quality);
        }
        finally
        {
            if (!ReferenceEquals(final, rendered)) final.Dispose();
        }
    }

    private static Bitmap ApplyColorMode(Bitmap source, ColorMode mode)
    {
        if (mode == ColorMode.Color) return source;

        // Convert to grayscale via ColorMatrix
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var g  = Graphics.FromImage(result);
        var cm = new ColorMatrix(new float[][]
        {
            new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
            new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
            new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
            new[] { 0f,     0f,     0f,     1f, 0f },
            new[] { 0f,     0f,     0f,     0f, 1f }
        });
        using var ia = new ImageAttributes();
        ia.SetColorMatrix(cm);
        g.DrawImage(source,
            new Rectangle(0, 0, source.Width, source.Height),
            0, 0, source.Width, source.Height,
            GraphicsUnit.Pixel, ia);

        if (mode == ColorMode.Grayscale) return result;

        // Black & White: threshold at 128
        var rect = new Rectangle(0, 0, result.Width, result.Height);
        var data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int bytes = data.Stride * data.Height;
        var buf   = new byte[bytes];
        Marshal.Copy(data.Scan0, buf, 0, bytes);
        for (int i = 0; i < bytes; i += 4)
        {
            byte bw = buf[i] < 128 ? (byte)0 : (byte)255;
            buf[i] = buf[i + 1] = buf[i + 2] = bw;
        }
        Marshal.Copy(buf, 0, data.Scan0, bytes);
        result.UnlockBits(data);
        return result;
    }

    private static void SaveJpeg(Bitmap bmp, string path, long quality)
    {
        var codec = ImageCodecInfo.GetImageEncoders()
                        .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var ep = new EncoderParameters(1);
        ep.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        bmp.Save(path, codec, ep);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}
