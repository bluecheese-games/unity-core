//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class AsyncInstantiateOperationAwaiter<T> : INotifyCompletion where T : UnityEngine.Object
	{
		private readonly AsyncInstantiateOperation<T> _asyncOp;
		private Action _continuation;

		public AsyncInstantiateOperationAwaiter(AsyncInstantiateOperation<T> asyncOp)
		{
			_asyncOp = asyncOp;
			asyncOp.completed += OnRequestCompleted;
		}

		public bool IsCompleted => _asyncOp.isDone;

		public T GetResult()
		{
			return _asyncOp.Result[0];
		}

		public void OnCompleted(Action continuation)
		{
			_continuation = continuation;
		}

		private void OnRequestCompleted(AsyncOperation obj)
		{
			_continuation();
		}
	}

	public static partial class AwaiterExtensionMethods
	{
		public static AsyncInstantiateOperationAwaiter<T> GetAwaiter<T>(this AsyncInstantiateOperation<T> asyncOp) where T : UnityEngine.Object
		{
			return new AsyncInstantiateOperationAwaiter<T>(asyncOp);
		}
	}
}