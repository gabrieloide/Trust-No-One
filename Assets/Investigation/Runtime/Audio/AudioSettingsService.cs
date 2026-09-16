using System;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    public enum TextSpeedSetting
    {
        Normal = 0,
        Fast = 1,
        Instant = 2
    }

    public class AudioSettingsService
    {
        private const string MasterVolKey = "Settings_MasterVol";
        private const string MusicVolKey = "Settings_MusicVol";
        private const string SfxVolKey = "Settings_SfxVol";
        private const string TextSpeedKey = "Settings_TextSpeed";

        private static AudioSettingsService instance;
        public static AudioSettingsService Instance => instance ?? (instance = new AudioSettingsService());

        public float MasterVolume { get; private set; } = 1.0f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 1.0f;
        public TextSpeedSetting TextSpeed { get; private set; } = TextSpeedSetting.Normal;

        public static event Action<AudioSettingsService> OnSettingsChanged;

        public AudioSettingsService()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterVolKey, 1.0f);
            MusicVolume = PlayerPrefs.GetFloat(MusicVolKey, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolKey, 1.0f);
            TextSpeed = (TextSpeedSetting)PlayerPrefs.GetInt(TextSpeedKey, (int)TextSpeedSetting.Normal);
            ApplySettings();
        }

        public void ApplySettings()
        {
            StoryAudioManager.SetGlobalVolumes(MasterVolume, MusicVolume, SfxVolume);
            StoryDialogueUI.TextSpeedMultiplier = GetTypewriterSpeedModifier();
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MasterVolKey, MasterVolume);
            PlayerPrefs.Save();
            ApplySettings();
            OnSettingsChanged?.Invoke(this);
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolKey, MusicVolume);
            PlayerPrefs.Save();
            ApplySettings();
            OnSettingsChanged?.Invoke(this);
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolKey, SfxVolume);
            PlayerPrefs.Save();
            ApplySettings();
            OnSettingsChanged?.Invoke(this);
        }

        public void SetTextSpeed(TextSpeedSetting speed)
        {
            TextSpeed = speed;
            PlayerPrefs.SetInt(TextSpeedKey, (int)TextSpeed);
            PlayerPrefs.Save();
            ApplySettings();
            OnSettingsChanged?.Invoke(this);
        }

        public float GetEffectiveMusicVolume(float baseVolume = 1f) => Mathf.Clamp01(baseVolume * MusicVolume * MasterVolume);

        public float GetEffectiveSfxVolume(float baseVolume = 1f) => Mathf.Clamp01(baseVolume * SfxVolume * MasterVolume);

        public float GetTypewriterSpeedModifier()
        {
            switch (TextSpeed)
            {
                case TextSpeedSetting.Fast: return 0.4f;
                case TextSpeedSetting.Instant: return 0f;
                default: return 1.0f;
            }
        }
    }
}
