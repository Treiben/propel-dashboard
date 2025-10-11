using Knara.UtcStrict;
using Propel.FeatureFlags.Domain;

namespace Propel.FeatureFlags.Dashboard.Api.Domain;

public record FlagLockPolicy(EvaluationMode[] EvaluationModes)
{
	public bool LockEnabled(UtcDateTime expirationDate, bool ispermanent)
	{
		if (EvaluationModes == null || EvaluationModes.Length == 0) 
			throw new Exception("Lock policies must contain at least one evaluation mode.");

		if (ispermanent)
			return true;

		bool isExpired = expirationDate <= UtcDateTime.UtcNow;
		if (isExpired)
			return false;

		var disabled = EvaluationModes.Contains(EvaluationMode.Off);
		if (disabled)
			return false;

		return true;
	}
}
public record RetentionPolicy(bool IsPermanent, UtcDateTime ExpirationDate, FlagLockPolicy FlagLockPolicy)
{
	public static UtcDateTime ExpiresIn90Days => DateTimeOffset.UtcNow.AddDays(90);

	public bool IsLocked => FlagLockPolicy.LockEnabled(ExpirationDate, IsPermanent);
}
