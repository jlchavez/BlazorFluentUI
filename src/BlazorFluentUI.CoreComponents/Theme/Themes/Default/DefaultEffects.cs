﻿namespace BlazorFluentUI.Themes.Default
{
    public class DefaultEffects : IEffects
    {
        public string RoundedCorner2 => "2px";
        public string Elevation4 { get; }
        public string Elevation8 { get; }
        public string Elevation16 { get; }
        public string Elevation64 { get; }

        public DefaultEffects()
        {
            DefaultDepths? depths = new();
            Elevation4 = depths.Depth4;
            Elevation8 = depths.Depth8;
            Elevation16 = depths.Depth16;
            Elevation64 = depths.Depth64;
        }
    }
}
