using Zeayii.Flow.Presentation.Implementations;

namespace Zeayii.Flow.Tests;

/// <summary>
/// 校验终端文本宽度处理逻辑的测试集合。
/// </summary>
public sealed class RenderTextTests
{
    /// <summary>
    /// 验证中文字符会按双宽裁剪并保持总宽度稳定。
    /// </summary>
    [Fact]
    public void TruncateAndPad_ShouldRespectDisplayWidthForCjkText()
    {
        var result = RenderText.TruncateAndPad("中文ABC", 6);

        Assert.Equal(6, RenderText.GetDisplayWidth(result));
        Assert.Equal("中文A…", result);
    }

    /// <summary>
    /// 验证左填充会按照显示宽度补空格。
    /// </summary>
    [Fact]
    public void PadLeftPlain_ShouldRespectDisplayWidthForCjkText()
    {
        var result = RenderText.PadLeftPlain("中文", 6);

        Assert.Equal(6, RenderText.GetDisplayWidth(result));
        Assert.Equal("  中文", result);
    }
}


