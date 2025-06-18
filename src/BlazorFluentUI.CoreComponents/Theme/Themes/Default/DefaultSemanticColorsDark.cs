namespace BlazorFluentUI.Themes.Default
{
    public class DefaultSemanticColorsDark : SemanticColors
    {
        public DefaultSemanticColorsDark(IPalette palette)
        {
            DisabledBackground = palette.NeutralQuaternaryAlt;
            InputBackgroundChecked = palette.ThemePrimary;
            MenuBackground = palette.NeutralLighter;
            MenuItemBackgroundHovered = palette.NeutralQuaternaryAlt;
            MenuItemBackgroundPressed = palette.NeutralQuaternary;
            MenuDivider = palette.NeutralTertiaryAlt;
            MenuIcon = palette.ThemeDarkAlt;
            MenuHeader = palette.Black;
            
        }
    }
}
