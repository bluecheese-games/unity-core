//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Linq;
using System.Reflection;

namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Utility for injecting dependencies into objects not created by the container (e.g., MonoBehaviours).
	/// </summary>
	public static class ServiceInjector
	{
		/// <summary>
		/// Populates fields and properties marked with [Injectable] using the current ServiceLocator.
		/// </summary>
		public static void Inject(object instance)
		{
			if (instance == null) return;
			for (var t = instance.GetType(); t != null && t != typeof(object); t = t.BaseType)
			{
				var members = t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
							   .Where(m => m is FieldInfo || m is PropertyInfo);

				foreach (var m in members)
				{
					var attr = m.GetCustomAttribute<InjectableAttribute>();
					if (attr == null) continue;

					var type = m is FieldInfo f ? f.FieldType : ((PropertyInfo)m).PropertyType;
					var service = ServiceLocator.Resolve(type, attr.Key);

					if (m is FieldInfo field) field.SetValue(instance, service);
					else if (m is PropertyInfo prop) prop.SetValue(instance, service);
				}
			}
		}
	}
}