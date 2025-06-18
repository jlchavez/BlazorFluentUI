namespace BlazorFluentUI
{
    public class MediaSelector : ISelector
    {
        public string? SelectorName { get; set; }

        public string? GetSelectorAsString()
        {
            return SelectorName;
        }
    }
}
