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

    // Alterna entre paneles de locación (todo en una sola escena, sin scene loading) y
    // mantiene el HUD de día/fase/acciones. Escucha StoryInteractable.OnGoToLocationRequested
    // igual que ConversationController escucha OnOpenConversationRequested.
    public class LocationController : MonoBehaviour
    {
        public static LocationController Instance { get; private set; }

        [SerializeField] private List<LocationEntry> locations = new List<LocationEntry>();
        [SerializeField] private TextMeshProUGUI hudText;
        [SerializeField] private string startingLocationId = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            StoryInteractable.OnGoToLocationRequested += GoTo;
            PhaseController.OnActionsChanged += RefreshHud;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StoryInteractable.OnGoToLocationRequested -= GoTo;
                PhaseController.OnActionsChanged -= RefreshHud;
            }
        }

        private void Start()
        {
            // No navegamos a ninguna locación todavía: los paneles arrancan todos
            // inactivos y GameFlowController llama a RevealStartingLocation() cuando
            // termina la intro, para que no se vean ni se puedan clickear hotspots
            // por detrás de la cutscene inicial.
            RefreshHud();
        }

        public void RevealStartingLocation()
        {
            string firstId = !string.IsNullOrEmpty(startingLocationId) ? startingLocationId
                : (locations.Count > 0 ? locations[0].id : null);
            if (firstId != null) GoTo(firstId);
        }

        // Para cutscenes (ej. la Noche 1): oculta la locación actual mientras dura,
        // para que no queden hotspots clickeables/visibles detrás del overlay/diálogo.
        public void HideAll()
        {
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
        }

        public void RefreshHud()
        {
            if (hudText == null) return;

            var cs = CaseState.Instance;
            hudText.text = PhaseController.Instance.IsCaseOver
                ? "Investigación terminada"
                : $"Día {cs.currentDay} · Fase {cs.currentPhase} · Acciones restantes: {cs.actionsRemainingInPhase}";
        }
    }
}
