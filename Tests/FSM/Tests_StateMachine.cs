//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM;
using NUnit.Framework;
using System;
using System.Linq;

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
                .AddState("A", true)
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
            var state = "A";
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
            var stateA = "A";
            var stateB = "B";
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB, true)
                .Build();

            // Assert
            Assert.That(stateMachine.DefaultState, Is.EqualTo(stateB));
        }

        [Test]
        public void Test_AddState_Twice()
        {
            // Arrange
            var state = "A";

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
            var stateA = "A";
            var stateABis = "A";

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA)
                .AddState(stateABis)
                .Build());
        }

		enum StateNames { A, B };

		[Test]
		public void Test_StateMachine_Build_FromEnum()
        {
			// Arrange
			StateNames defaultState = default;

            // Act
            stateMachine = new StateMachine.Builder()
                .FromEnum<StateNames>()
                .Build();

			// Assert
			Assert.That(stateMachine, Is.Not.Null);
            Assert.That(stateMachine.DefaultState, Is.EqualTo(defaultState.ToString()));
            Assert.That(stateMachine.States.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Test_GetStateHandler()
        {
            // Arrange
            var state = "A";
            var stateAHandler = new MockStateHandler();
            stateMachine = new StateMachine.Builder()
                .AddState(state, stateAHandler)
                .Build();

            // Act
            var stateHandlers = stateMachine.GetStateHandler("A");

            // Assert
            Assert.That(stateHandlers.Handlers, Has.Member(stateAHandler));
        }

        [Test]
        public void Test_AddTransition()
        {
            // Arrange / Act
            var stateA = "A";
            var stateB = "B";
            new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA, stateB, out var transition)
                .Build();

            // Assert
            Assert.That(transition, Is.Not.Null);
            Assert.That(transition.NextState, Is.EqualTo(stateB));
        }

        [Test]
        public void Test_AddTransition_ToSameState()
        {
            // Arrange / Act
            var state = "A";
            new StateMachine.Builder()
                .AddState(state, true)
                .AddTransition(state, state, out var transition)
                .Build();

            // Assert
            Assert.That(transition, Is.Not.Null);
        }

        [Test]
        public void Test_AddTransition_Twice()
        {
            // Arrange
            var stateA = "A";
            var stateB = "B";

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA, stateB)
                .AddTransition(stateA, stateB)
                .Build());
        }

        [Test]
        public void Test_AddTransition_Where_StateDoesNotExist()
        {
            // Arrange
            var stateA = "A";
            var stateB = "B";

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA, true)
                .AddTransition(stateA, stateB)
                .Build());
        }

        [Test]
        public void Test_AddTransition_FromAnyState_WithoutCondition()
        {
            // Arrange
            var state = "A";

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(state)
                .AddTransitionFromAnyState(state));
        }

        [Test]
        public void Test_AddTransition_FromAnyState()
        {
            // Arrange / Act
            var state = "A";
            new StateMachine.Builder()
                .AddState(state)
                .AddTransitionFromAnyState(state, out var transition, Condition.CreateTriggerCondition("dummy"))
                .Build();

            // Assert
            Assert.That(transition, Is.Not.Null);
        }

        [Test]
        public void Test_AddTransition_FromAnyState_WithNotExistingState()
        {
            // Arrange
            var stateA = "A";
            var stateB = "B";

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new StateMachine.Builder()
                .AddState(stateA)
                .AddTransitionFromAnyState(stateB, Condition.CreateTriggerCondition("dummy"))
                .Build());
        }

        [Test]
        public void Test_AddTransition_FromAnyState_Twice()
        {
            // Arrange
            var state = "A";

            // Act
            new StateMachine.Builder()
                .AddState(state)
                .AddTransitionFromAnyState(state, out var transition1, Condition.CreateTriggerCondition("dummy"))
                .AddTransitionFromAnyState(state, out var transition2, Condition.CreateTriggerCondition("dummy"))
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
            var state = "A";
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
            var stateA = "A";
            var stateB = "B";
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA, stateB)
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
            var stateA = "A";
            var stateAHandler = new MockStateHandler();
            var stateB = "B";
            var stateBHandler = new MockStateHandler();
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, stateAHandler, true)
                .AddState(stateB, stateBHandler)
                .AddTransition(stateA, stateB, 1f)
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.Update(1f);

            // Assert
            Assert.That(stateMachine.CurrentState, Is.EqualTo(stateB));
            Assert.That(stateAHandler.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateAHandler.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateBHandler.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateBHandler.OnExitCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Test_Update()
        {
            // Arrange
            var stateA = "A";
            var stateAHandler = new MockStateHandler();
            var stateB = "B";
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, stateAHandler, true)
                .AddState(stateB)
                .AddTransition(stateA, stateB, 1f)
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.Update(0.5f);

            // Assert
            Assert.That(stateMachine.StateTime, Is.EqualTo(0.5f));
            Assert.That(stateAHandler.OnUpdateTime, Is.EqualTo(0.5f));
        }

        [Test]
        public void Test_Update_OverTime()
        {
            // Arrange
            var stateA = "A";
            var stateB = "B";
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA, stateB, 1f)
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
            var stateA = "A";
            var stateB = "B";
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransitionFromAnyState(stateB, Condition.CreateTriggerCondition("trigger"))
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
            var stateA = "A";
            var stateB = "B";
            stateMachine = new StateMachine.Builder()
                .AddState(stateA, true)
                .AddState(stateB)
                .AddTransition(stateA, stateB, 0f, Condition.CreateTriggerCondition("trigger"))
                .Build();
            stateMachine.Start();

            // Act
            stateMachine.SetState(stateB);

            // Assert
            Assert.That(stateMachine.CurrentState, Is.EqualTo(stateB));
        }
    }

    public class MockStateHandler : IStateHandler
    {
        public int OnEnterCallCount = 0;
        public int OnExitCallCount = 0;
        public float OnUpdateTime = 0f;

        public void OnEnter() { OnEnterCallCount++; }

        public void OnExit() { OnExitCallCount++; }

        public void OnUpdate(float deltaTime) { OnUpdateTime += deltaTime; }

        public void Dispose() { }
    }
}