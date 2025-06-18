using System;

namespace BlazorFluentUI
{
    public class CsPropertyAttribute : Attribute
    {
        public string? PropertyName { get; set; }
        public bool IsCssStringProperty { get; set; } = false;

        public CsPropertyAttribute()
        {

        }
    }
}
