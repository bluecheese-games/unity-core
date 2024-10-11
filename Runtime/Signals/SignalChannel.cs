//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueCheese.Core.Signals
{
    public partial class SignalChannel
    {
        private readonly Dictionary<Type, ISubscriberCollection> _subscriberCollections = new Dictionary<Type, ISubscriberCollection>();

        /// <summary>
        /// Unsubscribe all subscribers from all signals
        /// </summary>
        public void Reset()
        {
            foreach (var collection in _subscriberCollections.Values)
            {
                collection.RemoveAll();
            }
        }

        /// <summary>
        /// Subscribe to a signal.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public void Subscribe<T>(Func<T, Task> handler, object handle = null)
            => Subscribe(handler, handle, false);

        /// <summary>
        /// Subscribe to a signal.
        /// Automatically unsubscribe after the first signal received.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public void SubscribeOnce<T>(Func<T, Task> handler, object handle = null)
            => Subscribe(handler, handle, true);

        private void Subscribe<T>(Func<T, Task> handler, object handle = null, bool oneshot = false)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            SignalHandleObserver.TryAddObserver(handle);

            GetSubscriberCollection<T>().Add(handler, handle, oneshot);
        }

        /// <summary>
        /// Subscribe to a signal.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public void Subscribe<T>(Action<T> handler, object handle = null)
            => Subscribe(handler, handle, false);

        /// <summary>
        /// Subscribe to a signal.
        /// Automatically unsubscribe after the first signal received.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public void SubscribeOnce<T>(Action<T> handler, object handle = null)
            => Subscribe(handler, handle, true);

        private void Subscribe<T>(Action<T> handler, object handle = null, bool oneshot = false)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            SignalHandleObserver.TryAddObserver(handle);

            GetSubscriberCollection<T>().Add(handler, handle, oneshot);
        }

        /// <summary>
        /// Returns the number of active subscribers of this signal.
        /// </summary>
        /// <typeparam name="T">The signal type</typeparam>
        public int Count<T>()
        {
            Type type = typeof(T);
            if (_subscriberCollections.ContainsKey(type) == false)
            {
                // There is no subscriber for this signal
                return 0;
            }
            return _subscriberCollections[type].Count();
        }

        /// <summary>
        /// Unsubscribe from a signal.
        /// </summary>
        /// <param name="handler">The subscribed handler method</param>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            GetSubscriberCollection<T>().RemoveAll(handler);
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

            foreach (var collection in _subscriberCollections.Values)
            {
                collection.RemoveAll(handle);
            }
        }

        /// <summary>
        /// Unsubscribe all subscribers from a signal.
        /// </summary>
        public void UnsubscribeAll<T>()
            => GetSubscriberCollection<T>().RemoveAll();

        /// <summary>
        /// Publish a signal.
        /// </summary>
        /// <param name="signal">The signal instance</param>
        public void Publish<T>(T signal = default)
        {
            GetSubscriberCollection<T>().Publish(signal);
        }

        /// <summary>
        /// Publish a signal asynchronously.
        /// </summary>
        /// <param name="signal">The signal instance</param>
        public async Task PublishAsync<T>(T signal = default)
        {
            await GetSubscriberCollection<T>().PublishAsync(signal);
        }

        private ISubscriberCollection GetSubscriberCollection<T>()
        {
            var type = typeof(T);
            if (!_subscriberCollections.ContainsKey(type))
            {
                _subscriberCollections[type] = new SubscriberCollection<T>();
            }
            return _subscriberCollections[type];
        }
    }
}
