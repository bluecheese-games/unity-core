//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM;
using NUnit.Framework;
using System;

namespace BlueCheese.Tests.FSM
{
    public class Tests_StateMachine
    {
        private StateMachine stateMachine;

        [TearDown]
        public void TearDown() => stateMachine?.Dispose();

        [Test]
        public void Test_StateMachine_Build()
        {
            // Arrange / Act
            stateMachine = new StateMachine.Builder()
                .AddState(new MockState("A"), true)
                .Build();

            // Assert
            Assert.That(stateMachine, Is.Not.Null);
            Assert.That(stateMachine.IsStarted, Is.False);
        }

        [Test]
        public void Test_StateMachine_Build_Empty()
        {
            // Arrange / Act
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder().Build());
        }

        [Test]
        public void Test_AddState()
        {
            // Arrange / Act
            var state = new MockState("A");
            stateMachine = new StateMachine.Builder()
                .AddState(state)
                .Build();

            // Assert
            Assert.That(stateMachine.CurrentState, Is.Null);
            Assert.That(stateMachine.DefaultState, Is.Not.Null);
            Assert.That(stateMachine.DefaultState, Is.EqualTo(state));
        }

        [Test]
        public void Test_AddState_With_2_Default_States()
        {
            // Arrange / Act
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB, true)
                .Build();

            // Assert
            Assert.That(stateMachine.DefaultState.Name, Is.EqualTo(stateB.Name));
        }

        [Test]
        public void Test_AddState_Twice()
        {
            // Arrange
            var state = new MockState("A");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(state)
                .AddState(state)
                .Build());
        }

        [Test]
        public void Test_AddState_WithSameName()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateABis = new MockState("A");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA)
                .AddState(stateABis)
                .Build());
        }

        [Test]
        public void Test_GetState()
        {
            // Arrange
            var state = new MockState("A");
            stateMachine = new StateMachine.Builder()
                .AddState(state)
                .Build();

            // Act
            var stateFromMachine = stateMachine.GetState("A");

            // Assert
            Assert.That(stateFromMachine, Is.EqualTo(state));
        }

        [Test]
        public void Test_GetState_As_Type()
        {
            // Arrange
            var state = new MockState("A");
            stateMachine = new StateMachine.Builder()
                .AddState(state)
                .Build();

            // Act
            var stateFromMachine = stateMachine.GetState<MockState>("A");

            // Assert
            Assert.That(stateFromMachine, Is.EqualTo(state));
        }

        [Test]
        public void Test_AddTransition()
        {
            // Arrange / Act
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, out var transition)
                .Build();

            // Assert
            Assert.That(transition, Is.Not.Null);
            Assert.That(transition.NextState, Is.EqualTo(stateB));
        }

        [Test]
        public void Test_AddTransition_ToSameState()
        {
            // Arrange / Act
            var state = new MockState("A");
            new StateMachine.Builder()
                .AddState(state, true)
                .AddTransition(state.Name, state.Name, out var transition)
                .Build();

            // Assert
            Assert.That(transition, Is.Not.Null);
        }

        [Test]
        public void Test_AddTransition_Twice()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name)
                .AddTransition(stateA.Name, stateB.Name)
                .Build());
        }

        [Test]
        public void Test_AddTransition_Where_StateDoesNotExist()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA, true)
                .AddTransition(stateA.Name, stateB.Name)
                .Build());
        }

        [Test]
        public void Test_AddTransition_FromAnyState_WithoutCondition()
        {
            // Arrange
            var state = new MockState("A");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(state)
                .AddTransitionFromAnyState(state.Name));
        }

        [Test]
        public void Test_AddTransition_FromAnyState()
        {
            // Arrange / Act
            var state = new MockState("A");
            new StateMachine.Builder()
                .AddState(state)
                .AddTransitionFromAnyState(state.Name, out var transition, Condition.CreateTriggerCondition("dummy"))
                .Build();

            // Assert
            Assert.That(transition, Is.Not.Null);
        }

        [Test]
        public void Test_AddTransition_FromAnyState_WithNotExistingState()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA)
                .AddTransitionFromAnyState(stateB.Name, Condition.CreateTriggerCondition("dummy"))
                .Build());
        }

        [Test]
        public void Test_AddTransition_FromAnyState_Twice()
        {
            // Arrange
            var state = new MockState("A");

            // Act
            new StateMachine.Builder()
                .AddState(state)
                .AddTransitionFromAnyState(state.Name, out var transition1, Condition.CreateTriggerCondition("dummy"))
                .AddTransitionFromAnyState(state.Name, out var transition2, Condition.CreateTriggerCondition("dummy"))
                .Build();

            // Assert
            Assert.That(transition1, Is.Not.Null);
            Assert.That(transition2, Is.Not.Null);
            Assert.That(transition1, Is.Not.EqualTo(transition2));
        }

        [Test]
        public void Test_StateMachine_Start()
        {
            // Arrange
            var state = new MockState("A");
            stateMachine = new StateMachine.Builder()
                .AddState(state, true)
                .Build();

            // Act
            stateMachine.Start();

            // Assert
            Assert.That(stateMachine.IsStarted, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(state));
        }

        [Test]
        public void Test_Start_Transition_WithoutCondition()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name)
                .Build();

            // Act
            stateMachine.Start();

            // Assert
            Assert.That(stateMachine.CurrentState, Is.EqualTo(stateB));
        }

        [Test]
        public void Test_Update_With_TimeTransition()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, 1f)
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.Update(1f);

            // Assert
            Assert.That(stateMachine.CurrentState, Is.EqualTo(stateB));
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnExitCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Test_Update()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, 1f)
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.Update(0.5f);

            // Assert
            Assert.That(stateMachine.StateTime, Is.EqualTo(0.5f));
            Assert.That(stateA.OnUpdateTime, Is.EqualTo(0.5f));
        }

        [Test]
        public void Test_Update_OverTime()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, 1f)
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.Update(1.5f);

            // Assert
            Assert.That(stateMachine.StateTime, Is.EqualTo(0.5f));
        }

        [Test]
        public void Test_Update_TranstionFromAnyState()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransitionFromAnyState(stateB.Name, Condition.CreateTriggerCondition("trigger"))
                .Build();
            stateMachine.Start();
            stateMachine.Blackboard.SetTrigger("trigger");

            // Act
            stateMachine.Update(0f);

            // Assert
            Assert.That(stateMachine.CurrentState, Is.EqualTo(stateB));
        }

        [Test]
        public void Test_SetState()
        {
            // Arrange
            var stateA = new MockState("A");
            var stateB = new MockState("B");
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA.Name, stateB.Name, 0f, Condition.CreateTriggerCondition("trigger"))
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.SetState(stateB.Name);

            // Assert
            Assert.That(stateMachine.CurrentState, Is.EqualTo(stateB));
        }
    }

    public class MockState : IState
    {
        public int OnEnterCallCount = 0;
        public int OnExitCallCount = 0;
        public float OnUpdateTime = 0f;

        public MockState(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public void OnEnter() { OnEnterCallCount++; }

        public void OnExit() { OnExitCallCount++; }

        public void OnUpdate(float deltaTime) { OnUpdateTime += deltaTime; }

        public override string ToString() => Name;

        public void Dispose() { }
    }
}