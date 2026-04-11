using System.Windows;

namespace PdfToJpgConverter;

public partial class App : Application
{
    public static void ApplyDarkTheme()
    {
        var r = Current.Resources;
        r["WindowBg"]        = Brush("#1E1E1E");
        r["TitleBarBg"]      = Brush("#252526");
        r["CardBg"]          = Brush("#2D2D2D");
        r["DropZoneBg"]      = Brush("#252525");
        r["ItemRowBg"]       = Brush("#333333");
        r["BorderBrush"]     = Brush("#404040");
        r["AccentBrush"]     = Brush("#049DD9");
        r["AccentHoverBrush"]= Brush("#0378A6");
        r["TextPrimary"]     = Brush("#D4D4D4");
        r["TextSecondary"]   = Brush("#888888");
        r["TextMuted"]       = Brush("#555555");
        r["ProgressBg"]      = Brush("#333333");
        r["InputBg"]         = Brush("#252525");
    }

    public static void ApplyLightTheme()
    {
        var r = Current.Resources;
        r["WindowBg"]        = Brush("#F2F2F2");
        r["TitleBarBg"]      = Brush("#E4E4E4");
        r["CardBg"]          = Brush("#FFFFFF");
        r["DropZoneBg"]      = Brush("#F8F8F8");
        r["ItemRowBg"]       = Brush("#F2F2F2");
        r["BorderBrush"]     = Brush("#D0D0D0");
        r["AccentBrush"]     = Brush("#049DD9");
        r["AccentHoverBrush"]= Brush("#0378A6");
        r["TextPrimary"]     = Brush("#0D0D0D");
        r["TextSecondary"]   = Brush("#555555");
        r["TextMuted"]       = Brush("#AAAAAA");
        r["ProgressBg"]      = Brush("#E0E0E0");
        r["InputBg"]         = Brush("#FFFFFF");
    }

    private static System.Windows.Media.SolidColorBrush Brush(string hex) =>
        new(System.Windows.Media.ColorConverter.ConvertFromString(hex) is System.Windows.Media.Color c
            ? c : System.Windows.Media.Colors.Transparent);
}
