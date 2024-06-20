﻿//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Signals
{
    public partial class SignalChannel
    {
        public interface ISubscriberCollection
        {
            void Add<T>(SignalHandler<T> handler, object handle, bool once);
            int Count();
            void Publish<T>(T signal);
            Task PublishAsync<T>(T signal);
            void Remove(ISubscriber subscriber);
            void RemoveAll();
            void RemoveAll(object handle);
            void RemoveAll<T>(SignalHandler<T> handler);
        }

        private sealed class SubscriberCollection<TSignal> : ISubscriberCollection
        {
            private readonly List<ISubscriber> _subscribers = new();

            public void Add<T>(SignalHandler<T> handler, object handle, bool oneShot)
            {
                var subscriber = new Subscriber<T>(handler, handle, oneShot);
                _subscribers.Add(subscriber);
            }

            public int Count() => _subscribers.Count;

            public void Publish<T>(T signal)
            {
                var subscribersCopy = _subscribers.ToArray();

                // Publish the signal to all subscribers
                for (int i = 0; i < subscribersCopy.Length; i++)
                {
                    Subscriber<T> subscriber = (Subscriber<T>)subscribersCopy[i];
                    subscriber.Invoke(signal);
                    if (subscriber.IsOneShot)
                    {
                        Remove(subscriber);
                    }
                }
            }

            public async Task PublishAsync<T>(T signal)
            {
                var subscribersCopy = _subscribers.ToArray();

                // Publish the signal to all subscribers
                for (int i = 0; i < subscribersCopy.Length; i++)
                {
                    Subscriber<T> subscriber = (Subscriber<T>)subscribersCopy[i];
                    await subscriber.InvokeAsync(signal);
                    if (subscriber.IsOneShot)
                    {
                        Remove(subscriber);
                    }
                }
            }

            public void Remove(ISubscriber subscriber) => _subscribers.Remove(subscriber);

            public void RemoveAll() => _subscribers.Clear();

            public void RemoveAll(object handle) => _subscribers.RemoveAll(s => s.HasHandle(handle));

            public void RemoveAll<T>(SignalHandler<T> handler) => _subscribers.RemoveAll(s => s.HasHandler(handler));
        }
    }
}
