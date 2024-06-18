//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using NUnit.Framework;
using System;
using System.Collections.Generic;
using Core.Signals;
using System.Threading.Tasks;

namespace Tests.Signals
{
    public class Tests
    {
        private SignalChannel _channel;

        [SetUp]
        public void Setup()
        {
            _channel = new SignalChannel();
        }

        [Test]
        public void Test_Subscribe()
        {
            int value = 0;
            _channel.Subscribe((TestSignal signal) =>
            {
                value = signal.Value;
            });
            Assert.That(value, Is.EqualTo(0));
            _channel.Publish(new TestSignal() { Value = 99 });
            Assert.That(value, Is.EqualTo(99));
            _channel.Publish(new TestSignal() { Value = 55 });
            Assert.That(value, Is.EqualTo(55));
        }

        [Test]
        public void Test_SubscribeOnce()
        {
            int count = 0;
            _channel.SubscribeOnce((TestSignal signal) =>
            {
                count++;
            });
            _channel.Publish(new TestSignal());
            Assert.That(count, Is.EqualTo(1));
            _channel.Publish(new TestSignal());
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Test_Subscribe_SeveralHandlers()
        {
            int value1 = -1;
            int value2 = -1;
            _channel.Subscribe((TestSignal signal) =>
            {
                value1 = signal.Value;
            });
            _channel.Subscribe((TestSignal signal) =>
            {
                value2 = signal.Value;
            });
            _channel.Publish(new TestSignal() { Value = 99 });
            Assert.That(value1, Is.EqualTo(99));
            Assert.That(value2, Is.EqualTo(99));
        }

        [Test]
        public void Test_Publish_NullSignal()
        {
            bool signalHasBeenReceived = false;
            TestSignal receivedSignal = null;
            _channel.Subscribe((TestSignal signal) =>
            {
                signalHasBeenReceived = true;
                receivedSignal = signal;
            });
            _channel.Publish<TestSignal>();
            Assert.That(signalHasBeenReceived, Is.True);
            Assert.That(receivedSignal, Is.Null);
        }

        [Test]
        public void Test_Publish_ClassSignal()
        {
            TestSignal signalToSend = new TestSignal() { Value = 99 };
            TestSignal receivedSignal = null;
            _channel.Subscribe((TestSignal signal) =>
            {
                receivedSignal = signal;
            });
            _channel.Publish(signalToSend);

            Assert.That(ReferenceEquals(receivedSignal, signalToSend), Is.True);
            Assert.That(receivedSignal.Value, Is.EqualTo(signalToSend.Value));
        }

        [Test]
        public void Test_Publish_StructSignal()
        {
            TestSignalStruct signalToSend = new TestSignalStruct() { Value = 99 };
            TestSignalStruct receivedSignal = default;
            _channel.Subscribe((TestSignalStruct signal) =>
            {
                receivedSignal = signal;
            });
            _channel.Publish(signalToSend);

            Assert.That(ReferenceEquals(receivedSignal, signalToSend), Is.False);
            Assert.That(receivedSignal.Value, Is.EqualTo(signalToSend.Value));
        }

        [Test]
        public void Test_HandlersPriority()
        {
            List<int> handlerCallOrder = new List<int>();
            _channel.Subscribe((TestSignal signal) =>
            {
                handlerCallOrder.Add(1);
            });
            _channel.Subscribe((TestSignal signal) =>
            {
                handlerCallOrder.Add(2);
            });
            _channel.Subscribe((TestSignal signal) =>
            {
                handlerCallOrder.Add(3);
            });
            _channel.Publish(new TestSignal());
            Assert.That(handlerCallOrder, Is.EquivalentTo(new List<int>() { 1, 2, 3 }));
        }

        [Test]
        public void Test_Unsubscribe()
        {
            int value = -1;
            SignalHandler<TestSignal> handler = (TestSignal signal) =>
            {
                value = signal.Value;
            };
            _channel.Subscribe(handler);
            _channel.Unsubscribe(handler);
            _channel.Publish(new TestSignal() { Value = 99 });
            Assert.That(value, Is.EqualTo(-1));
        }

        [Test]
        public void Test_Unsubscribe_SomeHandlers()
        {
            List<int> handlerCallOrder = new List<int>();
            SignalHandler<TestSignal> handler1 = (TestSignal signal) =>
            {
                handlerCallOrder.Add(1);
            };
            _channel.Subscribe(handler1);
            SignalHandler<TestSignal> handler2 = (TestSignal signal) =>
            {
                handlerCallOrder.Add(2);
            };
            _channel.Subscribe(handler2);
            SignalHandler<TestSignal> handler3 = (TestSignal signal) =>
            {
                handlerCallOrder.Add(3);
            };
            _channel.Subscribe(handler3);

            _channel.Unsubscribe(handler2);

            _channel.Publish(new TestSignal());
            Assert.That(handlerCallOrder, Is.EquivalentTo(new List<int>() { 1, 3 }));
        }

        [Test]
        public void Test_Subscribe_NullCheck()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _channel.Subscribe<TestSignal>(null);
            });
        }

        [Test]
        public void Test_Unsubscribe_NullCheck()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _channel.Unsubscribe<TestSignal>(null);
            });
        }

        [Test]
        public void Test_Unsubscribe_NotSubscribedHandler()
        {
            SignalHandler<TestSignal> handler = (TestSignal signal) => { };
            _channel.Unsubscribe(handler);

            Assert.Pass();
        }

        [Test]
        public void Test_UnsubscribeAll()
        {
            int value = -1;
            int onceValue = -1;
            _channel.Subscribe((TestSignal signal) =>
            {
                value = signal.Value;
            });
            _channel.SubscribeOnce((TestSignal signal) =>
            {
                onceValue = signal.Value;
            });

            _channel.UnsubscribeAll<TestSignal>();

            _channel.Publish(new TestSignal() { Value = 99 });
            Assert.That(value, Is.EqualTo(-1));
            Assert.That(onceValue, Is.EqualTo(-1));
        }

        [Test]
        public void Test_Reset()
        {
            int value = -1;
            int onceValue = -1;
            _channel.Subscribe((TestSignal signal) =>
            {
                value = signal.Value;
            });
            _channel.SubscribeOnce((TestSignal signal) =>
            {
                onceValue = signal.Value;
            });

            _channel.Reset();

            _channel.Publish(new TestSignal() { Value = 99 });
            Assert.That(value, Is.EqualTo(-1));
            Assert.That(onceValue, Is.EqualTo(-1));
        }

        [Test]
        public void Test_MutateSignalData()
        {
            int value = 0;
            _channel.Subscribe((TestSignal signal) =>
            {
                // First subscriber mutate signal data
                signal.Value = 55;
            });
            _channel.Subscribe((TestSignal signal) =>
            {
                // Second subscriber read signal data
                value = signal.Value;
            });

            _channel.Publish(new TestSignal() { Value = 99 });
            Assert.That(value, Is.EqualTo(55));
        }

        [Test]
        public void Test_Subscribe_WhileReceivingSignal()
        {
            SignalHandler<TestSignal> handler = null;
            SignalHandler<TestSignal> handler2 = null;
            handler = (TestSignal signal) =>
            {
                _channel.Subscribe(handler2);
            };
            handler2 = (TestSignal signal) =>
            {
                Assert.Fail("This handler should not be invoked");
            };

            _channel.Subscribe(handler);

            _channel.Publish(new TestSignal());

            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(2));
        }

        [Test]
        public void Test_Reset_WhileReceivingSignal()
        {
            SignalHandler<TestSignal> handler = null;
            SignalHandler<TestSignal> handler2 = null;
            handler = (TestSignal signal) =>
            {
                Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));
                _channel.Reset();
            };
            handler2 = (TestSignal signal) =>
            {
                Assert.Fail("This handler should not be invoked");
            };

            _channel.Subscribe(handler);

            _channel.Publish(new TestSignal());

            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(0));
        }

        [Test]
        public void Test_SubscribeOnce_WhileReceivingSignal()
        {
            int value = 0;
            SignalHandler<TestSignal> handler = null;
            SignalHandler<TestSignal> handler2 = null;
            handler = (TestSignal signal) =>
            {
                value = signal.Value;
                _channel.SubscribeOnce(handler2);
            };
            handler2 = (TestSignal signal) =>
            {
                value = signal.Value;
            };

            _channel.Subscribe(handler);

            _channel.Publish(new TestSignal() { Value = 66 });

            Assert.That(value, Is.EqualTo(66));

            _channel.Unsubscribe(handler);

            _channel.Publish(new TestSignal() { Value = 99 });

            Assert.That(value, Is.EqualTo(99));
        }

        [Test]
        public void Test_Unsubscribe_WhileReceivingSignal()
        {
            int value = 0;
            SignalHandler<TestSignal> handler = null;
            SignalHandler<TestSignal> handler2 = null;
            handler = (TestSignal signal) =>
            {
                _channel.Unsubscribe(handler2);
            };
            handler2 = (TestSignal signal) =>
            {
                value = signal.Value;
            };

            _channel.Subscribe(handler);
            _channel.Subscribe(handler2);

            _channel.Publish(new TestSignal() { Value = 99 });

            Assert.That(value, Is.EqualTo(99));
            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));
        }

        [Test]
        public void Test_Subscribe_And_Unsubscribe_WhileReceivingSignal()
        {
            SignalHandler<TestSignal> handler = null;
            SignalHandler<TestSignal> handler2 = null;
            handler = (TestSignal signal) =>
            {
                _channel.Subscribe(handler2);
                _channel.Unsubscribe(handler2);
            };
            handler2 = (TestSignal signal) =>
            {
                Assert.Fail("This handler should not be invoked");
            };

            _channel.Subscribe(handler);

            _channel.Publish(new TestSignal());

            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(1));
        }

        [Test]
        public void Test_Subscribe_And_UnsubscribeAll_WhileReceivingSignal()
        {
            SignalHandler<TestSignal> handler = null;
            SignalHandler<TestSignal> handler2 = null;
            handler = (TestSignal signal) =>
            {
                _channel.Subscribe(handler2);
                _channel.UnsubscribeAll<TestSignal>();
            };
            handler2 = (TestSignal signal) =>
            {
                Assert.Fail("This handler should not be invoked");
            };

            _channel.Subscribe(handler);

            _channel.Publish(new TestSignal());

            Assert.That(_channel.Count<TestSignal>(), Is.EqualTo(0));
        }

        [Test]
        public void Test_Unsubscribe_Using_Handle()
        {
            var handle = new object();
            _channel.Subscribe((TestSignal signal) =>
            {
                Assert.Fail("Signal handler should be unsubscribed");
            }, handle);
            _channel.Subscribe((TestSignal signal) =>
            {
                Assert.Fail("Signal handler should be unsubscribed");
            }, handle);

            _channel.Unsubscribe(handle);

            _channel.Publish(new TestSignal());
        }

        [Test]
        public async void Test_PublishAsync()
        {
            int value = 0;
            _channel.Subscribe(async (TestSignal signal) =>
            {
                await Task.Yield();
                value = 1;
            });
            _channel.Subscribe(async (TestSignal signal) =>
            {
                await Task.Yield();
                value = 2;
            });

            await _channel.PublishAsync(new TestSignal());

            Assert.That(value, Is.EqualTo(2));
        }

        [Test]
        public async void Test_PublishAsync_WithNonAsyncSubscriber()
        {
            int value = 0;
            _channel.Subscribe((TestSignal signal) =>
            {
                value = 1;
            });
            _channel.Subscribe(async (TestSignal signal) =>
            {
                await Task.Yield();
                value = 2;
            });

            await _channel.PublishAsync(new TestSignal());

            Assert.That(value, Is.EqualTo(2));
        }

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