using NUnit.Framework;
using UnityEngine;
using Investigation;

namespace Investigation.Tests
{
    [TestFixture]
    public class SaveGameServiceTests
    {
        private CaseState caseState;
        private InMemorySaveStorage memoryStorage;
        private SaveGameService saveService;

        [SetUp]
        public void SetUp()
        {
            CaseState.ResetForNewGame();
            caseState = CaseState.Instance;
            memoryStorage = new InMemorySaveStorage();
            saveService = new SaveGameService
            {
                Storage = memoryStorage
            };
        }

        [TearDown]
        public void TearDown()
        {
            CaseState.ResetForNewGame();
        }

        [Test]
        public void CaptureSnapshot_CapturesCurrentStateAccurately()
        {
            caseState.currentDay = 2;
            caseState.currentPhase = 3;
            caseState.actionsRemainingInPhase = 2;
            caseState.SetFlag("investigated_reception");
            caseState.CollectClue("bloody_knife", playSound: false);
            caseState.UnlockTopic("elena", "room4_key");
            caseState.MarkTopicSeen("elena", "room4_key");
            caseState.RecordConfrontation("elena", "bloody_knife");
            caseState.IncrementCounter("interrogations", 3);

            SaveData snapshot = caseState.CaptureSnapshot();

            Assert.IsNotNull(snapshot);
            Assert.AreEqual(2, snapshot.currentDay);
            Assert.AreEqual(3, snapshot.currentPhase);
            Assert.AreEqual(2, snapshot.actionsRemainingInPhase);
            Assert.IsTrue(snapshot.flags.Contains("investigated_reception"));
            Assert.IsTrue(snapshot.collectedClues.Contains("bloody_knife"));
            Assert.IsTrue(snapshot.unlockedTopics.Contains("elena:room4_key"));
            Assert.IsTrue(snapshot.seenTopics.Contains("elena:room4_key"));
            Assert.IsTrue(snapshot.confrontations.Contains("elena:bloody_knife"));
            
            var counterEntry = snapshot.counters.Find(c => c.key == "interrogations");
            Assert.IsNotNull(counterEntry);
            Assert.AreEqual(3, counterEntry.value);
        }

        [Test]
        public void RestoreSnapshot_RestoresStateIntoCaseState()
        {
            var data = new SaveData
            {
                currentDay = 3,
                currentPhase = 1,
                actionsRemainingInPhase = 4,
                flags = new System.Collections.Generic.List<string> { "door_opened" },
                collectedClues = new System.Collections.Generic.List<string> { "sheriff_badge" },
                unlockedTopics = new System.Collections.Generic.List<string> { "robert:alibi" },
                seenTopics = new System.Collections.Generic.List<string> { "robert:alibi" },
                confrontations = new System.Collections.Generic.List<string> { "robert:sheriff_badge" },
                counters = new System.Collections.Generic.List<CounterEntry> { new CounterEntry("lies_detected", 5) }
            };

            caseState.RestoreSnapshot(data);

            Assert.AreEqual(3, caseState.currentDay);
            Assert.AreEqual(1, caseState.currentPhase);
            Assert.AreEqual(4, caseState.actionsRemainingInPhase);
            Assert.IsTrue(caseState.HasFlag("door_opened"));
            Assert.IsTrue(caseState.HasClue("sheriff_badge"));
            Assert.IsTrue(caseState.IsTopicUnlocked("robert", "alibi"));
            Assert.IsTrue(caseState.HasSeenTopic("robert", "alibi"));
            Assert.IsTrue(caseState.WasConfrontedWith("robert", "sheriff_badge"));
            Assert.AreEqual(5, caseState.GetCounter("lies_detected"));
        }

        [Test]
        public void SaveAndLoad_ThroughStorage_RoundtripsAccurately()
        {
            caseState.currentDay = 2;
            caseState.currentPhase = 2;
            caseState.actionsRemainingInPhase = 1;
            caseState.SetFlag("met_marta");
            caseState.CollectClue("motel_ledger", playSound: false);

            bool saveSuccess = saveService.Save("slot_test");
            Assert.IsTrue(saveSuccess);
            Assert.IsTrue(saveService.HasSave("slot_test"));

            // Clear current state
            CaseState.ResetForNewGame();
            caseState = CaseState.Instance;
            Assert.AreEqual(1, caseState.currentDay);
            Assert.IsFalse(caseState.HasFlag("met_marta"));
            Assert.IsFalse(caseState.HasClue("motel_ledger"));

            // Load saved state
            bool loadSuccess = saveService.TryLoad(out SaveData loaded, "slot_test");
            Assert.IsTrue(loadSuccess);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(2, caseState.currentDay);
            Assert.AreEqual(2, caseState.currentPhase);
            Assert.AreEqual(1, caseState.actionsRemainingInPhase);
            Assert.IsTrue(caseState.HasFlag("met_marta"));
            Assert.IsTrue(caseState.HasClue("motel_ledger"));
        }

        [Test]
        public void DeleteSave_RemovesSavedData()
        {
            saveService.Save("to_delete");
            Assert.IsTrue(saveService.HasSave("to_delete"));

            saveService.DeleteSave("to_delete");
            Assert.IsFalse(saveService.HasSave("to_delete"));
        }

        [Test]
        public void CorruptedJson_FailsGracefullyWithoutThrowing()
        {
            memoryStorage.SetString("corrupt_slot", ":::INVALID_JSON_CORRUPT_DATA{{{");

            bool success = saveService.TryLoad(out SaveData loaded, "corrupt_slot");
            Assert.IsFalse(success);
            Assert.IsNull(loaded);
        }
    }
}
