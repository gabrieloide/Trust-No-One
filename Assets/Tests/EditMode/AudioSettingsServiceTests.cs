using NUnit.Framework;
using UnityEngine;
using Investigation;

namespace Investigation.Tests
{
    [TestFixture]
    public class AudioSettingsServiceTests
    {
        private AudioSettingsService service;

        [SetUp]
        public void SetUp()
        {
            service = new AudioSettingsService();
        }

        [Test]
        public void VolumeSetters_ClampBetweenZeroAndOne()
        {
            service.SetMasterVolume(1.5f);
            Assert.AreEqual(1.0f, service.MasterVolume, 0.001f);

            service.SetMasterVolume(-0.5f);
            Assert.AreEqual(0.0f, service.MasterVolume, 0.001f);

            service.SetMusicVolume(2.0f);
            Assert.AreEqual(1.0f, service.MusicVolume, 0.001f);

            service.SetMusicVolume(-1.0f);
            Assert.AreEqual(0.0f, service.MusicVolume, 0.001f);

            service.SetSfxVolume(1.2f);
            Assert.AreEqual(1.0f, service.SfxVolume, 0.001f);

            service.SetSfxVolume(-0.2f);
            Assert.AreEqual(0.0f, service.SfxVolume, 0.001f);
        }

        [Test]
        public void EffectiveVolumes_ScaleWithMasterAndChannelVolumes()
        {
            service.SetMasterVolume(0.5f);
            service.SetMusicVolume(0.8f);
            service.SetSfxVolume(0.6f);

            Assert.AreEqual(0.4f, service.GetEffectiveMusicVolume(1.0f), 0.001f);
            Assert.AreEqual(0.3f, service.GetEffectiveSfxVolume(1.0f), 0.001f);
        }

        [Test]
        public void TextSpeedSettings_ProvideExpectedTypewriterModifiers()
        {
            service.SetTextSpeed(TextSpeedSetting.Normal);
            Assert.AreEqual(1.0f, service.GetTypewriterSpeedModifier(), 0.001f);

            service.SetTextSpeed(TextSpeedSetting.Fast);
            Assert.AreEqual(0.4f, service.GetTypewriterSpeedModifier(), 0.001f);

            service.SetTextSpeed(TextSpeedSetting.Instant);
            Assert.AreEqual(0.0f, service.GetTypewriterSpeedModifier(), 0.001f);
        }

        [Test]
        public void OnSettingsChanged_FiresWhenAnyPropertyChanges()
        {
            int callCount = 0;
            AudioSettingsService.OnSettingsChanged += OnChanged;

            void OnChanged(AudioSettingsService s) => callCount++;

            try
            {
                service.SetMasterVolume(0.7f);
                service.SetMusicVolume(0.7f);
                service.SetSfxVolume(0.7f);
                service.SetTextSpeed(TextSpeedSetting.Fast);

                Assert.AreEqual(4, callCount);
            }
            finally
            {
                AudioSettingsService.OnSettingsChanged -= OnChanged;
            }
        }
    }
}
