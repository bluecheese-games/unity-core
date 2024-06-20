//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Signals
{

    public partial class SignalChannel
    {
        public interface ISubscriber
        {
            bool HasHandle(object handle);
            bool HasHandler<T>(Action<T> handler);
            void Invoke<T>(T signal);
            Task InvokeAsync<T>(T signal);
        }

        public class Subscriber<TSignal> : ISubscriber
        {
            private readonly Func<TSignal, Task> _asyncHandler;
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

            public Subscriber(Func<TSignal, Task> handler, object handle, bool isOneShot)
            {
                _asyncHandler = handler;
                _handle = handle;
                _isOneShot = isOneShot;
            }

            public void Invoke<T>(T signal)
            {
                _handler.DynamicInvoke(signal);
            }

            public async Task InvokeAsync<T>(T signal)
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
