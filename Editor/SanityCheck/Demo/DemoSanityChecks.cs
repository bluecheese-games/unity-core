using System.Threading;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.SanityCheck.Editor.Demo
{
	/*[SanityCheck("Always Valid", Category = "Demo")]
	public static class DemoValidCheck
	{
		public static SanityCheckResult Run()
		{
			return SanityCheckResult.Valid("Everything looks fine.");
		}
	}

	[SanityCheck("Always Warning", Category = "Demo")]
	public static class DemoWarningCheck
	{
		public static SanityCheckResult Run()
		{
			return SanityCheckResult.Warning("Something looks off, but it's not blocking.");
		}
	}

	[SanityCheck("Always Error (async)", Category = "Demo")]
	public static class DemoErrorAsyncCheck
	{
		public static async UniTask<SanityCheckResult> RunAsync(CancellationToken token)
		{
			await UniTask.Delay(5000, cancellationToken: token);
			return SanityCheckResult.Error("Something is definitely broken.");
		}
	}

	[SanityCheck("Always Valid (async)", Category = "Demo")]
	public static class DemoValidAsyncCheck
	{
		public static async UniTask<SanityCheckResult> RunAsync(CancellationToken token)
		{
			await UniTask.Delay(5000, cancellationToken: token);
			return SanityCheckResult.Valid("Everything looks fine.");
		}
	}*/
}
