//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM;
using NUnit.Framework;

namespace BlueCheese.Tests.FSM
{
    public class Tests_Transitions
    {
		[Test]
		public void Test_Transition_Evaluate_ExitTime()
		{
			// Arrange
			var state = new MockState("A");
			var blackboard = new Blackboard();
			ITransition transition = new Transition(state, 1f);

			// Act / Assert
			Assert.That(transition.Evaluate(0.5f, blackboard, out _, out _), Is.False);
			Assert.That(transition.Evaluate(1f, blackboard, out _, out _), Is.True);
			Assert.That(transition.Evaluate(2f, blackboard, out _, out _), Is.True);
		}

		[Test]
        public void Test_Transition_Evaluate_Trigger()
        {
            // Arrange
            var state = new MockState("A");
            var blackboard = new Blackboard();
            ITransition transition = new Transition(state, 0f, Condition.CreateTriggerCondition("test"));

			// Act
			blackboard.SetTrigger("test");

            // Assert
            Assert.That(transition.Evaluate(0f, blackboard, out _, out _), Is.True);
        }

        [Test]
        public void Test_Transition_Evaluate_Bool()
        {
            // Arrange
            var state = new MockState("A");
            var blackboard = new Blackboard();
            ITransition transition = new Transition(state, 0f, Condition.CreateBoolCondition("test", true));

			// Act
			blackboard.SetBool("test", true);

            // Assert
            Assert.That(transition.Evaluate(0f, blackboard, out _, out _), Is.True);
        }

        [Test]
        public void Test_Transition_Evaluate_Int()
        {
            // Arrange
            var state = new MockState("A");
            var blackboard = new Blackboard();
			ITransition transition = new Transition(state, 0f, Condition.CreateIntCondition("test", Condition.Operator.Equals, 1));

			// Act
			blackboard.SetInt("test", 1);

			// Assert
			Assert.That(transition.Evaluate(0f, blackboard, out _, out _), Is.True);
		}

        [Test]
        public void Test_Transition_Evaluate_Float()
		{
			// Arrange
			var state = new MockState("A");
			var blackboard = new Blackboard();
			ITransition transition = new Transition(state, 0f, Condition.CreateFloatCondition("test", Condition.Operator.Equals, 1f));

			// Act
			blackboard.SetFloat("test", 1f);

			// Assert
			Assert.That(transition.Evaluate(0f, blackboard, out _, out _), Is.True);
		}
    }
}