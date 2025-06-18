namespace BlazorFluentUI
{
    public class CssStringSelector : ISelector
    {
        public string? SelectorName { get; set; }

        public string? GetSelectorAsString()
        {
            return SelectorName;
        }
    }
}
