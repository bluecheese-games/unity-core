using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.SanityCheck.Editor
{
	/// <summary>
	/// Describes a discovered [SanityCheck] class and how to invoke it, sync or async.
	/// </summary>
	public sealed class SanityCheckEntry
	{
		public Type Type { get; }
		public string DisplayName { get; }
		public string Category { get; }
		public int Priority { get; }
		public bool IsAsync => _asyncMethod != null;

		private readonly MethodInfo _syncMethod;
		private readonly MethodInfo _asyncMethod;
		private readonly bool _asyncTakesToken;

		public SanityCheckEntry(Type type, SanityCheckAttribute attribute, MethodInfo syncMethod, MethodInfo asyncMethod, bool asyncTakesToken)
		{
			Type = type;
			DisplayName = string.IsNullOrEmpty(attribute.Name) ? type.Name : attribute.Name;
			Category = string.IsNullOrEmpty(attribute.Category) ? "General" : attribute.Category;
			Priority = attribute.Priority;
			_syncMethod = syncMethod;
			_asyncMethod = asyncMethod;
			_asyncTakesToken = asyncTakesToken;
		}

		public UniTask<SanityCheckResult> InvokeAsync(CancellationToken token)
		{
			if (_asyncMethod != null)
			{
				object[] args = _asyncTakesToken ? new object[] { token } : null;
				return (UniTask<SanityCheckResult>)_asyncMethod.Invoke(null, args);
			}

			var result = (SanityCheckResult)_syncMethod.Invoke(null, null);
			return UniTask.FromResult(result);
		}
	}
}
