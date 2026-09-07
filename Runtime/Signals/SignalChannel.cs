using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.Signals
{
    public partial class SignalChannel
    {
        private readonly Dictionary<Type, ISubscriberCollection> _subscriberCollections = new Dictionary<Type, ISubscriberCollection>();

        /// <summary>
        /// Unsubscribe all subscribers from all signals.
        /// </summary>
        public void Reset()
        {
            foreach (var collection in _subscriberCollections.Values)
                collection.RemoveAll();
        }

        // --- Subscribe (sync) ---

        public SubscriptionBuilder<T> Subscribe<T>(Action<T> handler, object handle = null)
            => SubscribeInternal(handler, handle, false);

        public SubscriptionBuilder<T> SubscribeOnce<T>(Action<T> handler, object handle = null)
            => SubscribeInternal(handler, handle, true);

        private SubscriptionBuilder<T> SubscribeInternal<T>(Action<T> handler, object handle, bool oneshot)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            SignalHandleObserver.TryAddObserver(handle);
            var collection = GetSubscriberCollection<T>();
            var subscriber = (Subscriber<T>)collection.Add(handler, handle, oneshot);
            return new SubscriptionBuilder<T>(subscriber, () => collection.ReplayLast(subscriber));
        }

        // --- Subscribe (async) ---

        public SubscriptionBuilder<T> Subscribe<T>(Func<T, UniTask> handler, object handle = null)
            => SubscribeInternal(handler, handle, false);

        public SubscriptionBuilder<T> SubscribeOnce<T>(Func<T, UniTask> handler, object handle = null)
            => SubscribeInternal(handler, handle, true);

        private SubscriptionBuilder<T> SubscribeInternal<T>(Func<T, UniTask> handler, object handle, bool oneshot)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            SignalHandleObserver.TryAddObserver(handle);
            var collection = GetSubscriberCollection<T>();
            var subscriber = (Subscriber<T>)collection.Add(handler, handle, oneshot);
            return new SubscriptionBuilder<T>(subscriber, () => collection.ReplayLast(subscriber));
        }

        // --- Subscribe (sync, cancellable) ---

        public SubscriptionBuilder<T> Subscribe<T>(Action<T, SignalContext> handler, object handle = null)
            => SubscribeInternal(handler, handle, false);

        public SubscriptionBuilder<T> SubscribeOnce<T>(Action<T, SignalContext> handler, object handle = null)
            => SubscribeInternal(handler, handle, true);

        private SubscriptionBuilder<T> SubscribeInternal<T>(Action<T, SignalContext> handler, object handle, bool oneshot)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            SignalHandleObserver.TryAddObserver(handle);
            var collection = GetSubscriberCollection<T>();
            var subscriber = (Subscriber<T>)collection.Add(handler, handle, oneshot);
            return new SubscriptionBuilder<T>(subscriber, () => collection.ReplayLast(subscriber));
        }

        // --- Subscribe (async, cancellable) ---

        public SubscriptionBuilder<T> Subscribe<T>(Func<T, SignalContext, UniTask> handler, object handle = null)
            => SubscribeInternal(handler, handle, false);

        public SubscriptionBuilder<T> SubscribeOnce<T>(Func<T, SignalContext, UniTask> handler, object handle = null)
            => SubscribeInternal(handler, handle, true);

        private SubscriptionBuilder<T> SubscribeInternal<T>(Func<T, SignalContext, UniTask> handler, object handle, bool oneshot)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            SignalHandleObserver.TryAddObserver(handle);
            var collection = GetSubscriberCollection<T>();
            var subscriber = (Subscriber<T>)collection.Add(handler, handle, oneshot);
            return new SubscriptionBuilder<T>(subscriber, () => collection.ReplayLast(subscriber));
        }

        // --- Count ---

        public int Count<T>()
        {
            var type = typeof(T);
            return _subscriberCollections.TryGetValue(type, out var collection) ? collection.Count() : 0;
        }

        // --- Unsubscribe ---

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetSubscriberCollection<T>().RemoveAll(handler);
        }

        public void Unsubscribe(object handle)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            foreach (var collection in _subscriberCollections.Values)
                collection.RemoveAll(handle);
        }

        public void UnsubscribeAll<T>()
            => GetSubscriberCollection<T>().RemoveAll();

        // --- Publish ---

        /// <summary>
        /// Publishes a signal synchronously. Returns the context, which may be checked for cancellation.
        /// </summary>
        public SignalContext Publish<T>(T signal = default)
            => GetSubscriberCollection<T>().Publish(signal);

        /// <summary>
        /// Publishes a signal asynchronously. Returns the context, which may be checked for cancellation.
        /// </summary>
        public async UniTask<SignalContext> PublishAsync<T>(T signal = default)
            => await GetSubscriberCollection<T>().PublishAsync(signal);

        // ---

        private ISubscriberCollection GetSubscriberCollection<T>()
        {
            var type = typeof(T);
            if (!_subscriberCollections.ContainsKey(type))
                _subscriberCollections[type] = new SubscriberCollection<T>();
            return _subscriberCollections[type];
        }
    }
}
