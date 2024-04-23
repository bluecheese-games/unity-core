//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Core.Utils
{
    public class ResourceRequestAwaiter : INotifyCompletion
    {
        private readonly ResourceRequest _request;
        private Action _continuation;

        public ResourceRequestAwaiter(ResourceRequest request)
        {
            _request = request;
            request.completed += OnRequestCompleted;
        }

        public bool IsCompleted => _request.isDone;

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
        public static ResourceRequestAwaiter GetAwaiter(this ResourceRequest request)
        {
            return new ResourceRequestAwaiter(request);
        }
    }
}