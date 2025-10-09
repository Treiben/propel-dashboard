using Knara.UtcStrict;

namespace Propel.FeatureFlags.Dashboard.Api.Domain;

public record RetentionPolicy(bool IsPermanent, UtcDateTime ExpirationDate)
{
	public static RetentionPolicy OneMonthRetentionPolicy => new(false, ExpirationDate: DateTimeOffset.UtcNow.AddDays(30));

	public static RetentionPolicy GlobalPolicy => new(true, UtcDateTime.MaxValue);

	public bool IsExpired => ExpirationDate <= DateTime.UtcNow;

	public bool IsDeleteable => !IsPermanent && IsExpired;
}
