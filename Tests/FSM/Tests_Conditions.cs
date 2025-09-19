//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM;
using NUnit.Framework;

namespace BlueCheese.Tests.FSM
{
    public class Tests_Conditions
    {
        [Test]
        public void Test_Condition_Evaluate_Trigger()
        {
            // Arrange
            var blackboard = new Blackboard();
            ICondition condition = Condition.CreateTriggerCondition("test");

			// Act
			blackboard.SetTrigger("test");

            // Assert
            Assert.That(condition.Evaluate(blackboard), Is.True);
        }

        [Test]
        public void Test_Condition_Evaluate_Bool()
        {
            // Arrange
            var blackboard = new Blackboard();
            ICondition condition = Condition.CreateBoolCondition("test", true);

            // Act
            blackboard.SetBool("test", true);

            // Assert
            Assert.That(condition.Evaluate(blackboard), Is.True);
        }

        [Test]
        [TestCase(Condition.Operator.Equals, 1, ExpectedResult = true)]
        [TestCase(Condition.Operator.NotEquals, 1, ExpectedResult = false)]
        [TestCase(Condition.Operator.Greater, 1, ExpectedResult = false)]
        [TestCase(Condition.Operator.Less, 1, ExpectedResult = false)]
        [TestCase(Condition.Operator.GreaterOrEqual, 1, ExpectedResult = true)]
        [TestCase(Condition.Operator.LessOrEqual, 1, ExpectedResult = true)]
        public bool Test_Condition_Evaluate_Int(Condition.Operator op, int targetValue)
        {
            // Arrange
            var blackboard = new Blackboard();
            ICondition condition = Condition.CreateIntCondition("test", op, targetValue);

			// Act
			blackboard.SetInt("test", 1);

            // Assert
            return condition.Evaluate(blackboard);
        }

        [Test]
        [TestCase(Condition.Operator.Equals, 1f, ExpectedResult = true)]
        [TestCase(Condition.Operator.NotEquals, 1f, ExpectedResult = false)]
        [TestCase(Condition.Operator.Greater, 1f, ExpectedResult = false)]
        [TestCase(Condition.Operator.Less, 1f, ExpectedResult = false)]
        [TestCase(Condition.Operator.GreaterOrEqual, 1f, ExpectedResult = true)]
        [TestCase(Condition.Operator.LessOrEqual, 1f, ExpectedResult = true)]
        public bool Test_Condition_Evaluate_Float(Condition.Operator op, float targetValue)
        {
			// Arrange
			var blackboard = new Blackboard();
			ICondition condition = Condition.CreateFloatCondition("test", op, targetValue);

            // Act
            blackboard.SetFloat("test", 1f);

            // Assert
            return condition.Evaluate(blackboard);
        }

        [Test]
        public void Test_Condition_Evaluate_NotExistingTrigger()
        {
			// Arrange
			var blackboard = new Blackboard();
			ICondition condition = Condition.CreateTriggerCondition("test");

			// Act
			blackboard.SetTrigger("test2");

            // Assert
            Assert.That(condition.Evaluate(blackboard), Is.False);
        }

        [Test]
        // test predicate condition
        public void Test_Condition_Evaluate_Predicate()
        {
			// Arrange / Act
			var blackboard = new Blackboard();
			ICondition condition = Condition.CreatePredicateCondition(() => true);

            // Assert
            Assert.That(condition.Evaluate(blackboard), Is.True);
        }
    }
}