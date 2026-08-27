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

    // Controla locaciones, navegación progresiva, visibilidad condicional de hotspots y HUD.
    public class LocationController : MonoBehaviour
    {
        public static LocationController Instance { get; private set; }

        [Header("Locaciones y Navegación")]
        [SerializeField] private List<LocationEntry> locations = new List<LocationEntry>();
        [SerializeField] private List<NavButtonEntry> navButtons = new List<NavButtonEntry>();
        [SerializeField] private List<InvestigateHotspotEntry> investigateHotspots = new List<InvestigateHotspotEntry>();
        [SerializeField] private string startingLocationId = "";

        [Header("Elementos de HUD")]
        [SerializeField] private GameObject worldUIRoot;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private TextMeshProUGUI hudText;
        [SerializeField] private GameObject navBarRoot;
        [SerializeField] private GameObject accuseButtonRoot;

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
            if (accuseButtonRoot != null)
            {
                if (!active) accuseButtonRoot.SetActive(false);
                else RefreshAccuseButton();
            }
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
            foreach (var entry in locations)
            {
                if (entry.panelRoot != null) entry.panelRoot.SetActive(entry.id == locationId);
            }
            RefreshHotspots();
        }

        public void RefreshAll()
        {
            RefreshHud();
            RefreshNavBar();
            RefreshAccuseButton();
            RefreshHotspots();
        }

        public void RefreshHud()
        {
            if (hudText == null) return;

            var cs = CaseState.Instance;
            hudText.text = PhaseController.Instance.IsCaseOver
                ? "Investigación terminada"
                : $"Día {cs.currentDay} · Fase {cs.currentPhase} · Acciones restantes: {cs.actionsRemainingInPhase}";
        }

        public void RefreshNavBar()
        {
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

        public void RefreshAccuseButton()
        {
            if (accuseButtonRoot == null) return;

            // El botón ACUSAR solo es visible en el Día 3 o cuando la investigación termina
            bool isDay3OrOver = CaseState.Instance.currentDay >= 3 || PhaseController.Instance.IsCaseOver;
            accuseButtonRoot.SetActive(isDay3OrOver);
        }

        public void RefreshHotspots()
        {
            if (DialogueDatabase.Instance == null) return;

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
    }
}
