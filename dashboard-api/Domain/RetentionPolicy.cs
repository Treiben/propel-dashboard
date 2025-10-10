using Knara.UtcStrict;
using Propel.FeatureFlags.Domain;

namespace Propel.FeatureFlags.Dashboard.Api.Domain;

public record FlagLockPolicy(EvaluationMode[] EvaluationModes)
{
	public bool EnableLock()
	{
		if (EvaluationModes == null || EvaluationModes.Length == 0) 
			throw new Exception("Lock policies must contain at least one evaluation mode.");

		return EvaluationModes.Contains(EvaluationMode.Off) == false;
	}
}
public record RetentionPolicy(bool IsPermanent, UtcDateTime ExpirationDate, FlagLockPolicy FlagLockPolicy)
{
	public static UtcDateTime ExpiresIn90Days => DateTimeOffset.UtcNow.AddDays(90);

	public bool IsExpired => ExpirationDate <= UtcDateTime.UtcNow;

	public bool IsLocked => IsPermanent || FlagLockPolicy.EnableLock();
}
