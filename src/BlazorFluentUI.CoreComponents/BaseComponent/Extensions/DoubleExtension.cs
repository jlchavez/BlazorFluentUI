﻿namespace BlazorFluentUI
{
    public static class DoubleExtension
    {
        public static string ToCssValue(this double value)
        {
            return value.ToString().Replace(',', '.');
        }
    }
}
