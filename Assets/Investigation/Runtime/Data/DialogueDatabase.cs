using System.Collections.Generic;
using UnityEngine;

namespace Investigation
{
    // Carga todo Resources/Dialogue/** una sola vez. Para agregar contenido de un
    // personaje nuevo alcanza con soltar el .json en Characters/ — no requiere wiring
    // manual en el Inspector.
    public class DialogueDatabase : MonoBehaviour
    {
        private static DialogueDatabase instance;

        // Getter perezoso (ver CaseState.Instance): no depende de RuntimeInitializeOnLoadMethod,
        // que con "Reload Domain" desactivado no vuelve a disparar en cada Play Mode.
        public static DialogueDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<DialogueDatabase>();
                    if (instance == null)
                    {
                        var go = new GameObject("DialogueDatabase");
                        instance = go.AddComponent<DialogueDatabase>();
                        instance.LoadAll();
                    }
                }
                if (instance.characters.Count == 0)
                {
                    instance.LoadAll();
                }
                return instance;
            }
        }

        private const string CharactersResourcePath = "Dialogue/Characters";
        private const string CluesResourcePath = "Dialogue/clues";
        private const string InvestigateResourcePath = "Dialogue/investigate";

        private readonly Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();
        private readonly Dictionary<string, ClueData> clues = new Dictionary<string, ClueData>();
        private readonly Dictionary<string, InvestigateSpotData> investigateSpots = new Dictionary<string, InvestigateSpotData>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        private void LoadAll()
        {
            characters.Clear();
            foreach (var file in Resources.LoadAll<TextAsset>(CharactersResourcePath))
            {
                var data = JsonUtility.FromJson<CharacterData>(file.text);
                if (data != null && !string.IsNullOrEmpty(data.id))
                {
                    characters[data.id] = data;
                }
                else
                {
                    Debug.LogWarning($"[DialogueDatabase] '{file.name}' no tiene un 'id' válido, se ignora.");
                }
            }

            clues.Clear();
            var cluesFile = Resources.Load<TextAsset>(CluesResourcePath);
            if (cluesFile != null)
            {
                var db = JsonUtility.FromJson<ClueDatabaseFile>(cluesFile.text);
                if (db != null && db.clues != null)
                {
                    foreach (var clue in db.clues)
                    {
                        if (!string.IsNullOrEmpty(clue.id)) clues[clue.id] = clue;
                    }
                }
            }

            investigateSpots.Clear();
            var investigateFile = Resources.Load<TextAsset>(InvestigateResourcePath);
            if (investigateFile != null)
            {
                var db = JsonUtility.FromJson<InvestigateSpotDatabaseFile>(investigateFile.text);
                if (db != null && db.spots != null)
                {
                    foreach (var spot in db.spots)
                    {
                        if (!string.IsNullOrEmpty(spot.id)) investigateSpots[spot.id] = spot;
                    }
                }
            }
        }

        public CharacterData GetCharacter(string characterId)
        {
            return characters.TryGetValue(characterId, out var data) ? data : null;
        }

        // Los diálogos usan solo el nombre de pila como "speaker" ("Robert", "Elena"...).
        // Esto lo resuelve contra el nombre completo del personaje (con apellido) para que
        // no haga falta llegar al final del juego para verlo. Nombres sin personaje
        // registrado (Gabe, Sheriff, narrador) pasan sin cambios.
        public string ResolveSpeakerDisplayName(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return speaker;
            return characters.TryGetValue(speaker.ToLowerInvariant(), out var data) ? data.displayName : speaker;
        }

        public ClueData GetClue(string clueId)
        {
            return clues.TryGetValue(clueId, out var data) ? data : null;
        }

        public InvestigateSpotData GetInvestigateSpot(string spotId)
        {
            return investigateSpots.TryGetValue(spotId, out var data) ? data : null;
        }

        public ConfrontationReactionData GetConfrontation(string characterId, string clueId)
        {
            var character = GetCharacter(characterId);
            if (character == null || character.confrontations == null) return null;

            foreach (var conf in character.confrontations)
            {
                if (conf.clueId == clueId) return conf;
                if (conf.alternativeClueIds != null && conf.alternativeClueIds.Contains(clueId)) return conf;
            }

            return null;
        }

        public string GetAmbientGreeting(string characterId, int day, int phase)
        {
            var character = GetCharacter(characterId);
            if (character == null || character.ambientGreetings == null || character.ambientGreetings.Count == 0)
            {
                return "Yes? How can I help you?";
            }

            // Buscar coincidencia exacta de día y fase
            foreach (var g in character.ambientGreetings)
            {
                if (g.day == day && g.phase == phase && !string.IsNullOrEmpty(g.text))
                {
                    return g.text;
                }
            }

            // Fallback: coincidencia de día con phase 0 (cualquiera)
            foreach (var g in character.ambientGreetings)
            {
                if (g.day == day && g.phase == 0 && !string.IsNullOrEmpty(g.text))
                {
                    return g.text;
                }
            }

            // Fallback: primer saludo disponible
            return character.ambientGreetings[0].text;
        }

        public string GetDefaultRejection(string characterId)
        {
            var character = GetCharacter(characterId);
            if (character != null && !string.IsNullOrEmpty(character.defaultRejection))
            {
                return character.defaultRejection;
            }
            return "I have nothing to say about that object.";
        }

        public IEnumerable<CharacterData> AllCharacters => characters.Values;
        public IEnumerable<ClueData> AllClues => clues.Values;
        public IEnumerable<InvestigateSpotData> AllInvestigateSpots => investigateSpots.Values;
    }
}
