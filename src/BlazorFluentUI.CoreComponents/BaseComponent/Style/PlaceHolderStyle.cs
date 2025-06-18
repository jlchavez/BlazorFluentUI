using System.Collections.Generic;

namespace BlazorFluentUI.Style
{
    public static class PlaceHolderStyle
    {
        public static IList<Rule> GetPlaceholderStyle(string selectorName ,IRuleProperties properties)
        {
            List<Rule>? placeholderRules = new()
            {
                new Rule()
                {
                    Selector = new CssStringSelector() { SelectorName = $"{selectorName}::placeholder" },
                    Properties = properties
                },
                new Rule()
                {
                    Selector = new CssStringSelector() { SelectorName = $"{selectorName}:-ms-input-placeholder" },
                    Properties = properties
                },
                new Rule()
                {
                    Selector = new CssStringSelector() { SelectorName = $"{selectorName}::-ms-input-placeholder" },
                    Properties = properties
                }
            };

            return placeholderRules;
        }
    }
}
