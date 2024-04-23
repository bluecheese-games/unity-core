//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Core.Utils
{
    public class AsyncOperationAwaiter : INotifyCompletion
    {
        private readonly AsyncOperation _asyncOp;
        private Action _continuation;

        public AsyncOperationAwaiter(AsyncOperation asyncOp)
        {
            _asyncOp = asyncOp;
            asyncOp.completed += OnRequestCompleted;
        }

        public bool IsCompleted => _asyncOp.isDone;

        public void GetResult()
        {
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
        public static AsyncOperationAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            return new AsyncOperationAwaiter(asyncOp);
        }
    }
}