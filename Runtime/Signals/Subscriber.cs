//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.Signals
{
    public partial class SignalChannel
    {
        public interface ISubscriber
        {
            bool HasHandle(object handle);
            bool HasHandler<T>(Action<T> handler);
            void Invoke<T>(T signal, SignalContext ctx);
            UniTask InvokeAsync<T>(T signal, SignalContext ctx);
            int Priority { get; set; }
            bool IsOneShot { get; set; }
        }

        public class Subscriber<TSignal> : ISubscriber
        {
            private readonly Action<TSignal> _handler;
            private readonly Func<TSignal, UniTask> _asyncHandler;
            private readonly Action<TSignal, SignalContext> _cancellableHandler;
            private readonly Func<TSignal, SignalContext, UniTask> _cancellableAsyncHandler;
            private readonly object _handle;

            public int Priority { get; set; } = 0;
            public bool IsOneShot { get; set; }
            public Func<TSignal, bool> Filter { get; set; }

            public Subscriber(Action<TSignal> handler, object handle, bool isOneShot)
            {
                _handler = handler;
                _handle = handle;
                IsOneShot = isOneShot;
            }

            public Subscriber(Func<TSignal, UniTask> handler, object handle, bool isOneShot)
            {
                _asyncHandler = handler;
                _handle = handle;
                IsOneShot = isOneShot;
            }

            public Subscriber(Action<TSignal, SignalContext> handler, object handle, bool isOneShot)
            {
                _cancellableHandler = handler;
                _handle = handle;
                IsOneShot = isOneShot;
            }

            public Subscriber(Func<TSignal, SignalContext, UniTask> handler, object handle, bool isOneShot)
            {
                _cancellableAsyncHandler = handler;
                _handle = handle;
                IsOneShot = isOneShot;
            }

            public void Invoke<T>(T signal, SignalContext ctx)
            {
                var typedSignal = (TSignal)(object)signal;
                if (Filter != null && !Filter(typedSignal)) return;

                if (_cancellableHandler != null)
                    _cancellableHandler(typedSignal, ctx);
                else
                    _handler(typedSignal);
            }

            public async UniTask InvokeAsync<T>(T signal, SignalContext ctx)
            {
                var typedSignal = (TSignal)(object)signal;
                if (Filter != null && !Filter(typedSignal)) return;

                if (_cancellableAsyncHandler != null)
                    await _cancellableAsyncHandler(typedSignal, ctx);
                else if (_asyncHandler != null)
                    await _asyncHandler(typedSignal);
                else if (_cancellableHandler != null)
                    _cancellableHandler(typedSignal, ctx);
                else
                    _handler(typedSignal);
            }

            public bool HasHandle(object handle) => handle == _handle;

            public bool HasHandler<T>(Action<T> handler)
            {
                return _handler != null && handler.Method.Equals(_handler.Method);
            }
        }
    }
}
