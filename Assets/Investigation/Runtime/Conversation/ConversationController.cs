using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Maneja conversaciones interactivas, selección de temas, saludos ambientales y confrontación con evidencias.
    public class ConversationController : MonoBehaviour
    {
        public static ConversationController Instance { get; private set; }

        private const string LeaveOptionId = "__leave__";
        private const string PresentEvidenceOptionId = "__present_evidence__";
        private bool isBusy = false;
        public bool IsBusy => isBusy;

        private StoryUIController UI => StoryUIController.Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            StoryInteractable.OnOpenConversationRequested += Open;
            StoryInteractable.OnInvestigateRequested += Investigate;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StoryInteractable.OnOpenConversationRequested -= Open;
                StoryInteractable.OnInvestigateRequested -= Investigate;
            }
        }

        public void Open(string characterId)
        {
            if (isBusy) return;
            StartCoroutine(OpenRoutine(characterId));
        }

        public void Confront(string characterId, string clueId)
        {
            if (isBusy) return;
            StartCoroutine(ConfrontRoutine(characterId, clueId));
        }

        public void Investigate(string spotId)
        {
            if (isBusy) return;
            StartCoroutine(InvestigateRoutine(spotId));
        }

        private IEnumerator OpenRoutine(string characterId)
        {
            isBusy = true;

            var character = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetCharacter(characterId) : null;
            if (character == null)
            {
                Debug.LogWarning($"[ConversationController] No hay datos de diálogo para '{characterId}'.");
                isBusy = false;
                yield break;
            }

            int day = CaseState.Instance != null ? CaseState.Instance.currentDay : 2;
            int phase = CaseState.Instance != null ? CaseState.Instance.currentPhase : 1;

            // 1. Saludo / Diálogo ambiental del personaje antes del menú de temas
            string greeting = GetAmbientGreeting(character.id, day, phase);
            if (!string.IsNullOrEmpty(greeting))
            {
                yield return UI.ShowDialogue(character.displayName, greeting, null, null, -1f, true);
            }

            // 2. Obtener temas disponibles para la fase actual
            var visibleTopics = GetVisibleTopics(character);

            var options = visibleTopics.Select(t =>
            {
                bool seen = CaseState.Instance != null && CaseState.Instance.HasSeenTopic(character.id, t.id);
                string label = seen ? $"{t.displayName} [✓]" : t.displayName;
                return new StoryChoiceOption { id = t.id, text = label };
            }).ToList();

            // Opción de presentar evidencia si el jugador tiene pistas
            if (CaseState.Instance != null && CaseState.Instance.CollectedClues.Count > 0)
            {
                options.Add(new StoryChoiceOption { id = PresentEvidenceOptionId, text = "🔍 Presentar Evidencia..." });
            }

            options.Add(new StoryChoiceOption { id = LeaveOptionId, text = "Dejar de hablar por ahora" });

            int selected = -1;
            yield return UI.ShowChoices($"¿De qué hablar con {character.displayName}?", options, idx => selected = idx);

            if (selected >= 0 && selected < options.Count)
            {
                string chosenId = options[selected].id;
                if (chosenId == PresentEvidenceOptionId)
                {
                    yield return PresentEvidenceRoutine(character);
                }
                else if (chosenId != LeaveOptionId)
                {
                    var topic = visibleTopics.First(t => t.id == chosenId);
                    yield return PlayTopic(character, topic);
                }
                else
                {
                    UI.HideDialogue();
                }
            }
            else
            {
                UI.HideDialogue();
            }

            isBusy = false;
        }

        private IEnumerator PresentEvidenceRoutine(CharacterData character)
        {
            var clues = CaseState.Instance.CollectedClues
                .Select(id => DialogueDatabase.Instance.GetClue(id))
                .Where(c => c != null)
                .ToList();

            if (clues.Count == 0)
            {
                yield return UI.ShowDialogue(character.displayName, "No tienes ninguna pista para mostrarme.", null, null, -1f, true);
                UI.HideDialogue();
                yield break;
            }

            var clueOptions = clues.Select(c => new StoryChoiceOption { id = c.id, text = c.displayName }).ToList();
            clueOptions.Add(new StoryChoiceOption { id = "__cancel__", text = "Volver a las preguntas" });

            int clueIndex = -1;
            yield return UI.ShowChoices($"¿Qué evidencia presentar a {character.displayName}?", clueOptions, idx => clueIndex = idx);

            if (clueIndex >= 0 && clueIndex < clueOptions.Count && clueOptions[clueIndex].id != "__cancel__")
            {
                string clueId = clueOptions[clueIndex].id;
                yield return ConfrontRoutine(character.id, clueId);
            }
            else
            {
                UI.HideDialogue();
            }
        }

        public IEnumerator ConfrontRoutine(string characterId, string clueId)
        {
            isBusy = true;

            var character = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetCharacter(characterId) : null;
            var clue = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetClue(clueId) : null;

            if (character != null && clue != null)
            {
                CaseState.Instance.RecordConfrontation(characterId, clueId);

                // Banner de confrontación
                yield return UI.ShowOverlay("EVIDENCIA PRESENTADA", clue.displayName, OverlayDisplayMode.TopHeader, OverlayEffect.Instant, 1.8f, true);

                bool isRelevant = ExecuteConfrontationReaction(characterId, clueId, out List<(string speaker, string text)> lines, out Action onFinish);

                foreach (var line in lines)
                {
                    yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
                }

                if (isRelevant)
                {
                    // Solo consume acción si es la primera vez que se descubre esta contradicción
                    string confrontKey = $"confront_{characterId}_{clueId}";
                    if (CaseState.Instance != null && !CaseState.Instance.HasFlag(confrontKey))
                    {
                        CaseState.Instance.SetFlag(confrontKey);
                        if (PhaseController.Instance != null)
                        {
                            PhaseController.Instance.SpendAction();
                        }
                    }
                    onFinish?.Invoke();
                }

                UI.HideDialogue();
            }

            isBusy = false;
        }

        private bool ExecuteConfrontationReaction(string characterId, string clueId, out List<(string speaker, string text)> lines, out Action onFinish)
        {
            lines = new List<(string speaker, string text)>();
            onFinish = null;

            switch (characterId)
            {
                case "ernesto":
                    if (clueId == "carpet_fiber")
                    {
                        lines.Add(("Gabe", "Encontré esta fibra sintética verde oliva cerca de donde murió Carla. Es idéntica al rollo que tiene en su mostrador."));
                        lines.Add(("Ernesto", "¡Medio pueblo tiene esa alfombra, detective! No prueba nada... ¡nada!"));
                        lines.Add(("Gabe", "¿Y por qué le tiemblan las manos, Ernesto? ¿De dónde es ese corte en la muñeca?"));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("ernesto_fiber_trapped");
                            CaseState.Instance.CollectClue("ernesto_false_alibi");
                            CaseState.Instance.CollectClue("ernesto_hand_cut");
                        };
                        return true;
                    }
                    if (clueId == "carpet_shop_receipt")
                    {
                        lines.Add(("Gabe", "Tres ventas en dos meses, Ernesto. Este negocio se cae a pedazos. ¿De dónde saca la plata para pagar el alquiler?"));
                        lines.Add(("Ernesto", "¿Ahora revisa mi basura? Robert me dio una prórroga... solo le hago unos favores de mantenimiento, nada más."));
                        lines.Add(("Gabe", "¿Favores nocturnos en el sótano del motel?"));
                        lines.Add(("Ernesto", "¡Yo no dije eso! ¡Cállese!"));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("ernesto_debt_revealed");
                            CaseState.Instance.SetFlag("robert_leverage_ernesto");
                        };
                        return true;
                    }
                    lines.Add(("Ernesto", "¿Y qué quiere que haga con eso? No tengo tiempo para jugar a los detectives. Compre algo o váyase."));
                    return false;

                case "robert":
                    if (clueId == "basement_lock")
                    {
                        lines.Add(("Gabe", "El candado del sótano no fue forzado desde afuera: cedió por un fuerte impacto desde adentro."));
                        lines.Add(("Robert", "Es un edificio antiguo, detective. La humedad hincha las vigas y los cerrojos ceden. O quizá algún intruso intentó refugiarse de la tormenta."));
                        lines.Add(("Gabe", "Usted tiene la única llave. Y la puerta estaba cerrada por fuera. No tiene sentido que un intruso se encierre a sí mismo."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("robert_cornered_basement");
                            CaseState.Instance.CollectClue("basement_exclusive_access");
                        };
                        return true;
                    }
                    if (clueId == "robert_quick_arrival")
                    {
                        lines.Add(("Gabe", "Los primeros testigos tardaron en acercarse tras el grito. Usted llegó en menos de noventa segundos, vestido y peinado."));
                        lines.Add(("Robert", "Es mi motel, Miller. Tengo el sueño liviano y duermo con la ropa de trabajo a mano por si hay emergencias. Velar por mis huéspedes es mi deber."));
                        onFinish = () => CaseState.Instance.SetFlag("robert_timing_doubt");
                        return true;
                    }
                    if (clueId == "carla_belongings")
                    {
                        lines.Add(("Gabe", "Encontré la cartera y documentos de Carla escondidos detrás de unas cajas cerca del sótano. No estaban en su habitación."));
                        lines.Add(("Robert", "Alguien debió moverlos... algún empleado deshonesto o el mismo atacante. Voy a revisar esto con el sheriff."));
                        onFinish = () => CaseState.Instance.SetFlag("robert_belongings_exposed");
                        return true;
                    }
                    if (clueId == "basement_noises_match")
                    {
                        lines.Add(("Gabe", "Hubo ruidos en el sótano a deshoras de la noche."));
                        lines.Add(("Robert", "Hago el mantenimiento pesado de madrugada para no molestar a los clientes con las bombas de agua. Si busca sospechosos, pregúntele al vagabundo que merodea por la estación."));
                        onFinish = () => CaseState.Instance.SetFlag("robert_excuse_locked");
                        return true;
                    }
                    lines.Add(("Robert", "Un objeto curioso, detective. Pero no veo qué relación guarda con la administración del Starlight."));
                    return false;

                case "elena":
                    if (clueId == "elena_master_keys")
                    {
                        lines.Add(("Gabe", "Solo hay dos llaves maestras en todo el complejo: la de Robert y la suya. Nadie puede entrar a los cuartos sin ellas."));
                        lines.Add(("Elena", "Yo no abrí la puerta de Carla esa noche... se lo juro. Pero vi a Robert salir de la oficina con su manojo de llaves cerca de la medianoche."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("elena_implicates_robert");
                            CaseState.Instance.CollectClue("robert_quick_arrival");
                        };
                        return true;
                    }
                    if (clueId == "elena_seen_running")
                    {
                        lines.Add(("Gabe", "Un testigo la vio salir disparada por el pasillo trasero justo después del grito."));
                        lines.Add(("Elena", "¡Tenía terror! Escuché a Robert discutir con ella... escuché un golpe seco y salí corriendo a encerrarme en mi cuarto. No quería ser la siguiente."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("elena_confession_full");
                            CaseState.Instance.CollectClue("elena_alibi_gap");
                        };
                        return true;
                    }
                    if (clueId == "carla_belongings")
                    {
                        lines.Add(("Elena", "Ese es el bolso que Carla dejó en recepción... Dios mío. Robert me prohibió terminantemente entrar al sótano o tocar nada."));
                        onFinish = () => CaseState.Instance.SetFlag("elena_confirmed_coverup");
                        return true;
                    }
                    lines.Add(("Elena", "No sé qué es eso, señor Miller... por favor, no me meta en más problemas de los que ya tengo."));
                    return false;

                case "mark":
                    if (clueId == "bottle_was_marks" || clueId == "glass_matches_bottle")
                    {
                        lines.Add(("Gabe", "Los vidrios rotos junto al cuerpo coinciden con la ginebra que compraste anoche en la gasolinera."));
                        lines.Add(("Mark", "¡No... no, yo no le hice nada! Tropecé en la banquina... la botella se me escapó de las manos y se reventó contra el cordón. Carla me gritó asustada y corrió hacia el fondo del motel."));
                        lines.Add(("Gabe", "¿Viste a alguien más cerca de ella?"));
                        lines.Add(("Mark", "Vi una silueta alta... alguien que salió desde las sombras del pasillo. Me asusté y me escondí detrás del letrero."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("mark_cleared_murder");
                            CaseState.Instance.CollectClue("mark_no_memory");
                        };
                        return true;
                    }
                    if (clueId == "frank_saw_mark_and_carla")
                    {
                        lines.Add(("Gabe", "Frank los vio cruzarse cerca de la ruta antes de la medianoche."));
                        lines.Add(("Mark", "Le pedí unas monedas... ella me dio un billete arrugado y me dijo que tenía que irse del pueblo antes del amanecer. ¡Eso fue todo!"));
                        onFinish = () => CaseState.Instance.SetFlag("mark_carla_warning_revealed");
                        return true;
                    }
                    lines.Add(("Mark", "Luces y sombras... eso no me dice nada a mí. Los ruidos vienen de la noche..."));
                    return false;

                case "frank":
                    if (clueId == "frank_saw_mark_and_carla")
                    {
                        lines.Add(("Gabe", "Frank, cuando vio a Mark con Carla, ¿había alguien más mirando desde las sombras?"));
                        lines.Add(("Frank", "Bueno... ahora que hace memoria... la camioneta de Ernesto pasó despacio por la banquina con las luces apagadas dos minutos después."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("frank_saw_ernesto_truck");
                            CaseState.Instance.CollectClue("ernesto_false_alibi");
                        };
                        return true;
                    }
                    if (clueId == "glass_matches_bottle")
                    {
                        lines.Add(("Gabe", "Este cuello de botella roto coincide con la ginebra que vende en su estación."));
                        lines.Add(("Frank", "Exacto. Se la vendí a Mark a las ocho de la noche. Se fue tambaleando hacia la banquina del motel."));
                        onFinish = () => CaseState.Instance.CollectClue("bottle_was_marks");
                        return true;
                    }
                    lines.Add(("Frank", "Ni idea de qué es eso, amigo. Yo de nafta entiendo todo; de chismes de pueblo, lo justo y necesario."));
                    return false;

                case "marta":
                    if (clueId == "carla_belongings")
                    {
                        lines.Add(("Marta", "Pobre Carla... me contó que estaba reuniendo plata para irse en el autobús a Seattle. Me dijo que Robert no la dejaba en paz y que le tenía miedo."));
                        onFinish = () => CaseState.Instance.SetFlag("marta_carla_seattle_confirmed");
                        return true;
                    }
                    lines.Add(("Marta", "No sé qué es eso, señor. Acá la gente viene a tomar café y callarse la boca."));
                    return false;

                default:
                    lines.Add((characterId, "No tengo nada que decir sobre ese objeto."));
                    return false;
            }
        }

        private string GetAmbientGreeting(string charId, int day, int phase)
        {
            switch (charId)
            {
                case "elena":
                    if (day == 2)
                    {
                        if (phase == 1) return "¿Qué... qué necesita, señor Miller? No pude pegar un ojo con todo lo que pasó anoche.";
                        if (phase == 2) return "Dígame rápido, por favor... no quiero tener problemas con Robert.";
                        return "La noche se pone tensa acá... ¿qué busca ahora?";
                    }
                    return "Sigue dando vueltas por acá... ¿averiguó algo sobre Carla?";

                case "robert":
                    if (day == 2)
                    {
                        if (phase == 1) return "Buen día, detective. Intento mantener la calma en el motel tras la tragedia. ¿En qué lo puedo ayudar?";
                        if (phase == 2) return "Señor Miller. ¿Encontró algo de utilidad para esclarecer el asunto?";
                        return "Se hace tarde, detective. ¿Necesita algo antes de que cierre la recepción?";
                    }
                    return "Último día por acá, entiendo. Espero que sus conclusiones sean justas y profesionales.";

                case "gus":
                    if (day == 2)
                    {
                        if (phase == 1) return "Lindo lío se armó en el motel anoche, ¿no? Qué nochecita.";
                        if (phase == 2) return "El motor de mi camión ya casi está a punto. ¿Qué cuenta el detective?";
                        return "Por acá de noche no se ve nada. Salvo lo que uno preferiría no ver.";
                    }
                    return "Apenas cargue el acoplado me voy de este pueblo. Me da mala espina.";

                case "ernesto":
                    if (day == 2)
                    {
                        if (phase == 1) return "¿Usted otra vez? Estoy ocupado acomodando pedidos, sea breve.";
                        if (phase == 2) return "Las alfombras no se van a vender solas. ¿Qué quiere ahora?";
                        return "Ya es hora de cerrar. Si no va a comprar nada, no moleste.";
                    }
                    return "¿Todavía dando vueltas? Ya le dije a la policía todo lo que tenía para decir.";

                case "marta":
                    if (day == 2)
                    {
                        if (phase == 1) return "Día frío para estar investigando crímenes, señor. ¿Le sirvo un café o busca respuestas?";
                        if (phase == 2) return "Carla solía venir a esta hora a sentarse junto al ventanal. Qué pena de chica...";
                        return "Cierro en un rato. Tenga cuidado si anda dando vueltas afuera de noche.";
                    }
                    return "Espero que encuentre al culpable de lo de Carla. Este pueblo necesita cerrar esa herida.";

                case "frank":
                    if (day == 2)
                    {
                        if (phase <= 2) return "¡Eh, el del auto roto! Terrible lo del motel. Menos mal que Robert tiene la cabeza fría.";
                        return "La noche trae de todo por la estación de servicio. ¿Qué se le ofrece?";
                    }
                    return "No me gusta el clima que hay en el pueblo hoy. Ojalá todo se aclare pronto.";

                case "mark":
                    if (day == 2)
                    {
                        return "Ruidos... pasos en la noche... no me miren a mí, yo no hice nada...";
                    }
                    return "El sótano... nadie me cree, pero ellos saben lo que hay ahí abajo...";

                default:
                    return "¿Sí? ¿En qué puedo ayudarlo?";
            }
        }

        private IEnumerator InvestigateRoutine(string spotId)
        {
            isBusy = true;

            var spot = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetInvestigateSpot(spotId) : null;
            if (spot == null)
            {
                Debug.LogWarning($"[ConversationController] No hay datos de punto de investigación para '{spotId}'.");
                isBusy = false;
                yield break;
            }

            var variant = PickVariant(spot.variants);
            if (variant != null)
            {
                foreach (var line in variant.lines)
                {
                    yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
                }

                // Solo gasta acción si es la primera vez que se inspecciona
                string spotKey = $"spot_seen_{spot.id}";
                if (!CaseState.Instance.HasFlag(spotKey))
                {
                    CaseState.Instance.SetFlag(spotKey);
                    if (PhaseController.Instance != null)
                    {
                        PhaseController.Instance.SpendAction();
                    }
                }

                ApplyEffects(variant.effects);
            }

            UI.HideDialogue();
            isBusy = false;
        }

        private List<TopicData> GetVisibleTopics(CharacterData character)
        {
            if (character == null || character.topics == null) return new List<TopicData>();
            var cs = CaseState.Instance;
            return character.topics
                .Where(t => t.kind != TopicKind.Confrontar && (cs == null || cs.EvaluateAll(t.unlockConditions)))
                .ToList();
        }

        private IEnumerator PlayTopic(CharacterData character, TopicData topic)
        {
            var variant = PickVariant(topic.variants);
            if (variant != null)
            {
                foreach (var line in variant.lines)
                {
                    yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
                }

                // Solo gasta acción si es la primera vez que se lee este tema
                if (!CaseState.Instance.HasSeenTopic(character.id, topic.id))
                {
                    CaseState.Instance.MarkTopicSeen(character.id, topic.id);
                    if (PhaseController.Instance != null)
                    {
                        PhaseController.Instance.SpendAction();
                    }
                }

                ApplyEffects(variant.effects);
            }

            UI.HideDialogue();
        }

        private DialogueVariantData PickVariant(List<DialogueVariantData> variants)
        {
            if (variants == null || variants.Count == 0) return null;
            var cs = CaseState.Instance;

            var matching = variants
                .Where(v => cs == null || cs.EvaluateAll(v.conditions))
                .OrderByDescending(v => v.priority)
                .ToList();

            return matching.Count > 0 ? matching[0] : null;
        }

        private void ApplyEffects(List<EffectData> effects)
        {
            if (effects == null || CaseState.Instance == null) return;

            foreach (var effect in effects)
            {
                switch (effect.kind)
                {
                    case EffectKind.SetFlag:
                        CaseState.Instance.SetFlag(effect.a);
                        break;
                    case EffectKind.ClearFlag:
                        CaseState.Instance.ClearFlag(effect.a);
                        break;
                    case EffectKind.UnlockTopic:
                        CaseState.Instance.UnlockTopic(effect.a, effect.b);
                        break;
                    case EffectKind.CollectClue:
                        CaseState.Instance.CollectClue(effect.a);
                        break;
                    case EffectKind.IncrementCounter:
                        int inc = int.TryParse(effect.b, out var parsedInc) ? parsedInc : 1;
                        CaseState.Instance.IncrementCounter(effect.a, inc);
                        break;
                }
            }
        }
    }
}
