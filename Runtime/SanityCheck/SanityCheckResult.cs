//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.SanityCheck
{
	/// <summary>
	/// Outcome severity of a sanity check.
	/// </summary>
	public enum SanitySeverity
	{
		Valid,
		Warning,
		Error
	}

	/// <summary>
	/// Result returned by a sanity check's Run/RunAsync method.
	/// </summary>
	public readonly struct SanityCheckResult
	{
		public readonly SanitySeverity Severity;
		public readonly string Message;

		private SanityCheckResult(SanitySeverity severity, string message)
		{
			Severity = severity;
			Message = message;
		}

		public static SanityCheckResult Valid(string message = null) => new SanityCheckResult(SanitySeverity.Valid, message);

		public static SanityCheckResult Warning(string message) => new SanityCheckResult(SanitySeverity.Warning, message);

		public static SanityCheckResult Error(string message) => new SanityCheckResult(SanitySeverity.Error, message);
	}
}
