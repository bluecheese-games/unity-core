using BlueCheese.Core.FSM;
using NUnit.Framework;

namespace BlueCheese.Tests.FSM
{
    public class Tests_Blackboard
    {
        private Blackboard blackboard;

        [SetUp]
        public void SetUp() => blackboard = new Blackboard();

        [Test]
        public void Test_GetBoolValue_Default()
        {
            Assert.That(blackboard.GetBoolValue("missing"), Is.False);
        }

        [Test]
        public void Test_GetIntValue_Default()
        {
            Assert.That(blackboard.GetIntValue("missing"), Is.EqualTo(0));
        }

        [Test]
        public void Test_GetFloatValue_Default()
        {
            Assert.That(blackboard.GetFloatValue("missing"), Is.EqualTo(0f));
        }

        [Test]
        public void Test_GetTriggerState_Default()
        {
            Assert.That(blackboard.GetTriggerState("missing"), Is.False);
        }

        [Test]
        public void Test_SetBool_True()
        {
            blackboard.SetBool("flag", true);
            Assert.That(blackboard.GetBoolValue("flag"), Is.True);
        }

        [Test]
        public void Test_SetBool_False()
        {
            blackboard.SetBool("flag", false);
            Assert.That(blackboard.GetBoolValue("flag"), Is.False);
        }

        [Test]
        public void Test_SetInt()
        {
            blackboard.SetInt("count", 42);
            Assert.That(blackboard.GetIntValue("count"), Is.EqualTo(42));
        }

        [Test]
        public void Test_SetFloat()
        {
            blackboard.SetFloat("speed", 3.14f);
            Assert.That(blackboard.GetFloatValue("speed"), Is.EqualTo(3.14f));
        }

        [Test]
        public void Test_SetTrigger()
        {
            blackboard.SetTrigger("fire");
            Assert.That(blackboard.GetTriggerState("fire"), Is.True);
        }

        [Test]
        public void Test_ResetTriggers()
        {
            blackboard.SetTrigger("fire");
            blackboard.ResetTriggers();
            Assert.That(blackboard.GetTriggerState("fire"), Is.False);
        }

        [Test]
        public void Test_ResetTriggers_DoesNotClearOtherParams()
        {
            blackboard.SetBool("flag", true);
            blackboard.SetInt("count", 5);
            blackboard.SetFloat("speed", 1f);
            blackboard.SetTrigger("fire");

            blackboard.ResetTriggers();

            Assert.That(blackboard.GetBoolValue("flag"), Is.True);
            Assert.That(blackboard.GetIntValue("count"), Is.EqualTo(5));
            Assert.That(blackboard.GetFloatValue("speed"), Is.EqualTo(1f));
        }

        [Test]
        public void Test_Reset_ClearsAll()
        {
            blackboard.SetBool("flag", true);
            blackboard.SetInt("count", 5);
            blackboard.SetFloat("speed", 1f);
            blackboard.SetTrigger("fire");

            blackboard.Reset();

            Assert.That(blackboard.GetBoolValue("flag"), Is.False);
            Assert.That(blackboard.GetIntValue("count"), Is.EqualTo(0));
            Assert.That(blackboard.GetFloatValue("speed"), Is.EqualTo(0f));
            Assert.That(blackboard.GetTriggerState("fire"), Is.False);
        }

        [Test]
        public void Test_Overwrite_Value()
        {
            blackboard.SetInt("count", 1);
            blackboard.SetInt("count", 99);
            Assert.That(blackboard.GetIntValue("count"), Is.EqualTo(99));
        }
    }
}
