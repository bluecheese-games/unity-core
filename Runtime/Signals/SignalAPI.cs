//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine;

namespace Core.Signals
{
    /// <summary>
    /// The SignalAPI main purpose is to provide a simple API to subscribe and publish signals.
    /// Main features are:
    /// - Subscribe/Unsubscribe to any signal without reference to any other system
    /// - Subscribe once to auto unsubscribe after the first handled signal
    /// - Signals can be isolated in channels
    /// </summary>
    public static class SignalAPI
    {
        private static readonly SignalChannel _defaultChannel = new SignalChannel();

        /// <summary>
        /// Reference to the default channel.
        /// </summary>
        public static SignalChannel Default => _defaultChannel;

        /// <summary>
        /// Reset the default channel.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // Ensure domain reload
        public static void Reset()
            => _defaultChannel.Reset();

        /// <summary>
        /// Subscribe to a signal in the default channel.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public static void Subscribe<T>(SignalHandler<T> handler, object handle = null)
            => _defaultChannel.Subscribe(handler, handle);

        /// <summary>
        /// Subscribe to a signal in the default channel.
        /// Unsubscribe automatically after the first signal received.
        /// </summary>
        /// <param name="handler">The handler method</param>
        /// <param name="handle">The subscribing handle</param>
        public static void SubscribeOnce<T>(SignalHandler<T> handler, object handle = null)
            => _defaultChannel.SubscribeOnce(handler, handle);

        /// <summary>
        /// Returns the number of subscribers of this signal in the default channel.
        /// </summary>
        /// <typeparam name="T">The signal type</typeparam>
        public static int Count<T>()
            => _defaultChannel.Count<T>();

        /// <summary>
        /// Unsubscribe from a signal in the default channel.
        /// </summary>
        /// <param name="handler">The subscribed handler method</param>
        public static void Unsubscribe<T>(SignalHandler<T> handler)
            => _defaultChannel.Unsubscribe<T>(handler);

        /// <summary>
        /// Unsubscribe from a signal in the default channel.
        /// </summary>
        /// <param name="handle">The subscribing handle</param>
        public static void Unsubscribe(object handle)
            => _defaultChannel.Unsubscribe(handle);

        /// <summary>
        /// Unsubscribe all subscribers from a signal in the default channel.
        /// </summary>
        public static void UnsubscribeAll<T>()
            => _defaultChannel.UnsubscribeAll<T>();

        /// <summary>
        /// Publish a signal in the default channel.
        /// </summary>
        /// <param name="signal">The signal instance</param>
        public static void Publish<T>(T signal = default)
            => _defaultChannel.Publish(signal);

        /// <summary>
        /// Publish a signal asynchronously in the default channel.
        /// </summary>
        /// <param name="signal">The signal instance</param>
        public static async Task PublishAsync<T>(T signal = default)
            => await _defaultChannel.PublishAsync(signal);
    }
}
