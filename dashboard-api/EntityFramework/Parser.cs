using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Utilities;
using System.Text.Json;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework;

public static class Parser
{
	public static ModeSet ParseEvaluationModes(string json)
	{
		try
		{
			var modes = JsonSerializer.Deserialize<int[]>(json, JsonDefaults.JsonOptions) ?? [];
			var enumModes = modes.Select(m => (EvaluationMode)m).ToHashSet();
			return new ModeSet(enumModes);
		}
		catch
		{
			return EvaluationMode.Off;
		}
	}

	public static DayOfWeek[] ParseWindowDays(string json)
	{
		try
		{
			var days = JsonSerializer.Deserialize<int[]>(json, JsonDefaults.JsonOptions) ?? [];
			return [.. days.Select(d => Enum.IsDefined((DayOfWeek)d) ? (DayOfWeek)d : throw new ArgumentException())];
		}
		catch
		{
			return [];
		}
	}

	public static List<ITargetingRule> ParseTargetingRules(string json)
	{
		try
		{
			// This would need a more sophisticated targeting rule parser
			// For now, return empty list
			return JsonSerializer.Deserialize<List<ITargetingRule>>(json, JsonDefaults.JsonOptions) ?? [];
		}
		catch
		{
			return [];
		}
	}

	public static List<string> ParseStringList(string json)
	{
		try
		{
			return JsonSerializer.Deserialize<List<string>>(json, JsonDefaults.JsonOptions) ?? [];
		}
		catch
		{
			return [];
		}
	}

	public static Variations ParseVariations(string json, string defaultVariation)
	{
		try
		{
			var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonDefaults.JsonOptions) ?? [];
			var variations = new Variations
			{
				Values = values,
				DefaultVariation = defaultVariation
			};
			return variations;
		}
		catch
		{
			return new Variations();
		}
	}
}

