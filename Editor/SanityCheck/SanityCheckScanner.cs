using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.Core.SanityCheck.Editor
{
	/// <summary>
	/// Scans loaded assemblies for static classes tagged with [SanityCheck].
	/// </summary>
	public static class SanityCheckScanner
	{
		public static List<SanityCheckEntry> Scan()
		{
			var entries = new List<SanityCheckEntry>();

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException e)
				{
					types = e.Types.Where(t => t != null).ToArray();
				}

				foreach (var type in types)
				{
					if (!type.IsClass || !type.IsAbstract || !type.IsSealed) continue; // static class

					var attribute = type.GetCustomAttribute<SanityCheckAttribute>();
					if (attribute == null) continue;

					var entry = TryCreateEntry(type, attribute);
					if (entry != null) entries.Add(entry);
				}
			}

			return entries.OrderBy(e => e.Priority).ThenBy(e => e.Category).ThenBy(e => e.DisplayName).ToList();
		}

		private static SanityCheckEntry TryCreateEntry(Type type, SanityCheckAttribute attribute)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

			var asyncWithToken = type.GetMethod("RunAsync", flags, null, new[] { typeof(System.Threading.CancellationToken) }, null);
			var asyncNoToken = asyncWithToken == null ? type.GetMethod("RunAsync", flags, null, Type.EmptyTypes, null) : null;
			var asyncMethod = asyncWithToken ?? asyncNoToken;

			if (asyncMethod != null && asyncMethod.ReturnType != typeof(UniTask<SanityCheckResult>))
				asyncMethod = null;

			if (asyncMethod != null)
				return new SanityCheckEntry(type, attribute, null, asyncMethod, asyncWithToken != null);

			var syncMethod = type.GetMethod("Run", flags, null, Type.EmptyTypes, null);
			if (syncMethod != null && syncMethod.ReturnType == typeof(SanityCheckResult))
				return new SanityCheckEntry(type, attribute, syncMethod, null, false);

			Debug.LogWarning($"[SanityCheck] '{type.FullName}' has [SanityCheck] but no valid Run() or RunAsync() method. Skipped.");
			return null;
		}
	}
}
