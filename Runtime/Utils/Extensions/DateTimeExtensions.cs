//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core
{
	public static class DateTimeExtensions
	{
		public static string TimeAgo(this DateTime dateTime)
			=> (DateTime.UtcNow - dateTime.ToUniversalTime()).TimeAgo();

		public static string TimeAgo(this TimeSpan ts)
		{
			return ts.TotalSeconds switch
			{
				< 60 => $"{ts.Seconds} second{(ts.Seconds == 1 ? "" : "s")} ago",
				< 3600 => $"{ts.Minutes} minute{(ts.Minutes == 1 ? "" : "s")} ago",
				< 86400 => $"{ts.Hours} hour{(ts.Hours == 1 ? "" : "s")} ago",
				< 172800 => "yesterday",
				< 2592000 => $"{ts.Days} day{(ts.Days == 1 ? "" : "s")} ago",
				< 31536000 => $"{(int)(ts.TotalDays / 30)} month{((int)(ts.TotalDays / 30) == 1 ? "" : "s")} ago",
				_ => $"{(int)(ts.TotalDays / 365)} year{((int)(ts.TotalDays / 365) == 1 ? "" : "s")} ago"
			};
		}
	}
}
