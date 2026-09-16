using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    public class StoryAudioManager : MonoBehaviour
    {
        public static StoryAudioManager Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource voiceSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }
            if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        }

        public static float MasterVolume { get; private set; } = 1.0f;
        public static float BGMVolume { get; private set; } = 1.0f;
        public static float SFXVolume { get; private set; } = 1.0f;

        private float currentBgmBaseVolume = 1f;

        public static void SetGlobalVolumes(float master, float bgm, float sfx)
        {
            MasterVolume = Mathf.Clamp01(master);
            BGMVolume = Mathf.Clamp01(bgm);
            SFXVolume = Mathf.Clamp01(sfx);

            if (Instance != null && Instance.bgmSource != null && Instance.bgmSource.isPlaying)
            {
                Instance.bgmSource.volume = Instance.currentBgmBaseVolume * MasterVolume * BGMVolume;
            }
        }

        public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
        {
            if (clip != null && sfxSource != null)
            {
                if (pitchVariation > 0f)
                {
                    sfxSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
                }
                else
                {
                    sfxSource.pitch = 1f;
                }
                float finalVolume = Mathf.Clamp01(volume * MasterVolume * SFXVolume);
                sfxSource.PlayOneShot(clip, finalVolume);
            }
        }

        public void PlayVoice(AudioClip clip, float volume = 1f)
        {
            if (clip != null && voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = clip;
                voiceSource.volume = Mathf.Clamp01(volume * MasterVolume * SFXVolume);
                voiceSource.Play();
            }
        }

        public void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true, float fadeDuration = 0.5f)
        {
            if (bgmSource == null) return;
            currentBgmBaseVolume = volume;
            float effectiveVolume = volume * MasterVolume * BGMVolume;
            StartCoroutine(FadeBGM(clip, effectiveVolume, loop, fadeDuration));
        }

        public void StopBGM(float fadeDuration = 0.5f)
        {
            if (bgmSource == null) return;
            StartCoroutine(FadeBGM(null, 0f, false, fadeDuration));
        }

        private IEnumerator FadeBGM(AudioClip newClip, float targetVolume, bool loop, float fadeDuration)
        {
            float startVolume = bgmSource.volume;

            if (bgmSource.isPlaying && fadeDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                    yield return null;
                }
            }

            bgmSource.Stop();

            if (newClip != null)
            {
                bgmSource.clip = newClip;
                bgmSource.loop = loop;
                bgmSource.volume = 0f;
                bgmSource.Play();

                if (fadeDuration > 0f)
                {
                    float elapsed = 0f;
                    while (elapsed < fadeDuration)
                    {
                        elapsed += Time.deltaTime;
                        bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeDuration);
                        yield return null;
                    }
                }
                bgmSource.volume = targetVolume;
            }
        }
    }
}
