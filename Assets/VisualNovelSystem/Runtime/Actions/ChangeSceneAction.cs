using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    public enum SceneChangeType
    {
        UnityScene,
        BackgroundSprite
    }

    [Serializable]
    public class ChangeSceneAction : StoryAction
    {
        public SceneChangeType changeType = SceneChangeType.UnityScene;

        [Header("Unity Scene Settings")]
        public string sceneName = "";
        public LoadSceneMode loadMode = LoadSceneMode.Single;

        [Header("Background Sprite Settings")]
        public Sprite newBackgroundSprite;
        public string backgroundObjectName = "Background";

        [Header("Fade Transition")]
        public bool fadeOut = true;
        public float fadeOutDuration = 0.8f;
        public bool fadeIn = true;
        public float fadeInDuration = 0.8f;
        public Color fadeColor = Color.black;

        public override IEnumerator Execute(StoryRunner runner)
        {
            var fader = runner != null && runner.UIController != null ? runner.UIController.Fader : null;

            // 1. Fade Out
            if (fadeOut && fader != null)
            {
                yield return fader.FadeRoutine(1.0f, fadeColor, fadeOutDuration);
            }

            // 2. Perform Change
            if (changeType == SceneChangeType.UnityScene)
            {
                if (!string.IsNullOrEmpty(sceneName))
                {
                    var asyncOp = SceneManager.LoadSceneAsync(sceneName, loadMode);
                    while (!asyncOp.isDone)
                    {
                        yield return null;
                    }
                }
            }
            else // BackgroundSprite
            {
                GameObject bgObj = GameObject.Find(backgroundObjectName);
                if (bgObj != null)
                {
                    var img = bgObj.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = newBackgroundSprite;
                    }
                    else
                    {
                        var sr = bgObj.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sprite = newBackgroundSprite;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ChangeSceneAction] Could not find Background object named '{backgroundObjectName}' in scene.");
                }
            }

            // 3. Fade In
            if (fadeIn && fader != null)
            {
                yield return fader.FadeRoutine(0.0f, fadeColor, fadeInDuration);
            }
        }

        public override string GetSummary()
        {
            if (changeType == SceneChangeType.UnityScene)
                return $"Change Scene -> '{sceneName}'";
            return $"Change BG -> {(newBackgroundSprite != null ? newBackgroundSprite.name : "None")}";
        }
    }
}
