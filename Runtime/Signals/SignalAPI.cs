//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.Core.Signals
{
    /// <summary>
    /// The SignalAPI main purpose is to provide a simple API to subscribe and publish signals.
    /// Main features are:
    /// - Subscribe/Unsubscribe to any signal without reference to any other system
    /// - Subscribe once to auto unsubscribe after the first handled signal
    /// - Handlers can be prioritized via .WithPriority() on the returned builder
    /// - Handlers can cancel signal propagation by accepting a SignalContext parameter
    /// - Signals can be isolated in channels
    /// </summary>
    public static class SignalAPI
    {
        private static readonly SignalChannel _defaultChannel = new SignalChannel();

        /// <summary>
        /// Reference to the default channel.
        /// </summary>
        public static SignalChannel Default => _defaultChannel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
            => _defaultChannel.Reset();

        // --- Subscribe (sync) ---

        public static SubscriptionBuilder<T> Subscribe<T>(Action<T> handler, object handle = null)
            => _defaultChannel.Subscribe(handler, handle);

        public static SubscriptionBuilder<T> SubscribeOnce<T>(Action<T> handler, object handle = null)
            => _defaultChannel.SubscribeOnce(handler, handle);

        // --- Subscribe (async) ---

        public static SubscriptionBuilder<T> Subscribe<T>(Func<T, UniTask> handler, object handle = null)
            => _defaultChannel.Subscribe(handler, handle);

        public static SubscriptionBuilder<T> SubscribeOnce<T>(Func<T, UniTask> handler, object handle = null)
            => _defaultChannel.SubscribeOnce(handler, handle);

        // --- Subscribe (sync, cancellable) ---

        public static SubscriptionBuilder<T> Subscribe<T>(Action<T, SignalContext> handler, object handle = null)
            => _defaultChannel.Subscribe(handler, handle);

        public static SubscriptionBuilder<T> SubscribeOnce<T>(Action<T, SignalContext> handler, object handle = null)
            => _defaultChannel.SubscribeOnce(handler, handle);

        // --- Subscribe (async, cancellable) ---

        public static SubscriptionBuilder<T> Subscribe<T>(Func<T, SignalContext, UniTask> handler, object handle = null)
            => _defaultChannel.Subscribe(handler, handle);

        public static SubscriptionBuilder<T> SubscribeOnce<T>(Func<T, SignalContext, UniTask> handler, object handle = null)
            => _defaultChannel.SubscribeOnce(handler, handle);

        // --- Count ---

        public static int Count<T>()
            => _defaultChannel.Count<T>();

        // --- Unsubscribe ---

        public static void Unsubscribe<T>(Action<T> handler)
            => _defaultChannel.Unsubscribe<T>(handler);

        public static void Unsubscribe(object handle)
            => _defaultChannel.Unsubscribe(handle);

        public static void UnsubscribeAll<T>()
            => _defaultChannel.UnsubscribeAll<T>();

        // --- Publish ---

        /// <summary>
        /// Publishes a signal synchronously. Returns the context, which may be checked for cancellation.
        /// </summary>
        public static SignalContext Publish<T>(T signal = default)
            => _defaultChannel.Publish(signal);

        /// <summary>
        /// Publishes a signal asynchronously. Returns the context, which may be checked for cancellation.
        /// </summary>
        public static async UniTask<SignalContext> PublishAsync<T>(T signal = default)
            => await _defaultChannel.PublishAsync(signal);
    }
}
