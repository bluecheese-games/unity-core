//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using NUnit.Framework;
using System;
using System.Collections.Generic;
using BlueCheese.Core.Signals;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading.Tasks;

namespace BlueCheese.Tests.Signals
{
    public class Tests_SignalChannel
    {
        private SignalChannel _channel;

        [SetUp]
        public void Setup()
        {
            _channel = new SignalChannel();
        }

        // -------------------------------------------------------------------------
        // Subscribe
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Subscribe()
        {
            // Arrange
            int value = 0;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; });

            // Act & Assert - handler keeps receiving on each publish
            _channel.Publish(new TestSignal() { Value = 99 });
            Assert.That(value, Is.EqualTo(99));

            _channel.Publish(new TestSignal() { Value = 55 });
            Assert.That(value, Is.EqualTo(55));
        }

        [Test]
        public void Test_Subscribe_SeveralHandlers()
        {
            // Arrange
            int value1 = -1;
            int value2 = -1;
            _channel.Subscribe((TestSignal signal) => { value1 = signal.Value; });
            _channel.Subscribe((TestSignal signal) => { value2 = signal.Value; });

            // Act
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert
            Assert.That(value1, Is.EqualTo(99));
            Assert.That(value2, Is.EqualTo(99));
        }

        [Test]
        public void Test_Subscribe_NullCheck()
        {
            // Arrange / Act / Assert
            Assert.Throws<ArgumentNullException>(() => _channel.Subscribe<TestSignal>((Action<TestSignal>)null));
        }

        [Test]
        public void Test_Subscribe_Async_NullCheck()
        {
            // Arrange / Act / Assert
            Assert.Throws<ArgumentNullException>(() => _channel.Subscribe<TestSignal>((Func<TestSignal, UniTask>)null));
        }

        // -------------------------------------------------------------------------
        // SubscribeOnce
        // -------------------------------------------------------------------------

        [Test]
        public void Test_SubscribeOnce()
        {
            // Arrange
            int count = 0;
            _channel.SubscribeOnce((TestSignal signal) => { count++; });

            // Act
            _channel.Publish(new TestSignal());
            _channel.Publish(new TestSignal());

            // Assert
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_SubscribeOnce_Async()
        {
            // Arrange
            int count = 0;
            _channel.SubscribeOnce(async (TestSignal signal) =>
            {
                await UniTask.Yield();
                count++;
            });

            // Act
            await _channel.PublishAsync(new TestSignal());
            await _channel.PublishAsync(new TestSignal());

            // Assert
            Assert.That(count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------------
        // Publish
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Publish_NoSubscribers()
        {
            // Arrange - no subscribers registered

            // Act / Assert - must not throw
            Assert.DoesNotThrow(() => _channel.Publish(new TestSignal() { Value = 99 }));
        }

        [Test]
        public void Test_Publish_NullSignal()
        {
            // Arrange
            bool received = false;
            TestSignal receivedSignal = null;
            _channel.Subscribe((TestSignal signal) => { received = true; receivedSignal = signal; });

            // Act
            _channel.Publish<TestSignal>();

            // Assert
            Assert.That(received, Is.True);
            Assert.That(receivedSignal, Is.Null);
        }

        [Test]
        public void Test_Publish_ClassSignal()
        {
            // Arrange
            TestSignal signalToSend = new TestSignal() { Value = 99 };
            TestSignal receivedSignal = null;
            _channel.Subscribe((TestSignal signal) => { receivedSignal = signal; });

            // Act
            _channel.Publish(signalToSend);

            // Assert - class signals are passed by reference
            Assert.That(ReferenceEquals(receivedSignal, signalToSend), Is.True);
            Assert.That(receivedSignal.Value, Is.EqualTo(99));
        }

        [Test]
        public void Test_Publish_StructSignal()
        {
            // Arrange
            TestSignalStruct signalToSend = new TestSignalStruct() { Value = 99 };
            TestSignalStruct receivedSignal = default;
            _channel.Subscribe((TestSignalStruct signal) => { receivedSignal = signal; });

            // Act
            _channel.Publish(signalToSend);

            // Assert - struct signals are copied by value
            Assert.That(ReferenceEquals(receivedSignal, signalToSend), Is.False);
            Assert.That(receivedSignal.Value, Is.EqualTo(99));
        }

        [Test]
        public void Test_HandlersPriority()
        {
            // Arrange
            var callOrder = new List<int>();
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(1); });
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(2); });
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(3); });

            // Act
            _channel.Publish(new TestSignal());

            // Assert - handlers execute in subscription order (FIFO)
            Assert.That(callOrder, Is.EqualTo(new List<int>() { 1, 2, 3 }));
        }

        [Test]
        public void Test_MutateSignalData_Class()
        {
            // Arrange
            int value = 0;
            _channel.Subscribe((TestSignal signal) => { signal.Value = 55; });
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; });

            // Act
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert - class signal mutation is visible to subsequent handlers
            Assert.That(value, Is.EqualTo(55));
        }

        [Test]
        public void Test_MutateSignalData_Struct()
        {
            // Arrange
            int value = 0;
            _channel.Subscribe((TestSignalStruct signal) => { signal.Value = 55; });
            _channel.Subscribe((TestSignalStruct signal) => { value = signal.Value; });

            // Act
            _channel.Publish(new TestSignalStruct() { Value = 99 });

            // Assert - struct signal is copied per handler, mutation is not visible to subsequent handlers
            Assert.That(value, Is.EqualTo(99));
        }

        // -------------------------------------------------------------------------
        // PublishAsync
        // -------------------------------------------------------------------------

        [Test]
        public async Task Test_PublishAsync()
        {
            // Arrange
            int value = 0;
            _channel.Subscribe(async (TestSignal signal) =>
            {
                Assert.That(value, Is.EqualTo(0));
                await UniTask.Yield();
                value = 1;
            });
            _channel.Subscribe(async (TestSignal signal) =>
            {
                Assert.That(value, Is.EqualTo(1));
                await UniTask.Yield();
                value = 2;
            });

            // Act
            await _channel.PublishAsync(new TestSignal());

            // Assert - async handlers execute sequentially
            Assert.That(value, Is.EqualTo(2));
        }

        [Test]
        public async Task Test_PublishAsync_WithNonAsyncSubscriber()
        {
            // Arrange
            int value = 0;
            _channel.Subscribe((TestSignal signal) => { value = 1; });
            _channel.Subscribe(async (TestSignal signal) =>
            {
                Assert.That(value, Is.EqualTo(1));
                await UniTask.Yield();
                value = 2;
            });

            // Act
            await _channel.PublishAsync(new TestSignal());

            // Assert - sync and async handlers execute sequentially in subscription order
            Assert.That(value, Is.EqualTo(2));
        }

        [Test]
        public async Task Test_PublishAsync_OnMainThread()
        {
            // Arrange
            bool complete = false;
            GameObject obj = null;
            _channel.Subscribe((TestSignal signal) =>
            {
                obj = new GameObject();
                complete = true;
                return UniTask.CompletedTask;
            });

            // Act
            await _channel.PublishAsync(new TestSignal());

            // Assert
            Assert.That(complete, Is.True);
            Assert.That(obj, Is.Not.Null);
        }

        [Test]
        public async Task Test_PublishAsync_SubscribeOnce()
        {
            // Arrange
            int count = 0;
            _channel.SubscribeOnce(async (TestSignal signal) =>
            {
                await UniTask.Yield();
                count++;
            });

            // Act
            await _channel.PublishAsync(new TestSignal());
            await _channel.PublishAsync(new TestSignal());

            // Assert - one-shot handler fires only once even with async publish
            Assert.That(count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------------
        // Unsubscribe
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Unsubscribe()
        {
            // Arrange
            int value = -1;
            Action<TestSignal> handler = (TestSignal signal) => { value = signal.Value; };
            _channel.Subscribe(handler);

            // Act
            _channel.Unsubscribe(handler);
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert
            Assert.That(value, Is.EqualTo(-1));
        }

        [Test]
        public void Test_Unsubscribe_SomeHandlers()
        {
            // Arrange
            var callOrder = new List<int>();
            Action<TestSignal> handler1 = (TestSignal signal) => { callOrder.Add(1); };
            Action<TestSignal> handler2 = (TestSignal signal) => { callOrder.Add(2); };
            Action<TestSignal> handler3 = (TestSignal signal) => { callOrder.Add(3); };
            _channel.Subscribe(handler1);
            _channel.Subscribe(handler2);
            _channel.Subscribe(handler3);

            // Act
            _channel.Unsubscribe(handler2);
            _channel.Publish(new TestSignal());

            // Assert - remaining handlers execute in original subscription order
            Assert.That(callOrder, Is.EqualTo(new List<int>() { 1, 3 }));
        }

        [Test]
        public void Test_Unsubscribe_NullCheck()
        {
            // Arrange / Act / Assert
            Assert.Throws<ArgumentNullException>(() => _channel.Unsubscribe<TestSignal>(null));
        }

        [Test]
        public void Test_Unsubscribe_NotSubscribedHandler()
        {
            // Arrange
            Action<TestSignal> handler = (TestSignal signal) => { };

            // Act / Assert - must not throw
            Assert.DoesNotThrow(() => _channel.Unsubscribe(handler));
        }

        [Test]
        public void Test_Unsubscribe_Using_Handle()
        {
            // Arrange
            var handle = new object();
            _channel.Subscribe((TestSignal signal) => { Assert.Fail("Handler should be unsubscribed"); }, handle);
            _channel.Subscribe((TestSignal signal) => { Assert.Fail("Handler should be unsubscribed"); }, handle);

            // Act
            _channel.Unsubscribe(handle);
            _channel.Publish(new TestSignal());

            // Assert - implicit: no Assert.Fail reached
        }

        [Test]
        public void Test_Unsubscribe_Handle_NullCheck()
        {
            // Arrange / Act / Assert
            Assert.Throws<ArgumentNullException>(() => _channel.Unsubscribe((object)null));
        }

        // -------------------------------------------------------------------------
        // UnsubscribeAll
        // -------------------------------------------------------------------------

        [Test]
        public void Test_UnsubscribeAll()
        {
            // Arrange
            int value = -1;
            int onceValue = -1;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; });
            _channel.SubscribeOnce((TestSignal signal) => { onceValue = signal.Value; });

            // Act
            _channel.UnsubscribeAll<TestSignal>();
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert
            Assert.That(value, Is.EqualTo(-1));
            Assert.That(onceValue, Is.EqualTo(-1));
        }

        [Test]
        public void Test_UnsubscribeAll_DoesNotAffectOtherSignals()
        {
            // Arrange
            int testSignalValue = -1;
            int structSignalValue = -1;
            _channel.Subscribe((TestSignal signal) => { testSignalValue = signal.Value; });
            _channel.Subscribe((TestSignalStruct signal) => { structSignalValue = signal.Value; });

            // Act
            _channel.UnsubscribeAll<TestSignal>();
            _channel.Publish(new TestSignal() { Value = 99 });
            _channel.Publish(new TestSignalStruct() { Value = 42 });

            // Assert - TestSignal handler removed, TestSignalStruct handler untouched
            Assert.That(testSignalValue, Is.EqualTo(-1));
            Assert.That(structSignalValue, Is.EqualTo(42));
        }

        // -------------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Reset()
        {
            // Arrange
            int value = -1;
            int onceValue = -1;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; });
            _channel.SubscribeOnce((TestSignal signal) => { onceValue = signal.Value; });

            // Act
            _channel.Reset();
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert
            Assert.That(value, Is.EqualTo(-1));
            Assert.That(onceValue, Is.EqualTo(-1));
        }

        [Test]
        public void Test_Reset_MultipleSignalTypes()
        {
            // Arrange
            int value1 = -1;
            int value2 = -1;
            _channel.Subscribe((TestSignal signal) => { value1 = signal.Value; });
            _channel.Subscribe((TestSignalStruct signal) => { value2 = signal.Value; });

            // Act
            _channel.Reset();
            _channel.Publish(new TestSignal() { Value = 99 });
            _channel.Publish(new TestSignalStruct() { Value = 99 });

            // Assert - Reset clears all signal types
            Assert.That(value1, Is.EqualTo(-1));
            Assert.That(value2, Is.EqualTo(-1));
        }

        // -------------------------------------------------------------------------
        // Count
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Count_NoSubscribers()
        {
            // Arrange - no subscriptions

            // Act / Assert
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(0));
        }

        [Test]
        public void Test_Count_AfterSubscribeAndUnsubscribe()
        {
            // Arrange
            Action<TestSignal> handler1 = (TestSignal signal) => { };
            Action<TestSignal> handler2 = (TestSignal signal) => { };

            // Act & Assert
            _channel.Subscribe(handler1);
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));

            _channel.Subscribe(handler2);
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(2));

            _channel.Unsubscribe(handler1);
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));

            _channel.Unsubscribe(handler2);
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(0));
        }

        // -------------------------------------------------------------------------
        // Reentrancy during publish
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Subscribe_WhileReceivingSignal()
        {
            // Arrange
            Action<TestSignal> handler2 = (TestSignal signal) => { Assert.Fail("Handler subscribed during publish must not fire in same publish"); };
            _channel.Subscribe((TestSignal signal) => { _channel.Subscribe(handler2); });

            // Act
            _channel.Publish(new TestSignal());

            // Assert - late-subscribed handler is registered but not invoked in the current publish
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(2));
        }

        [Test]
        public void Test_Unsubscribe_WhileReceivingSignal()
        {
            // Arrange
            int value = 0;
            Action<TestSignal> handler2 = (TestSignal signal) => { value = signal.Value; };
            _channel.Subscribe((TestSignal signal) => { _channel.Unsubscribe(handler2); });
            _channel.Subscribe(handler2);

            // Act
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert - handler unsubscribed during publish still fires in the current publish (snapshot semantics)
            Assert.That(value, Is.EqualTo(99));
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));
        }

        [Test]
        public void Test_Unsubscribe_Handle_WhileReceivingSignal()
        {
            // Arrange
            var handle = new object();
            int value = 0;
            _channel.Subscribe((TestSignal signal) => { _channel.Unsubscribe(handle); });
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; }, handle);

            // Act
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert - handle unsubscribed during publish still fires in the current publish (snapshot semantics)
            Assert.That(value, Is.EqualTo(99));
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));
        }

        [Test]
        public void Test_SubscribeOnce_WhileReceivingSignal()
        {
            // Arrange
            int value = 0;
            Action<TestSignal> handler = null;
            Action<TestSignal> handler2 = (TestSignal signal) => { value = signal.Value; };
            handler = (TestSignal signal) =>
            {
                value = signal.Value;
                _channel.SubscribeOnce(handler2);
            };
            _channel.Subscribe(handler);

            // Act
            _channel.Publish(new TestSignal() { Value = 66 });
            _channel.Unsubscribe(handler);
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert - one-shot handler fires once on second publish then unsubscribes
            Assert.That(value, Is.EqualTo(99));
        }

        [Test]
        public void Test_Reset_WhileReceivingSignal()
        {
            // Arrange - two handlers subscribed; first calls Reset()
            int secondHandlerCallCount = 0;
            _channel.Subscribe((TestSignal signal) => { _channel.Reset(); });
            _channel.Subscribe((TestSignal signal) => { secondHandlerCallCount++; });

            // Act
            _channel.Publish(new TestSignal());

            // Assert - snapshot semantics: second handler still fires in the current publish,
            // but a subsequent publish fires nothing (Reset took effect)
            Assert.That(secondHandlerCallCount, Is.EqualTo(1));
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(0));

            _channel.Publish(new TestSignal());
            Assert.That(secondHandlerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Test_Subscribe_And_Unsubscribe_WhileReceivingSignal()
        {
            // Arrange
            Action<TestSignal> handler2 = (TestSignal signal) => { Assert.Fail("Immediately unsubscribed handler must not be invoked"); };
            _channel.Subscribe((TestSignal signal) =>
            {
                _channel.Subscribe(handler2);
                _channel.Unsubscribe(handler2);
            });

            // Act
            _channel.Publish(new TestSignal());

            // Assert
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));
        }

        [Test]
        public void Test_Subscribe_And_UnsubscribeAll_WhileReceivingSignal()
        {
            // Arrange
            Action<TestSignal> handler2 = (TestSignal signal) => { Assert.Fail("Handler added then UnsubscribeAll'd must not be invoked"); };
            _channel.Subscribe((TestSignal signal) =>
            {
                _channel.Subscribe(handler2);
                _channel.UnsubscribeAll<TestSignal>();
            });

            // Act
            _channel.Publish(new TestSignal());

            // Assert
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(0));
        }

        // -------------------------------------------------------------------------
        // Priority
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Priority_HandlersExecuteInPriorityOrder()
        {
            // Arrange
            var callOrder = new List<int>();
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(1); }).WithPriority(10);
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(2); });           // priority 0
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(3); }).WithPriority(20);

            // Act
            _channel.Publish(new TestSignal());

            // Assert - highest priority runs first
            Assert.That(callOrder, Is.EqualTo(new List<int>() { 3, 1, 2 }));
        }

        [Test]
        public void Test_Priority_EqualPriorityPreservesSubscriptionOrder()
        {
            // Arrange
            var callOrder = new List<int>();
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(1); }).WithPriority(5);
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(2); }).WithPriority(5);
            _channel.Subscribe((TestSignal signal) => { callOrder.Add(3); }).WithPriority(5);

            // Act
            _channel.Publish(new TestSignal());

            // Assert - equal priorities preserve FIFO order
            Assert.That(callOrder, Is.EqualTo(new List<int>() { 1, 2, 3 }));
        }

        // -------------------------------------------------------------------------
        // Cancellation
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Cancel_StopsSignalPropagation()
        {
            // Arrange
            int callCount = 0;
            _channel.Subscribe((TestSignal signal, SignalContext ctx) => { callCount++; ctx.Cancel(); });
            _channel.Subscribe((TestSignal signal) => { callCount++; });

            // Act
            var ctx = _channel.Publish(new TestSignal());

            // Assert - second handler never runs
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(ctx.IsCancelled, Is.True);
        }

        [Test]
        public void Test_Cancel_HigherPriorityCancellerBlocksLowerPriority()
        {
            // Arrange
            int callCount = 0;
            _channel.Subscribe((TestSignal signal) => { callCount++; });                        // priority 0
            _channel.Subscribe((TestSignal signal, SignalContext ctx) => { ctx.Cancel(); })     // runs first
                .WithPriority(10);

            // Act
            var ctx = _channel.Publish(new TestSignal());

            // Assert - canceller fires first, normal handler is skipped
            Assert.That(callCount, Is.EqualTo(0));
            Assert.That(ctx.IsCancelled, Is.True);
        }

        [Test]
        public void Test_Cancel_DoesNotPersistAcrossPublishes()
        {
            // Arrange
            int callCount = 0;
            int publishCount = 0;
            _channel.Subscribe((TestSignal signal, SignalContext ctx) =>
            {
                publishCount++;
                if (publishCount == 1) ctx.Cancel(); // only cancel on first publish
            });
            _channel.Subscribe((TestSignal signal) => { callCount++; });

            // Act
            _channel.Publish(new TestSignal());     // first publish: cancellation skips second handler
            _channel.Publish(new TestSignal());     // second publish: fresh context, second handler runs

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_Cancel_Async_StopsSignalPropagation()
        {
            // Arrange
            int callCount = 0;
            _channel.Subscribe(async (TestSignal signal, SignalContext ctx) =>
            {
                await UniTask.Yield();
                callCount++;
                ctx.Cancel();
            });
            _channel.Subscribe(async (TestSignal signal) =>
            {
                await UniTask.Yield();
                callCount++;
            });

            // Act
            var ctx = await _channel.PublishAsync(new TestSignal());

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(ctx.IsCancelled, Is.True);
        }

        // -------------------------------------------------------------------------
        // Fluent builder
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Builder_Once()
        {
            // Arrange
            int count = 0;
            _channel.Subscribe((TestSignal signal) => { count++; }).Once();

            // Act
            _channel.Publish(new TestSignal());
            _channel.Publish(new TestSignal());

            // Assert - fires only once, identical to SubscribeOnce
            Assert.That(count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------------
        // Sticky
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Sticky_ReplaysLastSignalImmediately()
        {
            // Arrange
            _channel.Publish(new TestSignal() { Value = 42 });

            // Act
            int value = -1;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; }).Sticky();

            // Assert - handler was called immediately with the last published value
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Test_Sticky_NoReplayIfNothingPublished()
        {
            // Arrange - no publish before subscribe

            // Act
            int callCount = 0;
            _channel.Subscribe((TestSignal signal) => { callCount++; }).Sticky();

            // Assert
            Assert.That(callCount, Is.EqualTo(0));
        }

        [Test]
        public void Test_Sticky_AlsoReceivesFutureSignals()
        {
            // Arrange
            _channel.Publish(new TestSignal() { Value = 1 });

            int value = -1;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; }).Sticky();
            Assert.That(value, Is.EqualTo(1)); // replayed immediately

            // Act
            _channel.Publish(new TestSignal() { Value = 2 });

            // Assert - also receives subsequent publishes
            Assert.That(value, Is.EqualTo(2));
        }

        [Test]
        public void Test_Sticky_RespectsFilter()
        {
            // Arrange
            _channel.Publish(new TestSignal() { Value = 5 });

            int value = -1;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; })
                .When(s => s.Value > 10)
                .Sticky();

            // Assert - replayed signal does not pass the filter, handler not called
            Assert.That(value, Is.EqualTo(-1));
        }

        [Test]
        public void Test_Sticky_Once_UnsubscribesAfterReplay()
        {
            // Arrange
            _channel.Publish(new TestSignal() { Value = 99 });

            int callCount = 0;
            _channel.Subscribe((TestSignal signal) => { callCount++; })
                .Once()
                .Sticky();

            // Act - replay consumed the one-shot; next publish should not fire
            _channel.Publish(new TestSignal() { Value = 42 });

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------------
        // Filter
        // -------------------------------------------------------------------------

        [Test]
        public void Test_Filter_HandlerInvokedWhenPredicateMatches()
        {
            // Arrange
            int value = -1;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; })
                .When(s => s.Value > 10);

            // Act
            _channel.Publish(new TestSignal() { Value = 99 });

            // Assert
            Assert.That(value, Is.EqualTo(99));
        }

        [Test]
        public void Test_Filter_HandlerNotInvokedWhenPredicateDoesNotMatch()
        {
            // Arrange
            int value = -1;
            _channel.Subscribe((TestSignal signal) => { value = signal.Value; })
                .When(s => s.Value > 10);

            // Act
            _channel.Publish(new TestSignal() { Value = 5 });

            // Assert
            Assert.That(value, Is.EqualTo(-1));
        }

        [Test]
        public void Test_Filter_OnlyMatchingHandlersReceiveSignal()
        {
            // Arrange
            var received = new List<int>();
            _channel.Subscribe((TestSignal signal) => { received.Add(signal.Value); })
                .When(s => s.Value % 2 == 0); // even only

            // Act
            _channel.Publish(new TestSignal() { Value = 1 });
            _channel.Publish(new TestSignal() { Value = 2 });
            _channel.Publish(new TestSignal() { Value = 3 });
            _channel.Publish(new TestSignal() { Value = 4 });

            // Assert
            Assert.That(received, Is.EqualTo(new List<int>() { 2, 4 }));
        }

        // -------------------------------------------------------------------------
        // Test signals
        // -------------------------------------------------------------------------

        private class TestSignal
        {
            public int Value = 0;
        }

        private struct TestSignalStruct
        {
            public int Value;
        }
    }
}
