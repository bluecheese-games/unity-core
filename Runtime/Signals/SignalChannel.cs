//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Signals
{
    public delegate void SignalHandler<T>(T signal);

    public class SignalChannel
    {
        private readonly Dictionary<Type, List<Subscriber>> _subscribers = new Dictionary<Type, List<Subscriber>>();

        /// <summary>
        /// Unsubscribe all subscribers from all signals
        /// </summary>
        public void Reset()
        {
            foreach (Type type in _subscribers.Keys)
            {
                UnsubscribeAll(type);
            }
        }

        /// <summary>
        /// Subscribe to a signal.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public void Subscribe<T>(SignalHandler<T> handler, object handle = null)
            => Subscribe(handler, handle, false);

        /// <summary>
        /// Subscribe to a signal.
        /// Automatically unsubscribe after the first signal received.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public void SubscribeOnce<T>(SignalHandler<T> handler, object handle = null)
            => Subscribe(handler, handle, true);

        private void Subscribe<T>(SignalHandler<T> handler, object handle = null, bool once = false)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type type = typeof(T);
            var subscriber = new Subscriber()
            {
                Handler = handler,
                Handle = handle,
                IsOneShot = once,
            };

            SignalHandleObserver.TryAddObserver(handle);

            EnsureSubscriberList(type);
            _subscribers[type].Add(subscriber);
        }

        /// <summary>
        /// Returns the number of active subscribers of this signal.
        /// </summary>
        /// <typeparam name="T">The signal type</typeparam>
        public int Count<T>()
        {
            Type type = typeof(T);
            if (_subscribers.ContainsKey(type) == false)
            {
                // There is no subscriber for this signal
                return 0;
            }
            return _subscribers[type].Count;
        }

        /// <summary>
        /// Unsubscribe from a signal.
        /// </summary>
        /// <param name="handler">The subscribed handler method</param>
        public void Unsubscribe<T>(SignalHandler<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type type = typeof(T);
            EnsureSubscriberList(type);

            _subscribers[type].RemoveAll(s => s.Handler.Equals(handler));
        }

        /// <summary>
        /// Unsubscribe from a signal.
        /// </summary>
        /// <param name="handle">The subscribing handle</param>
        public void Unsubscribe(object handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            foreach (var type in _subscribers.Keys)
            {
                EnsureSubscriberList(type);
                _subscribers[type].RemoveAll(s => s.Handle == handle);
            }
        }

        /// <summary>
        /// Unsubscribe all subscribers from a signal.
        /// </summary>
        public void UnsubscribeAll<T>()
            => UnsubscribeAll(typeof(T));

        private void UnsubscribeAll(Type type)
        {
            if (_subscribers.ContainsKey(type) == false)
            {
                // There is no subscriber for this signal
                return;
            }

            _subscribers[type].Clear();
        }

        /// <summary>
        /// Publish a signal.
        /// </summary>
        /// <param name="signal">The signal instance</param>
        public void Publish<T>(T signal = default)
        {
            Type type = typeof(T);
            if (_subscribers.ContainsKey(type) == false)
            {
                // There is no subscriber for this signal
                return;
            }

            // Make a copy of the subscriber list before publishing the signal
            var subscribersCopy = _subscribers[type].ToArray();

            // Publish the signal to all subscribers
            for (int i = 0; i < subscribersCopy.Length; i++)
            {
                Subscriber subscriber = subscribersCopy[i];
                subscriber.Handler.DynamicInvoke(signal);
                if (subscriber.IsOneShot)
                {
                    _subscribers[type].Remove(subscriber);
                }
            }
        }

        /// <summary>
        /// Publish a signal asynchronously.
        /// </summary>
        /// <param name="signal">The signal instance</param>
        public async Task PublishAsync<T>(T signal = default)
        {
            Type type = typeof(T);
            if (_subscribers.ContainsKey(type) == false)
            {
                // There is no subscriber for this signal
                return;
            }

            // Make a copy of the subscriber list before publishing the signal
            var subscribersCopy = _subscribers[type].ToArray();

            // Publish the signal to all subscribers
            for (int i = 0; i < subscribersCopy.Length; i++)
            {
                Subscriber subscriber = subscribersCopy[i];
                await Task.Run(() => subscriber.Handler.DynamicInvoke(signal)).ConfigureAwait(false);
                if (subscriber.IsOneShot)
                {
                    _subscribers[type].Remove(subscriber);
                }
            }
        }

        private void EnsureSubscriberList(Type type)
        {
            if (_subscribers.ContainsKey(type))
            {
                return;
            }

            _subscribers[type] = new List<Subscriber>();
        }

        private class Subscriber
        {
            public Delegate Handler;
            public object Handle;
            public bool IsOneShot;
        }
    }
}
