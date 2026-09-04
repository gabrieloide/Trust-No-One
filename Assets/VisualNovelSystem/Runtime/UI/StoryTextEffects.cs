using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum TextVertexEffect
    {
        None,
        Shake,    // Vibración / Pánico / Enfado / Grito
        Tremble,  // Temblor sutil / Miedo / Desesperación
        Wave      // Onda flotante / Misterio / Mareo / Espectral
    }

    public enum TypingDynamics
    {
        Constant,
        Accelerate, // Acelera carácter a carácter (adrenalina, furia, apuro)
        Decelerate  // Desacelera carácter a carácter con pausas (miedo, shock, ahogo)
    }

    public struct CharacterTimingInfo
    {
        public float pauseBefore;
        public float? speedOverride;
        public TypingDynamics dynamics;
        public TextVertexEffect vertexEffect;
        public PortraitEmotion? inlineEmotion;
    }

    public class ParsedDialogueData
    {
        public string cleanText = "";
        public List<CharacterTimingInfo> charTimings = new List<CharacterTimingInfo>();
        public bool hasVertexEffects = false;
        public PortraitEmotion startingPortraitEmotion = PortraitEmotion.None;
    }

    public static class StoryTextEffects
    {
        public static ParsedDialogueData Parse(
            string rawText,
            bool enableAutoPunctuation,
            float commaPause = 0.14f,
            float periodPause = 0.32f,
            float questionExclamationPause = 0.38f,
            float ellipsisPause = 0.52f,
            float ellipsisStepPause = 0.10f,
            float colonPause = 0.18f,
            float dashPause = 0.20f)
        {
            var result = new ParsedDialogueData();
            if (string.IsNullOrEmpty(rawText)) return result;

            var sb = new StringBuilder(rawText.Length);

            // Estado de parsing
            TextVertexEffect currentVertexEffect = TextVertexEffect.None;
            TypingDynamics currentDynamics = TypingDynamics.Constant;
            float? currentSpeedOverride = null;
            PortraitEmotion? pendingPortraitEmotion = null;
            float pendingExplicitPause = 0f;

            int i = 0;
            int n = rawText.Length;

            while (i < n)
            {
                // Detectar etiquetas custom en corchetes: [pause:0.5], [speed:0.02], [shake], [wave], etc.
                if (rawText[i] == '[')
                {
                    int closeBracket = rawText.IndexOf(']', i);
                    if (closeBracket != -1)
                    {
                        string tag = rawText.Substring(i + 1, closeBracket - i - 1).Trim();
                        string tagLower = tag.ToLowerInvariant();

                        bool tagHandled = true;

                        if (tagLower.StartsWith("pause:") || tagLower.StartsWith("wait:") || tagLower.StartsWith("p:") || tagLower.StartsWith("w:"))
                        {
                            string valStr = tag.Substring(tag.IndexOf(':') + 1);
                            if (float.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float pauseSec))
                            {
                                pendingExplicitPause += Mathf.Max(0f, pauseSec);
                            }
                        }
                        else if (tagLower.StartsWith("speed:") || tagLower.StartsWith("s:"))
                        {
                            string valStr = tagLower.Substring(tagLower.IndexOf(':') + 1);
                            if (valStr == "fast" || valStr == "rapido")
                            {
                                currentSpeedOverride = 0.012f;
                            }
                            else if (valStr == "slow" || valStr == "lento")
                            {
                                currentSpeedOverride = 0.075f;
                            }
                            else if (valStr == "normal" || valStr == "reset")
                            {
                                currentSpeedOverride = null;
                            }
                            else if (float.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float speedSec))
                            {
                                currentSpeedOverride = Mathf.Max(0.002f, speedSec);
                            }
                        }
                        else if (tagLower == "accel" || tagLower == "acelera" || tagLower == "panic")
                        {
                            currentDynamics = TypingDynamics.Accelerate;
                        }
                        else if (tagLower == "decel" || tagLower == "desacelera" || tagLower == "fear" || tagLower == "miedo")
                        {
                            currentDynamics = TypingDynamics.Decelerate;
                        }
                        else if (tagLower == "normal" || tagLower == "reset" || tagLower == "/accel" || tagLower == "/decel" || tagLower == "/fear" || tagLower == "/panic" || tagLower == "/miedo" || tagLower == "/speed" || tagLower == "/s")
                        {
                            currentDynamics = TypingDynamics.Constant;
                            currentSpeedOverride = null;
                        }
                        else if (tagLower == "shake")
                        {
                            currentVertexEffect = TextVertexEffect.Shake;
                            result.hasVertexEffects = true;
                            pendingPortraitEmotion = PortraitEmotion.Shake;
                        }
                        else if (tagLower == "/shake")
                        {
                            currentVertexEffect = TextVertexEffect.None;
                        }
                        else if (tagLower == "tremble")
                        {
                            currentVertexEffect = TextVertexEffect.Tremble;
                            result.hasVertexEffects = true;
                            pendingPortraitEmotion = PortraitEmotion.Tremble;
                        }
                        else if (tagLower == "/tremble")
                        {
                            currentVertexEffect = TextVertexEffect.None;
                        }
                        else if (tagLower == "wave")
                        {
                            currentVertexEffect = TextVertexEffect.Wave;
                            result.hasVertexEffects = true;
                        }
                        else if (tagLower == "/wave")
                        {
                            currentVertexEffect = TextVertexEffect.None;
                        }
                        else if (tagLower == "bounce")
                        {
                            pendingPortraitEmotion = PortraitEmotion.Bounce;
                        }
                        else if (tagLower == "punch")
                        {
                            pendingPortraitEmotion = PortraitEmotion.Punch;
                        }
                        else if (tagLower == "nod")
                        {
                            pendingPortraitEmotion = PortraitEmotion.Nod;
                        }
                        else
                        {
                            tagHandled = false;
                        }

                        if (tagHandled)
                        {
                            i = closeBracket + 1;
                            continue;
                        }
                    }
                }

                // Detectar etiquetas TextMeshPro Rich Text estándar: <color=...>, <b>, <i>, <size=...>, etc.
                if (rawText[i] == '<')
                {
                    int closeAngle = rawText.IndexOf('>', i);
                    if (closeAngle != -1)
                    {
                        // Copiar etiqueta completa al cleanText sin contarla como carácter visible
                        string tmpTag = rawText.Substring(i, closeAngle - i + 1);
                        sb.Append(tmpTag);
                        i = closeAngle + 1;
                        continue;
                    }
                }

                // Es un carácter real que se mostrará en pantalla
                char c = rawText[i];
                float charPause = 0f;

                // Detección de pausas por puntuación si está habilitado
                if (enableAutoPunctuation)
                {
                    // Puntos suspensivos ("..." o "…")
                    if (c == '…')
                    {
                        charPause = ellipsisPause;
                    }
                    else if (c == '.' && i + 2 < n && rawText[i + 1] == '.' && rawText[i + 2] == '.')
                    {
                        charPause = ellipsisStepPause;
                    }
                    else if (c == '.' && i > 0 && rawText[i - 1] == '.' && i + 1 < n && rawText[i + 1] == '.')
                    {
                        charPause = ellipsisStepPause;
                    }
                    else if (c == '.' && i >= 2 && rawText[i - 1] == '.' && rawText[i - 2] == '.')
                    {
                        charPause = ellipsisPause;
                    }
                    else if (c == ',')
                    {
                        charPause = commaPause;
                    }
                    else if (c == ';' || c == ':')
                    {
                        charPause = colonPause;
                    }
                    else if (c == '—' || (c == '-' && (i + 1 == n || rawText[i + 1] == ' ' || rawText[i + 1] == '-')))
                    {
                        charPause = dashPause;
                    }
                    else if (c == '?' || c == '!')
                    {
                        if (i + 1 < n && (rawText[i + 1] == '!' || rawText[i + 1] == '?'))
                        {
                            charPause = ellipsisStepPause;
                        }
                        else
                        {
                            charPause = questionExclamationPause;
                        }
                    }
                    else if (c == '.')
                    {
                        bool isDecimal = (i > 0 && char.IsDigit(rawText[i - 1])) && (i + 1 < n && char.IsDigit(rawText[i + 1]));
                        bool isAbbr = IsCommonAbbreviation(rawText, i);
                        if (!isDecimal && !isAbbr)
                        {
                            charPause = periodPause;
                        }
                    }
                }

                // Aplicar pausa explícita acumulada de tags si existe
                if (pendingExplicitPause > 0f)
                {
                    charPause += pendingExplicitPause;
                    pendingExplicitPause = 0f;
                }

                sb.Append(c);

                var timing = new CharacterTimingInfo
                {
                    pauseBefore = charPause,
                    speedOverride = currentSpeedOverride,
                    dynamics = currentDynamics,
                    vertexEffect = currentVertexEffect,
                    inlineEmotion = pendingPortraitEmotion
                };

                pendingPortraitEmotion = null;
                result.charTimings.Add(timing);

                i++;
            }

            result.cleanText = sb.ToString();

            // Detección contextual por emoción inicial si el texto contiene indicadores generales
            if (rawText.Contains("?!") || rawText.Contains("!?") || rawText.EndsWith("!!!"))
            {
                result.startingPortraitEmotion = PortraitEmotion.Shake;
            }
            else if (rawText.EndsWith("!"))
            {
                result.startingPortraitEmotion = PortraitEmotion.Punch;
            }
            else if (rawText.EndsWith("?"))
            {
                result.startingPortraitEmotion = PortraitEmotion.Nod;
            }
            else if (rawText.Contains("...") && (rawText.Contains("afraid") || rawText.Contains("scared") || rawText.Contains("dead") || rawText.Contains("body") || rawText.Contains("blood") || rawText.Contains("murder") || rawText.Contains("kill") || rawText.Contains("lie") || rawText.Contains("knife") || rawText.Contains("glass")))
            {
                result.startingPortraitEmotion = PortraitEmotion.Tremble;
            }

            return result;
        }

        private static bool IsCommonAbbreviation(string text, int dotIndex)
        {
            if (dotIndex <= 0) return false;
            int start = dotIndex - 1;
            while (start >= 0 && char.IsLetter(text[start]))
            {
                start--;
            }
            start++;
            int len = dotIndex - start;
            if (len <= 0) return false;

            // Siglas de una sola letra (ej. "J. Doe", "F. Kennedy")
            if (len == 1) return true;

            string word = text.Substring(start, len).ToLowerInvariant();
            return word == "mr" || word == "mrs" || word == "ms" || word == "dr" || word == "prof" || word == "sr" || word == "sra" || word == "vs" || word == "st" || word == "etc";
        }
    }
}
