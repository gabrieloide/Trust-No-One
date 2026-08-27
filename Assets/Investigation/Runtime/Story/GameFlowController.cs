using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Beats de "Evento" fijos de la matriz (slots 1, 5-8, 9) que no son conversación con
    // nadie: la intro del Día 1 y el cierre de la Noche 1 (el crimen), que también es
    // donde se otorgan automáticamente las dos primeras pistas (elena_seen_running,
    // robert_quick_arrival). El resto de la matriz son acciones del jugador, ya cubiertas
    // por ConversationController.
    public class GameFlowController : MonoBehaviour
    {
        private int lastSeenDay = 1;

        private StoryUIController UI => StoryUIController.Instance;

        private void Awake()
        {
            PhaseController.OnActionsChanged += CheckDayTransition;
        }

        private void OnDestroy()
        {
            PhaseController.OnActionsChanged -= CheckDayTransition;
        }

        private void Start()
        {
            StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            LocationController.Instance.HideAll();
            yield return UI.ShowOverlay("DÍA 1", "La carretera de ningún lugar", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
            yield return UI.ShowDialogue("", "El auto se apaga a diez minutos de cualquier cosa. El único cartel en kilómetros dice MOTEL, con una flecha pintada a mano.", null, null, -1f, true);
            yield return UI.ShowDialogue("", "No va a venir ninguna grúa antes de mañana. Voy a tener que quedarme.", null, null, -1f, true);
            LocationController.Instance.RevealStartingLocation();
        }

        private void CheckDayTransition()
        {
            int day = CaseState.Instance.currentDay;
            if (day == lastSeenDay) return;

            int fromDay = lastSeenDay;
            lastSeenDay = day;

            if (fromDay == 1 && day == 2)
            {
                StartCoroutine(Day1NightRoutine());
            }
            else if (day <= 3)
            {
                StartCoroutine(SimpleDayOverlay(day));
            }
        }

        private IEnumerator Day1NightRoutine()
        {
            LocationController.Instance.HideAll();

            yield return UI.ShowOverlay("", "Esa noche...", OverlayDisplayMode.TopHeader, OverlayEffect.Fade, 2f, true);
            yield return UI.ShowDialogue("", "Un grito corto, cortado a la mitad. Después, vidrio rompiéndose.", null, null, -1f, true);
            yield return UI.ShowDialogue("", "Cuando salgo al pasillo alcanzo a ver a Elena, corriendo en dirección contraria al ruido.", null, null, -1f, true);
            CaseState.Instance.CollectClue("elena_seen_running");

            yield return UI.ShowDialogue("", "Para cuando llego, ya hay gente alrededor del cuerpo. Alguien fue a buscar a Robert.", null, null, -1f, true);
            yield return UI.ShowDialogue("", "Llega antes de lo que debería tardar cualquiera en despertarse y vestirse. Sin marcas, sin agitación, con la explicación ya lista.", null, null, -1f, true);
            CaseState.Instance.CollectClue("robert_quick_arrival");

            yield return UI.ShowOverlay("", "Fin del Día 1", OverlayDisplayMode.BottomTimestamp, OverlayEffect.Fade, 2f, true);
            yield return UI.ShowOverlay("DÍA 2", "La lista de sospechosos no incluye a Robert Hale", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2.5f, true);

            LocationController.Instance.RevealStartingLocation();
        }

        private IEnumerator SimpleDayOverlay(int day)
        {
            yield return UI.ShowOverlay("DÍA " + day, day >= 3 ? "Última oportunidad para acusar" : "", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
        }
    }
}
