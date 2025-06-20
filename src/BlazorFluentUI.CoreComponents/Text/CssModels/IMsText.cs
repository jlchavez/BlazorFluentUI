﻿namespace BlazorFluentUI
{
    public interface IMsText : IRuleProperties
	{
		string? Color { get; set; }

		string? FontFamily { get; set; }

		string? FontSize { get; set; }

		string? FontWeight { get; set; }

		string? WebkitFontSmoothing { get; set; }

		string? MozOsxFontSmoothing { get; set; }
	}
}
