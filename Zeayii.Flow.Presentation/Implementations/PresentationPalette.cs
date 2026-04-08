using Spectre.Console;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// Flow Presentation 统一语义色板。
/// </summary>
internal static class PresentationPalette
{
    public static Color Muted => Color.Grey70;
    public static Color Accent => Color.DeepSkyBlue1;
    public static Color Success => Color.SpringGreen2;
    public static Color Warning => Color.Gold1;
    public static Color Failure => Color.IndianRed1;
    public static Color Info => Color.White;
    public static Color Skipped => Color.Orange3;
}
