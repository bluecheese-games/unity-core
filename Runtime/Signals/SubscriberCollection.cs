//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.Signals
{
    public partial class SignalChannel
    {
        public interface ISubscriberCollection
        {
            ISubscriber Add<T>(Action<T> handler, object handle, bool oneShot);
            ISubscriber Add<T>(Func<T, UniTask> handler, object handle, bool oneShot);
            ISubscriber Add<T>(Action<T, SignalContext> handler, object handle, bool oneShot);
            ISubscriber Add<T>(Func<T, SignalContext, UniTask> handler, object handle, bool oneShot);
            int Count();
            SignalContext Publish<T>(T signal);
            UniTask<SignalContext> PublishAsync<T>(T signal);
            void ReplayLast(ISubscriber subscriber);
            void Remove(ISubscriber subscriber);
            void RemoveAll();
            void RemoveAll(object handle);
            void RemoveAll<T>(Action<T> handler);
        }

        private sealed class SubscriberCollection<TSignal> : ISubscriberCollection
        {
            private readonly List<ISubscriber> _subscribers = new();
            private TSignal _lastSignal;
            private bool _hasLastSignal;

            public ISubscriber Add<T>(Action<T> handler, object handle, bool oneShot)
            {
                var subscriber = new Subscriber<T>(handler, handle, oneShot);
                _subscribers.Add(subscriber);
                return subscriber;
            }

            public ISubscriber Add<T>(Func<T, UniTask> handler, object handle, bool oneShot)
            {
                var subscriber = new Subscriber<T>(handler, handle, oneShot);
                _subscribers.Add(subscriber);
                return subscriber;
            }

            public ISubscriber Add<T>(Action<T, SignalContext> handler, object handle, bool oneShot)
            {
                var subscriber = new Subscriber<T>(handler, handle, oneShot);
                _subscribers.Add(subscriber);
                return subscriber;
            }

            public ISubscriber Add<T>(Func<T, SignalContext, UniTask> handler, object handle, bool oneShot)
            {
                var subscriber = new Subscriber<T>(handler, handle, oneShot);
                _subscribers.Add(subscriber);
                return subscriber;
            }

            public int Count() => _subscribers.Count;

            public SignalContext Publish<T>(T signal)
            {
                _lastSignal = (TSignal)(object)signal;
                _hasLastSignal = true;

                var ctx = new SignalContext();
                var snapshot = GetSortedSnapshot();

                for (int i = 0; i < snapshot.Length; i++)
                {
                    var subscriber = (Subscriber<T>)snapshot[i];
                    subscriber.Invoke(signal, ctx);
                    if (subscriber.IsOneShot) Remove(subscriber);
                    if (ctx.IsCancelled) break;
                }
                return ctx;
            }

            public async UniTask<SignalContext> PublishAsync<T>(T signal)
            {
                _lastSignal = (TSignal)(object)signal;
                _hasLastSignal = true;

                var ctx = new SignalContext();
                var snapshot = GetSortedSnapshot();

                for (int i = 0; i < snapshot.Length; i++)
                {
                    var subscriber = (Subscriber<T>)snapshot[i];
                    await subscriber.InvokeAsync(signal, ctx);
                    if (subscriber.IsOneShot) Remove(subscriber);
                    if (ctx.IsCancelled) break;
                }
                return ctx;
            }

            public void ReplayLast(ISubscriber subscriber)
            {
                if (!_hasLastSignal) return;
                var ctx = new SignalContext();
                subscriber.InvokeAsync<TSignal>(_lastSignal, ctx).Forget();
                if (subscriber.IsOneShot) Remove(subscriber);
            }

            // OrderByDescending is stable: equal priorities preserve subscription (FIFO) order.
            private ISubscriber[] GetSortedSnapshot()
                => _subscribers.OrderByDescending(s => s.Priority).ToArray();

            public void Remove(ISubscriber subscriber) => _subscribers.Remove(subscriber);
            public void RemoveAll() => _subscribers.Clear();
            public void RemoveAll(object handle) => _subscribers.RemoveAll(s => s.HasHandle(handle));
            public void RemoveAll<T>(Action<T> handler) => _subscribers.RemoveAll(s => s.HasHandler(handler));
        }
    }
}
