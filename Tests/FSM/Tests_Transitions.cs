//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM;
using NUnit.Framework;

namespace BlueCheese.Tests.FSM
{
    public class Tests_Transitions
    {
        private StateMachine stateMachine;

        [TearDown]
        public void TearDown() => stateMachine?.Dispose();

        [Test]
        public void Test_Transition_Evaluate_Trigger()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, out var transition, 0, Condition.CreateTriggerCondition("test"))
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.SetTrigger("test");

            // Assert
            Assert.That(transition.Evaluate(stateMachine, out _, out _), Is.True);
        }

        [Test]
        public void Test_Transition_Evaluate_Trigger_WithExitTime()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, out var transition, 1, Condition.CreateTriggerCondition("test"))
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.SetTrigger("test");
            stateMachine.Update(0.5f);
            stateMachine.Update(1f);

            // Assert
            Assert.That(stateMachine.CurrentState.Name, Is.EqualTo("B"));
        }

        [Test]
        public void Test_Transition_Evaluate_Bool()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, out var transition, 0, Condition.CreateBoolCondition("test", true))
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.SetBool("test", true);

            // Assert
            Assert.That(transition.Evaluate(stateMachine, out _, out _), Is.True);
        }

        [Test]
        public void Test_Transition_Evaluate_Int()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, out var transition, 0, Condition.CreateIntCondition("test", Condition.Operator.Equals, 1))
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.SetInt("test", 1);

            // Assert
            Assert.That(transition.Evaluate(stateMachine, out _, out _), Is.True);
        }

        [Test]
        public void Test_Transition_Evaluate_Float()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, out var transition, 0, Condition.CreateFloatCondition("test", Condition.Operator.Equals, 1f))
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.SetFloat("test", 1f);

            // Assert
            Assert.That(transition.Evaluate(stateMachine, out _, out _), Is.True);
        }
    }
}