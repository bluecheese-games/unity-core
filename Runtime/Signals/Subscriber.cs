//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.Signals
{

    public partial class SignalChannel
    {
        public interface ISubscriber
        {
            bool HasHandle(object handle);
            bool HasHandler<T>(Action<T> handler);
            void Invoke<T>(T signal);
			UniTask InvokeAsync<T>(T signal);
        }

        public class Subscriber<TSignal> : ISubscriber
        {
            private readonly Func<TSignal, UniTask> _asyncHandler;
            private readonly Action<TSignal> _handler;
            private readonly object _handle;
            private readonly bool _isOneShot;

            public bool IsOneShot => _isOneShot;

            public Subscriber(Action<TSignal> handler, object handle, bool isOneShot)
            {
                _handler = handler;
                _handle = handle;
                _isOneShot = isOneShot;
            }

            public Subscriber(Func<TSignal, UniTask> handler, object handle, bool isOneShot)
            {
                _asyncHandler = handler;
                _handle = handle;
                _isOneShot = isOneShot;
            }

            public void Invoke<T>(T signal)
            {
                _handler.DynamicInvoke(signal);
            }

            public async UniTask InvokeAsync<T>(T signal)
            {
                if (_asyncHandler != null)
                {
                    await _asyncHandler.Invoke((TSignal)Convert.ChangeType(signal, typeof(TSignal)));
                }
                else
                {
                    _handler.DynamicInvoke(signal);
                }
            }

            public bool HasHandle(object handle)
            {
                return handle == _handle;
            }

            public bool HasHandler<T>(Action<T> handler)
            {
                return handler.Method.Equals(_handler.Method);
            }
        }
    }
}
