using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Investigation;

namespace Investigation.Tests
{
    [TestFixture]
    public class CaseStateTests
    {
        private CaseState state;

        [SetUp]
        public void SetUp()
        {
            CaseState.ResetForNewGame();
            state = CaseState.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            CaseState.ResetForNewGame();
        }

        [Test]
        public void Flags_SetClearAndEvaluate_WorksCorrectly()
        {
            Assert.IsFalse(state.HasFlag("test_flag"));

            state.SetFlag("test_flag");
            Assert.IsTrue(state.HasFlag("test_flag"));

            var cond = new ConditionData { kind = ConditionKind.Flag, a = "test_flag", negate = false };
            Assert.IsTrue(state.Evaluate(cond));

            var condNegated = new ConditionData { kind = ConditionKind.Flag, a = "test_flag", negate = true };
            Assert.IsFalse(state.Evaluate(condNegated));

            state.ClearFlag("test_flag");
            Assert.IsFalse(state.HasFlag("test_flag"));
            Assert.IsTrue(state.Evaluate(condNegated));
        }

        [Test]
        public void Topics_UnlockAndMarkSeen_EvaluatesAccurately()
        {
            Assert.IsFalse(state.IsTopicUnlocked("robert", "topic1"));
            Assert.IsFalse(state.HasSeenTopic("robert", "topic1"));

            state.UnlockTopic("robert", "topic1");
            Assert.IsTrue(state.IsTopicUnlocked("robert", "topic1"));

            var condUnlocked = new ConditionData { kind = ConditionKind.TopicUnlocked, a = "robert", b = "topic1" };
            Assert.IsTrue(state.Evaluate(condUnlocked));

            state.MarkTopicSeen("robert", "topic1");
            Assert.IsTrue(state.HasSeenTopic("robert", "topic1"));

            var condSeen = new ConditionData { kind = ConditionKind.TopicSeen, a = "robert", b = "topic1" };
            Assert.IsTrue(state.Evaluate(condSeen));
        }

        [Test]
        public void ClueCollection_IsIdempotent_AndEventFiresOnlyOnce()
        {
            int eventCount = 0;
            CaseState.OnClueCollected += HandleClue;

            try
            {
                Assert.IsFalse(state.HasClue("knife"));

                // Primera recolección
                state.CollectClue("knife", playSound: false);
                Assert.IsTrue(state.HasClue("knife"));
                Assert.AreEqual(1, eventCount);

                // Segunda recolección (duplicada) no debe disparar nuevo evento
                state.CollectClue("knife", playSound: false);
                Assert.AreEqual(1, eventCount);
                Assert.AreEqual(1, state.CollectedClues.Count);
            }
            finally
            {
                CaseState.OnClueCollected -= HandleClue;
            }

            void HandleClue() => eventCount++;
        }

        [Test]
        public void Counters_IncrementAndThresholdEvaluation_Works()
        {
            Assert.AreEqual(0, state.GetCounter("reputation"));

            state.IncrementCounter("reputation", 3);
            Assert.AreEqual(3, state.GetCounter("reputation"));

            var cond3 = new ConditionData { kind = ConditionKind.CounterAtLeast, a = "reputation", b = "3" };
            Assert.IsTrue(state.Evaluate(cond3));

            var cond5 = new ConditionData { kind = ConditionKind.CounterAtLeast, a = "reputation", b = "5" };
            Assert.IsFalse(state.Evaluate(cond5));

            var cond5Negated = new ConditionData { kind = ConditionKind.CounterAtLeast, a = "reputation", b = "5", negate = true };
            Assert.IsTrue(state.Evaluate(cond5Negated));
        }

        [Test]
        public void EvaluateAll_HandlesNullAndLists()
        {
            Assert.IsTrue(state.EvaluateAll(null));
            Assert.IsTrue(state.EvaluateAll(new List<ConditionData>()));

            state.SetFlag("alpha");
            state.SetFlag("beta");

            var conditions = new List<ConditionData>
            {
                new ConditionData { kind = ConditionKind.Flag, a = "alpha" },
                new ConditionData { kind = ConditionKind.Flag, a = "beta" }
            };
            Assert.IsTrue(state.EvaluateAll(conditions));

            conditions.Add(new ConditionData { kind = ConditionKind.Flag, a = "gamma" });
            Assert.IsFalse(state.EvaluateAll(conditions));
        }

        [Test]
        public void ApplyEffects_AppliesAllEffectKinds()
        {
            var effects = new List<EffectData>
            {
                new EffectData { kind = EffectKind.SetFlag, a = "effect_flag" },
                new EffectData { kind = EffectKind.CollectClue, a = "clue_effect" },
                new EffectData { kind = EffectKind.UnlockTopic, a = "elena", b = "topic_elena" },
                new EffectData { kind = EffectKind.IncrementCounter, a = "score", b = "5" }
            };

            state.ApplyAll(effects);

            Assert.IsTrue(state.HasFlag("effect_flag"));
            Assert.IsTrue(state.HasClue("clue_effect"));
            Assert.IsTrue(state.IsTopicUnlocked("elena", "topic_elena"));
            Assert.AreEqual(5, state.GetCounter("score"));
        }
    }
}
