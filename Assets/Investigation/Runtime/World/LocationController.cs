using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    [Serializable]
    public class LocationEntry
    {
        public string id;
        public GameObject panelRoot;
    }

    [Serializable]
    public class NavButtonEntry
    {
        public string locationId;
        public GameObject buttonGO;
    }

    [Serializable]
    public class InvestigateHotspotEntry
    {
        public string spotId;
        public GameObject hotspotGO;
    }

    [Serializable]
    public class CharacterHotspotEntry
    {
        public string characterId;
        public string locationId;
        public GameObject hotspotGO;
    }

    // Controla locaciones, navegación progresiva, visibilidad condicional de hotspots, presencia de NPCs y HUD.
    public class LocationController : MonoBehaviour
    {
        public static LocationController Instance { get; private set; }

        [Header("Locaciones y Navegación")]
        [SerializeField] private List<LocationEntry> locations = new List<LocationEntry>();
        [SerializeField] private List<NavButtonEntry> navButtons = new List<NavButtonEntry>();
        [SerializeField] private List<InvestigateHotspotEntry> investigateHotspots = new List<InvestigateHotspotEntry>();
        [SerializeField] private List<CharacterHotspotEntry> characterHotspots = new List<CharacterHotspotEntry>();
        [SerializeField] private string startingLocationId = "";

        [Header("Elementos de HUD")]
        [SerializeField] private GameObject worldUIRoot;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private TextMeshProUGUI hudText;
        [SerializeField] private GameObject navBarRoot;

        private string currentLocationId = "";
        private Coroutine activeTransitionRoutine;

        public string CurrentLocationId => currentLocationId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            StoryInteractable.OnGoToLocationRequested += GoTo;
            PhaseController.OnActionsChanged += RefreshAll;

            if (Room4Controller.Instance == null && GetComponent<Room4Controller>() == null)
            {
                gameObject.AddComponent<Room4Controller>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StoryInteractable.OnGoToLocationRequested -= GoTo;
                PhaseController.OnActionsChanged -= RefreshAll;
            }
        }

        private void Start()
        {
            RefreshAll();
        }

        public void RevealStartingLocation()
        {
            SetWorldUIActive(true);
            string firstId = !string.IsNullOrEmpty(startingLocationId) ? startingLocationId
                : (locations.Count > 0 ? locations[0].id : null);
            if (firstId != null) GoTo(firstId);
            RefreshAll();
        }

        public void SetWorldUIActive(bool active)
        {
            if (worldUIRoot != null) worldUIRoot.SetActive(active);
            if (navBarRoot != null) navBarRoot.SetActive(active);
            if (hudRoot != null) hudRoot.SetActive(active);
        }

        public void HideAll()
        {
            SetWorldUIActive(false);
            foreach (var entry in locations)
            {
                if (entry.panelRoot != null) entry.panelRoot.SetActive(false);
            }
        }

        public void GoTo(string locationId)
        {
            GoTo(locationId, false);
        }

        public void GoTo(string locationId, bool instant)
        {
            if (activeTransitionRoutine != null)
            {
                StopCoroutine(activeTransitionRoutine);
                activeTransitionRoutine = null;
            }

            var fader = StoryUIController.Instance != null ? StoryUIController.Instance.Fader : null;
            bool isAlreadyBlack = fader != null && fader.IsBlack;

            if (instant || isAlreadyBlack || string.IsNullOrEmpty(currentLocationId) || currentLocationId == locationId || StoryUIController.Instance == null)
            {
                ApplyLocationChange(locationId);
            }
            else
            {
                activeTransitionRoutine = StartCoroutine(TransitionLocationRoutine(locationId));
            }
        }

        private System.Collections.IEnumerator TransitionLocationRoutine(string targetLocationId)
        {
            var ui = StoryUIController.Instance;
            if (ui != null)
            {
                ui.HideDialogue();
                // 1. Fade Out a negro (0.22s)
                yield return ui.FadeScreen(1f, Color.black, 0.22f);

                // 2. Cambiar espacio / locación en negro
                ApplyLocationChange(targetLocationId);

                // 3. Fade In revelando el nuevo espacio (0.22s)
                yield return ui.FadeScreen(0f, Color.black, 0.22f);
            }
            else
            {
                ApplyLocationChange(targetLocationId);
            }
            activeTransitionRoutine = null;
        }

        private void ApplyLocationChange(string locationId)
        {
            currentLocationId = locationId;
            foreach (var entry in locations)
            {
                if (entry.panelRoot != null) entry.panelRoot.SetActive(entry.id == locationId);
            }
            RefreshHotspots();
            RefreshCharacters();
        }

        public void RefreshAll()
        {
            RefreshHud();
            RefreshNavBar();
            RefreshHotspots();
            RefreshCharacters();
        }

        public void RefreshHud()
        {
            if (hudText == null) return;

            var cs = CaseState.Instance;
            if (cs == null) return;

            hudText.text = PhaseController.Instance != null && PhaseController.Instance.IsCaseOver
                ? "Investigation over"
                : $"Day {cs.currentDay} · Phase {cs.currentPhase} · Actions left: {cs.actionsRemainingInPhase}";
        }

        public void RefreshNavBar()
        {
            if (CaseState.Instance == null) return;
            int currentDay = CaseState.Instance.currentDay;
            foreach (var nav in navButtons)
            {
                if (nav.buttonGO == null) continue;

                // Día 1: solo motel y carretera
                if (currentDay == 1)
                {
                    bool isDay1Allowed = nav.locationId == "motel" || nav.locationId == "road";
                    nav.buttonGO.SetActive(isDay1Allowed);
                }
                else
                {
                    // Día 2 y 3: todas las locaciones están desbloqueadas
                    nav.buttonGO.SetActive(true);
                }
            }
        }


        public void RefreshHotspots()
        {
            if (DialogueDatabase.Instance == null || CaseState.Instance == null) return;

            foreach (var entry in investigateHotspots)
            {
                if (entry.hotspotGO == null || string.IsNullOrEmpty(entry.spotId)) continue;

                var spotData = DialogueDatabase.Instance.GetInvestigateSpot(entry.spotId);
                if (spotData != null)
                {
                    bool isUnlocked = CaseState.Instance.EvaluateAll(spotData.unlockConditions);
                    entry.hotspotGO.SetActive(isUnlocked);
                }
            }
        }

        public void RefreshCharacters()
        {
            if (CaseState.Instance == null) return;
            int day = CaseState.Instance.currentDay;
            int phase = CaseState.Instance.currentPhase;

            foreach (var entry in characterHotspots)
            {
                if (entry.hotspotGO == null || string.IsNullOrEmpty(entry.characterId)) continue;
                bool isPresent = NPCSchedule.IsCharacterPresent(entry.characterId, entry.locationId, day, phase);
                entry.hotspotGO.SetActive(isPresent);
            }
        }
    }
}
