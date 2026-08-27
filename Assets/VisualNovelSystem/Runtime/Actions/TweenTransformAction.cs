using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum TweenType
    {
        MoveTo,
        MoveBy,
        ScaleTo,
        RotateTo,
        FadeAlpha
    }

    [Serializable]
    public class TweenTransformAction : StoryAction
    {
        public string targetObjectName = "Character";
        public TweenType tweenType = TweenType.MoveTo;
        public Vector3 targetVector = Vector3.zero;
        public float targetAlpha = 1f;
        public float duration = 1f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool waitForCompletion = true;

        public override IEnumerator Execute(StoryRunner runner)
        {
            GameObject target = GameObject.Find(targetObjectName);
            if (target == null)
            {
                Debug.LogWarning($"[TweenTransformAction] Target GameObject '{targetObjectName}' not found in scene.");
                yield break;
            }

            if (waitForCompletion)
            {
                yield return RunTween(target);
            }
            else if (runner != null)
            {
                runner.StartCoroutine(RunTween(target));
            }
        }

        private IEnumerator RunTween(GameObject target)
        {
            if (target == null) yield break;

            Transform tr = target.transform;
            RectTransform rectTr = target.GetComponent<RectTransform>();
            CanvasGroup cg = target.GetComponent<CanvasGroup>();
            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();

            Vector3 startPos = rectTr != null ? (Vector3)rectTr.anchoredPosition : tr.position;
            Vector3 endPos = (tweenType == TweenType.MoveBy) ? startPos + targetVector : targetVector;
            Vector3 startScale = tr.localScale;
            Quaternion startRot = tr.rotation;
            Quaternion endRot = Quaternion.Euler(targetVector);
            float startAlpha = cg != null ? cg.alpha : (sr != null ? sr.color.a : 1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float evalT = (curve != null) ? curve.Evaluate(t) : t;

                switch (tweenType)
                {
                    case TweenType.MoveTo:
                    case TweenType.MoveBy:
                        if (rectTr != null)
                            rectTr.anchoredPosition = Vector3.Lerp(startPos, endPos, evalT);
                        else
                            tr.position = Vector3.Lerp(startPos, endPos, evalT);
                        break;

                    case TweenType.ScaleTo:
                        tr.localScale = Vector3.Lerp(startScale, targetVector, evalT);
                        break;

                    case TweenType.RotateTo:
                        tr.rotation = Quaternion.Slerp(startRot, endRot, evalT);
                        break;

                    case TweenType.FadeAlpha:
                        float curA = Mathf.Lerp(startAlpha, targetAlpha, evalT);
                        if (cg != null) cg.alpha = curA;
                        if (sr != null)
                        {
                            Color c = sr.color;
                            c.a = curA;
                            sr.color = c;
                        }
                        break;
                }

                yield return null;
            }

            // Final snap
            if (target != null)
            {
                switch (tweenType)
                {
                    case TweenType.MoveTo:
                    case TweenType.MoveBy:
                        if (rectTr != null) rectTr.anchoredPosition = endPos;
                        else tr.position = endPos;
                        break;
                    case TweenType.ScaleTo:
                        tr.localScale = targetVector;
                        break;
                    case TweenType.RotateTo:
                        tr.rotation = endRot;
                        break;
                    case TweenType.FadeAlpha:
                        if (cg != null) cg.alpha = targetAlpha;
                        if (sr != null)
                        {
                            Color c = sr.color;
                            c.a = targetAlpha;
                            sr.color = c;
                        }
                        break;
                }
            }
        }

        public override string GetSummary()
        {
            return $"Tween '{targetObjectName}' -> {tweenType} ({duration}s)";
        }
    }
}
