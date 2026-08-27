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

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        public void PlayVoice(AudioClip clip, float volume = 1f)
        {
            if (clip != null && voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = clip;
                voiceSource.volume = volume;
                voiceSource.Play();
            }
        }

        public void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true, float fadeDuration = 0.5f)
        {
            if (bgmSource == null) return;
            StartCoroutine(FadeBGM(clip, volume, loop, fadeDuration));
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
