using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Investigation
{
    // Herramienta de QA: setea flags de CaseState al arrancar la escena, para probar
    // contenido de un día/fase específico sin tener todavía el controlador de fases real
    // (Etapa 4 del plan). Se usa en escenas de prueba, no en el flujo final del juego.
    public class DebugPhaseSetter : MonoBehaviour
    {
        [SerializeField] private List<string> flagsToSetOnStart = new List<string>();

        [Header("QA: abrir una conversación automáticamente")]
        [SerializeField] private string autoOpenCharacterId = "";
        [SerializeField] private float autoOpenDelaySeconds = 0.5f;

        [Header("QA: investigar un punto automáticamente")]
        [SerializeField] private string autoInvestigateSpotId = "";

        [Header("QA: recolectar una pista directamente (sin diálogo)")]
        [SerializeField] private string autoCollectClueId = "";

        [Header("QA: abrir la acusación automáticamente")]
        [SerializeField] private bool autoOpenAccusation = false;

        [Header("QA: forzar día > 3 para que IsCaseOver sea true sin jugar las 32 acciones")]
        [SerializeField] private bool forceCaseOver = false;

        [Header("QA: recolectar TODAS las pistas del juego (para probar cualquier combinación sospechoso+evidencia)")]
        [SerializeField] private bool autoCollectAllClues = false;

        [Header("QA: saltar la intro y revelar la locación ya")]
        [SerializeField] private bool autoRevealLocation = false;

        [Header("QA: gastar N acciones directamente (sin diálogo)")]
        [SerializeField] private int autoSpendActions = 0;

        [SerializeField] private bool logDatabaseSummaryOnStart = false;

        private void Awake()
        {
            // Awake corre para todos los objetos de la escena antes que cualquier Start(),
            // así que esto gana la carrera contra GameFlowController.Start() sin importar
            // el orden de ejecución de scripts. Sin esto, el prólogo (Día 1) y la pantalla
            // de acusación intentan mostrar diálogo al mismo tiempo y el texto se solapa.
            if (autoOpenAccusation)
            {
                var flow = FindFirstObjectByType<GameFlowController>();
                if (flow != null) flow.enabled = false;
            }
        }

        private void Start()
        {
            if (forceCaseOver)
            {
                // PhaseController.IsCaseOver exige currentDay > TotalDays (3).
                CaseState.Instance.currentDay = 4;

                // LocationController solo refresca el botón ACUSAR cuando PhaseController
                // dispara OnActionsChanged; como aquí saltamos ese flujo, el botón puede
                // quedar leyendo el día viejo (oculto) si su propio Start() corrió antes que
                // el nuestro. Forzamos el refresco explícitamente para no depender del orden.
                if (LocationController.Instance != null)
                {
                    LocationController.Instance.RefreshAccuseButton();
                }
            }

            foreach (var flag in flagsToSetOnStart)
            {
                if (!string.IsNullOrEmpty(flag)) CaseState.Instance.SetFlag(flag);
            }

            if (logDatabaseSummaryOnStart)
            {
                var db = DialogueDatabase.Instance;
                foreach (var c in db.AllCharacters)
                {
                    Debug.Log($"[DEBUG] character={c.id} topics={c.topics.Count}");
                }
                Debug.Log($"[DEBUG] clues={db.AllClues.Count()} investigateSpots={db.AllInvestigateSpots.Count()}");
            }

            if (!string.IsNullOrEmpty(autoOpenCharacterId))
            {
                StartCoroutine(AutoOpenRoutine());
            }

            if (!string.IsNullOrEmpty(autoInvestigateSpotId))
            {
                StartCoroutine(AutoInvestigateRoutine());
            }

            if (!string.IsNullOrEmpty(autoCollectClueId))
            {
                CaseState.Instance.CollectClue(autoCollectClueId);
            }

            if (autoCollectAllClues)
            {
                foreach (var clue in DialogueDatabase.Instance.AllClues)
                {
                    CaseState.Instance.CollectClue(clue.id, playSound: false);
                }
            }

            if (autoOpenAccusation)
            {
                AccusationController.Instance.BeginAccusation();
            }

            if (autoRevealLocation)
            {
                LocationController.Instance.RevealStartingLocation();
            }

            for (int i = 0; i < autoSpendActions; i++)
            {
                PhaseController.Instance.SpendAction();
            }
        }

        private IEnumerator AutoOpenRoutine()
        {
            yield return new WaitForSeconds(autoOpenDelaySeconds);
            ConversationController.Instance.Open(autoOpenCharacterId);
        }

        private IEnumerator AutoInvestigateRoutine()
        {
            yield return new WaitForSeconds(autoOpenDelaySeconds);
            ConversationController.Instance.Investigate(autoInvestigateSpotId);
        }
    }
}
