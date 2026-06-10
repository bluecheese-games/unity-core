//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.Signals
{
    public class SubscriptionBuilder<T>
    {
        private readonly SignalChannel.Subscriber<T> _subscriber;
        private readonly Action _stickyReplay;

        internal SubscriptionBuilder(SignalChannel.Subscriber<T> subscriber, Action stickyReplay)
        {
            _subscriber = subscriber;
            _stickyReplay = stickyReplay;
        }

        /// <summary>
        /// Sets the execution priority. Higher values run first. Default is 0.
        /// Equal priorities preserve subscription order (FIFO).
        /// </summary>
        public SubscriptionBuilder<T> WithPriority(int priority)
        {
            _subscriber.Priority = priority;
            return this;
        }

        /// <summary>
        /// Automatically unsubscribe after the first signal received.
        /// </summary>
        public SubscriptionBuilder<T> Once()
        {
            _subscriber.IsOneShot = true;
            return this;
        }

        /// <summary>
        /// Only invoke the handler when the predicate returns true.
        /// </summary>
        public SubscriptionBuilder<T> When(Func<T, bool> predicate)
        {
            _subscriber.Filter = predicate;
            return this;
        }

        /// <summary>
        /// If a signal of this type has already been published, invoke the handler immediately
        /// with the last published value. Also receives all future signals normally.
        /// </summary>
        public SubscriptionBuilder<T> Sticky()
        {
            _stickyReplay?.Invoke();
            return this;
        }
    }
}
