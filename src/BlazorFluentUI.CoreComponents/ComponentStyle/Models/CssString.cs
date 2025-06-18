namespace BlazorFluentUI
{
    public class CssString : IRuleProperties
    {
        [CsProperty(IsCssStringProperty = true)]
        public string? Css { get; set; }
    }
}
