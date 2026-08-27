using System;
using System.Collections.Generic;
using UnityEngine;

namespace Investigation
{
    // Único punto de verdad persistente del caso: temas desbloqueados/vistos por
    // personaje, pistas recolectadas y confrontaciones. A diferencia del StoryBlackboard
    // de VisualNovelSystem (embebido por StoryGraph y reseteado en cada StartStory),
    // esta instancia vive toda la partida y es compartida por todos los personajes.
    public class CaseState : MonoBehaviour
    {
        private static CaseState instance;

        // Getter perezoso en vez de RuntimeInitializeOnLoadMethod: con "Reload Domain"
        // desactivado en Enter Play Mode Settings, ese hook solo dispara una vez por
        // sesión del Editor, no en cada entrada a Play Mode. El getter garantiza una
        // instancia válida sin importar esa configuración.
        public static CaseState Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("CaseState");
                    instance = go.AddComponent<CaseState>();
                }
                return instance;
            }
        }

        [Header("Progreso temporal")]
        public int currentDay = 1;
        public int currentPhase = 1;
        public int actionsRemainingInPhase = 4;

        private readonly HashSet<string> flags = new HashSet<string>();
        private readonly HashSet<string> unlockedTopics = new HashSet<string>();
        private readonly HashSet<string> seenTopics = new HashSet<string>();
        private readonly HashSet<string> collectedClues = new HashSet<string>();
        private readonly HashSet<string> confrontations = new HashSet<string>();
        private readonly Dictionary<string, int> counters = new Dictionary<string, int>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetFlag(string flag) => flags.Add(flag);
        public void ClearFlag(string flag) => flags.Remove(flag);
        public bool HasFlag(string flag) => flags.Contains(flag);

        public void UnlockTopic(string characterId, string topicId) => unlockedTopics.Add(TopicKey(characterId, topicId));
        public bool IsTopicUnlocked(string characterId, string topicId) => unlockedTopics.Contains(TopicKey(characterId, topicId));

        public void MarkTopicSeen(string characterId, string topicId) => seenTopics.Add(TopicKey(characterId, topicId));
        public bool HasSeenTopic(string characterId, string topicId) => seenTopics.Contains(TopicKey(characterId, topicId));

        public static event Action OnClueCollected;

        public void CollectClue(string clueId, bool playSound = true)
        {
            if (collectedClues.Add(clueId))
            {
                if (playSound)
                {
                    AudioManager.Play(SFXType.ClueFound);
                }
                OnClueCollected?.Invoke();
            }
        }

        public bool HasClue(string clueId) => collectedClues.Contains(clueId);
        public IReadOnlyCollection<string> CollectedClues => collectedClues;

        public void RecordConfrontation(string characterId, string clueId)
        {
            confrontations.Add(ConfrontKey(characterId, clueId));
            AudioManager.Play(SFXType.ConfrontationSlam);
        }
        public bool WasConfrontedWith(string characterId, string clueId) => confrontations.Contains(ConfrontKey(characterId, clueId));

        public int IncrementCounter(string key, int amount = 1)
        {
            counters.TryGetValue(key, out int current);
            counters[key] = current + amount;
            return counters[key];
        }

        public int GetCounter(string key) => counters.TryGetValue(key, out int value) ? value : 0;

        public bool Evaluate(ConditionData c)
        {
            bool result;
            switch (c.kind)
            {
                case ConditionKind.Flag: result = HasFlag(c.a); break;
                case ConditionKind.TopicUnlocked: result = IsTopicUnlocked(c.a, c.b); break;
                case ConditionKind.TopicSeen: result = HasSeenTopic(c.a, c.b); break;
                case ConditionKind.ClueCollected: result = HasClue(c.a); break;
                case ConditionKind.Confronted: result = WasConfrontedWith(c.a, c.b); break;
                case ConditionKind.CounterAtLeast:
                    int.TryParse(c.b, out int threshold);
                    result = GetCounter(c.a) >= threshold;
                    break;
                default: result = true; break;
            }
            return c.negate ? !result : result;
        }

        public bool EvaluateAll(List<ConditionData> conditions)
        {
            if (conditions == null) return true;
            foreach (var c in conditions)
            {
                if (!Evaluate(c)) return false;
            }
            return true;
        }

        public void Apply(EffectData e)
        {
            switch (e.kind)
            {
                case EffectKind.SetFlag: SetFlag(e.a); break;
                case EffectKind.ClearFlag: ClearFlag(e.a); break;
                case EffectKind.UnlockTopic: UnlockTopic(e.a, e.b); break;
                case EffectKind.CollectClue: CollectClue(e.a); break;
                case EffectKind.IncrementCounter:
                    int amount = 1;
                    if (!string.IsNullOrEmpty(e.b)) int.TryParse(e.b, out amount);
                    IncrementCounter(e.a, amount);
                    break;
            }
        }

        public void ApplyAll(List<EffectData> effects)
        {
            if (effects == null) return;
            foreach (var e in effects) Apply(e);
        }

        private static string TopicKey(string characterId, string topicId) => $"{characterId}:{topicId}";
        private static string ConfrontKey(string characterId, string clueId) => $"{characterId}:{clueId}";
    }
}
