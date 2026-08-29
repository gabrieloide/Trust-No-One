using System;
using UnityEngine;

namespace Investigation
{
    // Día 1: 2 fases. Día 2 y 3: 3 fases. 4 acciones por fase = 32 acciones totales,
    // tal como está mapeado en la matriz maestra del brief.
    public class PhaseController : MonoBehaviour
    {
        private static PhaseController instance;

        public static PhaseController Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("PhaseController");
                    instance = go.AddComponent<PhaseController>();
                }
                return instance;
            }
        }

        private static readonly int[] PhasesPerDay = { 2, 3, 3 };
        private const int ActionsPerPhase = 6;
        private const int TotalDays = 3;

        public static event Action OnActionsChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (!CaseState.Instance.HasFlag(PhaseFlag(1, 1)))
            {
                CaseState.Instance.currentDay = 1;
                CaseState.Instance.currentPhase = 1;
                CaseState.Instance.actionsRemainingInPhase = ActionsPerPhase;
                CaseState.Instance.SetFlag(PhaseFlag(1, 1));
            }
        }

        // Ver CaseState.ResetForNewGame(): mismo motivo, mismo llamador (EndingCreditsUI).
        public static void ResetForNewGame()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }
        }

        public bool IsCaseOver => CaseState.Instance.currentDay > TotalDays;

        public int GetTotalActionsRemainingToday()
        {
            if (IsCaseOver || CaseState.Instance == null) return 0;
            int day = CaseState.Instance.currentDay;
            if (day > TotalDays) return 0;

            int maxPhases = PhasesPerDay[Mathf.Clamp(day - 1, 0, PhasesPerDay.Length - 1)];
            int currentPhase = CaseState.Instance.currentPhase;
            int remainingPhases = Mathf.Max(0, maxPhases - currentPhase);
            return CaseState.Instance.actionsRemainingInPhase + (remainingPhases * ActionsPerPhase);
        }

        public void AdvanceToNextDay()
        {
            if (IsCaseOver || CaseState.Instance == null) return;

            int currentDay = CaseState.Instance.currentDay;
            if (currentDay >= TotalDays)
            {
                CaseState.Instance.currentDay = TotalDays + 1;
                CaseState.Instance.actionsRemainingInPhase = 0;
            }
            else
            {
                int nextDay = currentDay + 1;
                CaseState.Instance.currentDay = nextDay;
                CaseState.Instance.currentPhase = 1;
                CaseState.Instance.actionsRemainingInPhase = ActionsPerPhase;
                CaseState.Instance.SetFlag(PhaseFlag(nextDay, 1));
            }

            OnActionsChanged?.Invoke();
        }

        public void SpendAction()
        {
            if (IsCaseOver || CaseState.Instance.actionsRemainingInPhase <= 0) return;

            CaseState.Instance.actionsRemainingInPhase--;
            if (CaseState.Instance.actionsRemainingInPhase <= 0)
            {
                AdvancePhase();
            }

            OnActionsChanged?.Invoke();
        }

        public void SetPhase(int day, int phase, int actions = ActionsPerPhase)
        {
            day = Mathf.Clamp(day, 1, TotalDays + 1);
            int maxPhase = day <= TotalDays ? PhasesPerDay[day - 1] : 1;
            phase = Mathf.Clamp(phase, 1, maxPhase);

            CaseState.Instance.currentDay = day;
            CaseState.Instance.currentPhase = phase;
            CaseState.Instance.actionsRemainingInPhase = actions;

            if (day <= TotalDays)
            {
                CaseState.Instance.SetFlag(PhaseFlag(day, phase));
            }

            OnActionsChanged?.Invoke();
        }

        private void AdvancePhase()
        {
            int day = CaseState.Instance.currentDay;
            int phase = CaseState.Instance.currentPhase;

            if (day == 1 && phase == 1)
            {
                // D1-Noche (fase 2) es enteramente Evento fijo en la matriz (slots 5-8:
                // Gabe duerme, el grito, encuentran el cuerpo, cierre de día) — no hay
                // ningún Hablar/Investigar/Confrontar propio de esa fase. GameFlowController
                // reproduce esa cutscene automáticamente al detectar el cambio de día, así
                // que saltamos derecho a Día 2 en vez de pedirle al jugador 4 acciones más
                // sobre contenido que no existe.
                CaseState.Instance.SetFlag(PhaseFlag(1, 2));
                day = 2;
                phase = 1;
            }
            else
            {
                int maxPhase = PhasesPerDay[Mathf.Clamp(day - 1, 0, PhasesPerDay.Length - 1)];
                if (phase < maxPhase)
                {
                    phase++;
                }
                else
                {
                    day++;
                    phase = 1;
                }
            }

            CaseState.Instance.currentDay = day;
            CaseState.Instance.currentPhase = phase;
            CaseState.Instance.actionsRemainingInPhase = ActionsPerPhase;

            if (day <= TotalDays)
            {
                CaseState.Instance.SetFlag(PhaseFlag(day, phase));
            }
        }

        private static string PhaseFlag(int day, int phase) => $"d{day}p{phase}_started";
    }
}
