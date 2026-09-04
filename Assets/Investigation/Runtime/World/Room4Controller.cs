using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Controla las interacciones específicas de la Habitación 4 (Cama, Espejo, Escritorio)
    public class Room4Controller : MonoBehaviour
    {
        public static Room4Controller Instance { get; private set; }

        private bool isBusy;
        private StoryUIController UI => StoryUIController.Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            StoryInteractable.OnEndDayRequested += HandleEndDayBed;
            StoryInteractable.OnRoomMirrorRequested += HandleRoomMirror;
            StoryInteractable.OnRoomDeskRequested += HandleRoomDesk;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StoryInteractable.OnEndDayRequested -= HandleEndDayBed;
                StoryInteractable.OnRoomMirrorRequested -= HandleRoomMirror;
                StoryInteractable.OnRoomDeskRequested -= HandleRoomDesk;
            }
        }

        public void HandleEndDayBed()
        {
            if (isBusy) return;
            StartCoroutine(EndDayRoutine());
        }

        public void HandleRoomMirror()
        {
            if (isBusy) return;
            StartCoroutine(RoomMirrorRoutine());
        }

        public void HandleRoomDesk()
        {
            if (isBusy) return;
            // Abre el tablero de deducción / análisis del caso
            StoryInteractable.TriggerOpenClueBoard();
        }

        private IEnumerator EndDayRoutine()
        {
            isBusy = true;

            int actionsLeft = PhaseController.Instance != null ? PhaseController.Instance.GetTotalActionsRemainingToday() : 0;
            int day = CaseState.Instance != null ? CaseState.Instance.currentDay : 1;

            if (day >= 3)
            {
                var options = new List<StoryChoiceOption>
                {
                    new StoryChoiceOption { id = "sleep", text = "Yes, wrap up the investigation" },
                    new StoryChoiceOption { id = "stay", text = "Keep investigating" }
                };

                int selected = -1;
                yield return UI.ShowChoices("Sundown ends it. The sheriff will arrive at nightfall to close the case, and whatever is still buried stays buried.\n\nAre you ready to close your investigation and prepare your final accusation?", options, idx => selected = idx);

                if (selected == 0)
                {
                    UI.HideDialogue();
                    PhaseController.Instance.AdvanceToNextDay();
                }
                else
                {
                    UI.HideDialogue();
                }
            }
            else
            {
                var options = new List<StoryChoiceOption>
                {
                    new StoryChoiceOption { id = "sleep", text = "Yes, turn in for the night" },
                    new StoryChoiceOption { id = "stay", text = "Keep investigating" }
                };

                int selected = -1;
                yield return UI.ShowChoices("The sun is still up, and the clock is ticking. Leads won't stay warm once the night sets in.\n\nAre you sure you want to turn in until tomorrow morning?", options, idx => selected = idx);

                if (selected == 0)
                {
                    UI.HideDialogue();
                    PhaseController.Instance.AdvanceToNextDay();
                }
                else
                {
                    UI.HideDialogue();
                }
            }

            isBusy = false;
        }

        private IEnumerator RoomMirrorRoutine()
        {
            isBusy = true;

            int day = CaseState.Instance != null ? CaseState.Instance.currentDay : 1;
            string monologue;

            switch (day)
            {
                case 1:
                    monologue = "Tired eyes staring back from the grimy mirror. Just need the repair shop to open tomorrow so I can leave this roadside purgatory.";
                    break;
                case 2:
                    monologue = "A dead girl in the bushes, and an entire motel pretending they didn't hear the glass break. Nobody in this place is innocent... I can feel it in the cold air.";
                    break;
                case 3:
                default:
                    monologue = "Time is running out. By nightfall the county sheriff will arrive to close the file. I need to be damn sure before I point my finger at someone.";
                    break;
            }

            yield return UI.ShowDialogue("Gabe", monologue, null, null, -1f, true);
            UI.HideDialogue();

            isBusy = false;
        }
    }
}
