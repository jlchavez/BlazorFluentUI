using System;

namespace BlazorFluentUI
{
    public class ThemeChangedArgs : EventArgs
    {
        public ITheme Theme { get; }
        public ThemeChangedArgs(ITheme theme)
        {
            Theme = theme;
        }
    }
}
