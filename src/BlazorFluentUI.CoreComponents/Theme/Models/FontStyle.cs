using BlazorFluentUI.Themes.Default;

namespace BlazorFluentUI
{
    public class FontStyle : IFontStyle
    {
        public IFontSize FontSize { get; set; } = new DefaultFontSize();
        public IFontWeight FontWeight { get; set; } = new DefaultFontWeight();
        public IIconFontSize IconFontSize { get; set; } = new DefaultIconFontSize();
    }
}
