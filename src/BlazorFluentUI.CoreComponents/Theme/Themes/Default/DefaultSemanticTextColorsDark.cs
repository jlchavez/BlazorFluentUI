namespace BlazorFluentUI.Themes.Default
{
    public class DefaultSemanticTextColorsDark : SemanticTextColors
    {
        public DefaultSemanticTextColorsDark(IPalette palette)
        {
            ButtonText = palette.Black;
            ButtonTextPressed = palette.NeutralDark;
            ButtonTextHovered = palette.NeutralPrimary;
            BodySubtext = palette.Black;

            MenuItemText = palette.NeutralPrimary;
            MenuItemTextHovered = palette.NeutralDark;
        }
    }
}
