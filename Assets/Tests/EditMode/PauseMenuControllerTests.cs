using NUnit.Framework;
using UnityEngine;
using Investigation;

namespace Investigation.Tests
{
    [TestFixture]
    public class PauseMenuControllerTests
    {
        private GameObject controllerGO;
        private PauseMenuController controller;

        [SetUp]
        public void SetUp()
        {
            controllerGO = new GameObject("Test_PauseMenuController");
            controller = controllerGO.AddComponent<PauseMenuController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (controllerGO != null)
            {
                Object.DestroyImmediate(controllerGO);
            }
        }

        [Test]
        public void SetPaused_UpdatesIsPausedState()
        {
            Assert.IsFalse(controller.IsPaused);

            controller.SetPaused(true);
            Assert.IsTrue(controller.IsPaused);

            controller.Resume();
            Assert.IsFalse(controller.IsPaused);
        }

        [Test]
        public void TogglePause_FlipsState()
        {
            Assert.IsFalse(controller.IsPaused);

            controller.TogglePause();
            Assert.IsTrue(controller.IsPaused);

            controller.TogglePause();
            Assert.IsFalse(controller.IsPaused);
        }

        [Test]
        public void AdjustVolumes_ModifiesAudioSettingsServiceWithinRange()
        {
            var audio = AudioSettingsService.Instance;
            audio.SetMasterVolume(0.5f);

            controller.AdjustMasterVolume(0.1f);
            Assert.AreEqual(0.6f, audio.MasterVolume, 0.001f);

            controller.AdjustMasterVolume(-0.2f);
            Assert.AreEqual(0.4f, audio.MasterVolume, 0.001f);
        }

        [Test]
        public void CycleTextSpeed_CyclesThroughAllSettings()
        {
            var audio = AudioSettingsService.Instance;
            audio.SetTextSpeed(TextSpeedSetting.Normal);

            controller.CycleTextSpeed();
            Assert.AreEqual(TextSpeedSetting.Fast, audio.TextSpeed);

            controller.CycleTextSpeed();
            Assert.AreEqual(TextSpeedSetting.Instant, audio.TextSpeed);

            controller.CycleTextSpeed();
            Assert.AreEqual(TextSpeedSetting.Normal, audio.TextSpeed);
        }

        [Test]
        public void OnPauseToggled_FiresEventWithState()
        {
            bool? lastState = null;
            int eventCount = 0;

            void Handler(bool paused)
            {
                eventCount++;
                lastState = paused;
            }

            PauseMenuController.OnPauseToggled += Handler;

            try
            {
                controller.SetPaused(true);
                Assert.AreEqual(1, eventCount);
                Assert.IsTrue(lastState);

                controller.Resume();
                Assert.AreEqual(2, eventCount);
                Assert.IsFalse(lastState);
            }
            finally
            {
                PauseMenuController.OnPauseToggled -= Handler;
            }
        }
    }
}
