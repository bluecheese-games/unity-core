//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;

namespace Core.Signals
{

    public partial class SignalChannel
    {
        public interface ISubscriber
        {
            bool HasHandle(object handle);
            bool HasHandler<T>(SignalHandler<T> handler);
            void Invoke<T>(T signal);
            Task InvokeAsync<T>(T signal);
        }

        public readonly struct Subscriber<TSignal> : ISubscriber
        {
            private readonly SignalHandler<TSignal> _handler;
            private readonly object _handle;
            private readonly bool _isOneShot;

            public readonly bool IsOneShot => _isOneShot;

            public Subscriber(SignalHandler<TSignal> handler, object handle, bool isOneShot)
            {
                _handler = handler;
                _handle = handle;
                _isOneShot = isOneShot;
            }

            public readonly void Invoke<T>(T signal)
            {
                _handler.DynamicInvoke(signal);
            }

            public async readonly Task InvokeAsync<T>(T signal)
            {
                var handler = _handler;
                await Task.Run(() => handler.DynamicInvoke(signal)).ConfigureAwait(false);
            }

            public readonly bool HasHandle(object handle)
            {
                return handle == _handle;
            }

            public readonly bool HasHandler<T>(SignalHandler<T> handler)
            {
                return handler.Method.Equals(_handler.Method);
            }
        }
    }
}
