//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM;
using NUnit.Framework;

namespace BlueCheese.Tests.FSM
{
    public class Tests_Conditions
    {
        private StateMachine stateMachine;

        [TearDown]
        public void TearDown() => stateMachine?.Dispose();

        [Test]
        public void Test_Condition_Evaluate_Trigger()
        {
            // Arrange
            stateMachine = new StateMachine.Builder()
                .AddState(new MockState("A"), true)
                .Build();
            ICondition condition = Condition.CreateTriggerCondition("test");

            // Act
            stateMachine.SetTrigger("test");

            // Assert
            Assert.That(condition.Evaluate(stateMachine), Is.True);
        }

        [Test]
        public void Test_Condition_Evaluate_Bool()
        {
            // Arrange
            stateMachine = new StateMachine.Builder()
                .AddState(new MockState("A"), true)
                .Build();
            ICondition condition = Condition.CreateBoolCondition("test", true);

            // Act
            stateMachine.SetBool("test", true);

            // Assert
            Assert.That(condition.Evaluate(stateMachine), Is.True);
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
            stateMachine = new StateMachine.Builder()
                .AddState(new MockState("A"), true)
                .Build();
            ICondition condition = Condition.CreateIntCondition("test", op, targetValue);

            // Act
            stateMachine.SetInt("test", 1);

            // Assert
            return condition.Evaluate(stateMachine);
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
            stateMachine = new StateMachine.Builder()
                .AddState(new MockState("A"), true)
                .Build();
            ICondition condition = Condition.CreateFloatCondition("test", op, targetValue);

            // Act
            stateMachine.SetFloat("test", 1f);

            // Assert
            return condition.Evaluate(stateMachine);
        }

        [Test]
        public void Test_Condition_Evaluate_NotExistingTrigger()
        {
            // Arrange
            stateMachine = new StateMachine.Builder()
                .AddState(new MockState("A"), true)
                .Build();
            ICondition condition = Condition.CreateTriggerCondition("test");

            // Act
            stateMachine.SetTrigger("test2");

            // Assert
            Assert.That(condition.Evaluate(stateMachine), Is.False);
        }

        [Test]
        // test predicate condition
        public void Test_Condition_Evaluate_Predicate()
        {
            // Arrange / Act
            stateMachine = new StateMachine.Builder()
                .AddState(new MockState("A"), true)
                .Build();
            ICondition condition = Condition.CreatePredicateCondition(() => true);

            // Assert
            Assert.That(condition.Evaluate(stateMachine), Is.True);
        }
    }
}