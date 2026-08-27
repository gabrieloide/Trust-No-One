using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum StoryAudioType
    {
        SFX,
        BGM,
        Voice,
        StopBGM
    }

    [Serializable]
    public class PlayAudioAction : StoryAction
    {
        public StoryAudioType audioType = StoryAudioType.SFX;
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = false;
        public float bgmFadeDuration = 0.5f;

        public override IEnumerator Execute(StoryRunner runner)
        {
            var audioMgr = StoryAudioManager.Instance;
            if (audioMgr == null)
            {
                var found = UnityEngine.Object.FindAnyObjectByType<StoryAudioManager>();
                if (found != null) audioMgr = found;
            }

            if (audioMgr != null)
            {
                switch (audioType)
                {
                    case StoryAudioType.SFX:
                        audioMgr.PlaySFX(audioClip, volume);
                        break;
                    case StoryAudioType.BGM:
                        audioMgr.PlayBGM(audioClip, volume, loop, bgmFadeDuration);
                        break;
                    case StoryAudioType.Voice:
                        audioMgr.PlayVoice(audioClip, volume);
                        break;
                    case StoryAudioType.StopBGM:
                        audioMgr.StopBGM(bgmFadeDuration);
                        break;
                }
            }
            else if (audioClip != null)
            {
                AudioSource.PlayClipAtPoint(audioClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
            }

            yield break;
        }

        public override string GetSummary()
        {
            if (audioType == StoryAudioType.StopBGM) return "Stop BGM";
            return $"Play {audioType}: {(audioClip != null ? audioClip.name : "None")}";
        }
    }
}
