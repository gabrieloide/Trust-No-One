# Ruta de QA — Los 8 finales

Este documento cubre dos formas de llegar a los finales: la **ruta rápida** (recomendada
para revisar drama/arte antes de comprometer recursos) y la **ruta legítima** (jugando
normal, útil para validar que el gating de contenido día/fase no está roto).

La secuencia que se dispara en ambos casos es la misma: `AccusationController.BeginAccusation()`
→ `ResolveOutcome()`. No hay una versión "de prueba" distinta a la de producción.

---

## 1. Ruta rápida (debug)

1. En una escena de prueba (o en `Investigation.unity`), añade un GameObject vacío con el
   componente `DebugPhaseSetter` (`Assets/Investigation/Runtime/Debug/DebugPhaseSetter.cs`).
2. Marca en el inspector:
   - **Force Case Over** ✔ — pone `currentDay = 4` para que `PhaseController.IsCaseOver` sea `true`.
   - **Auto Collect All Clues** ✔ — recolecta las 15 pistas de `clues.json`, así el menú de
     evidencia siempre te ofrece todas las opciones.
   - **Auto Open Accusation** ✔ — abre la pantalla de acusación al entrar en Play.
3. Dale a Play. Caerás directo en "Who do I accuse?".
4. Al terminar un final, la pantalla vuelve a estado normal. Usa el interactable `OpenAccusation`
   que ya existe en el mundo (el que dispara `StoryInteractable.OnOpenAccusationRequested`) para
   volver a abrir la acusación **sin reiniciar Play Mode** y probar la siguiente combinación de
   la tabla de abajo.

No hace falta reiniciar Play entre cada final — todas las pistas ya están recolectadas desde
el paso 2, así que solo cambias sospechoso + evidencia en el mismo diálogo de elección.

---

## 2. Tabla de combinaciones → final

El resultado depende solo de dos cosas: a quién acusas, y si la pista que presentas tiene
`hiddenWeight: Strong` **y** apunta a ese sospechoso (`pointsTo` lo incluye). Todo lo demás
cae en la rama "débil".

| # | Sospechoso | Evidencia a presentar | `hiddenWeight` | Resultado |
|---|-----------|------------------------|-----------------|-----------|
| 1 | **Robert** | `basement_lock` o `basement_exclusive_access` | Strong | ✅ **Final verdadero.** Confesión muda + beat dramático nuevo. |
| 2 | **Robert** | Cualquier otra pista (o ninguna) | — | Final "tengo razón pero no puedo probarlo". |
| 3 | **Mark** | `bottle_was_marks` o `glass_matches_bottle` | Strong | Falso positivo "convincente" — caso cerrado mal. |
| 4 | **Mark** | Cualquier otra pista (o ninguna) | — | Acusación floja, Mark se defiende con razón. |
| 5 | **Elena** | `elena_master_keys` | Strong | Falso positivo "convincente" — caso cerrado mal. |
| 6 | **Elena** | Cualquier otra pista (o ninguna) | — | Acusación floja. |
| 7 | **Ernesto** | `carpet_fiber` o `ernesto_false_alibi` | Strong | Falso positivo "convincente" — caso cerrado mal. |
| 8 | **Ernesto** | Cualquier otra pista (o ninguna) | — | Acusación floja. |

`carla_belongings` apunta tanto a `robert` como a `elena` (weight Strong) — sirve como
evidencia "fuerte" para el final #1 o el #5, según a quién acuses con ella.

---

## 3. Ruta legítima (jugando normal, sin debug)

Para validar que el contenido se desbloquea en el orden correcto. Fases: Día 1 (2 fases),
Día 2 y Día 3 (3 fases c/u), 6 acciones por fase.

### Día 2, Fase 1 (`d2p1_started`)
- Habla con **Mark** → "Ask him what he saw or heard" → da `basement_noises_match`.
- Habla con **Robert** → "Ask him about what happened" → da `robert_quick_arrival`.
- Habla con **Gus** → "Ask him what he saw" → marca `arbustos_lead_mentioned` (mejora la
  variante del arbusto más adelante, no da pista por sí sola).
- (Opcional) Investiga los arbustos (`inv_arbustos`) — sin la pista de Gus da solo flavor.

### Día 2, Fase 2 (`d2p2_started`)
- Habla con **Frank** → "Ask if he saw anything odd" → da `frank_saw_mark_and_carla`.
- Confronta a **Elena** (calm o harsh, cualquiera) → da `elena_alibi_gap` + `elena_seen_running`.
  ⚠️ Elige **calm** ("Calmly ask her why she was running") si luego quieres la variante rica
  de `carla_belongings` en Día 3 (deja `elena_trust_intact`); **harsh** la rompe.
- Habla con **Mark** → "Tell him you believe him" (NO "Cut him off"). ⚠️ Si lo cortas,
  pierdes la pista `mark_no_memory` en Día 3 (la variante "broken" no la da).
- Investiga el mostrador de la tienda de alfombras (`inv_carpet_shop_receipt`) → da
  `carpet_shop_receipt`.

### Día 2, Fase 3 (`d2p3_started`)
- Habla con **Elena** → "Ask her about the master keys" → da `elena_master_keys` (**Strong**).
- Investiga la escena del crimen (`inv_scene_glass`) → da `glass_matches_bottle` (**Strong**);
  si ya hablaste con Frank en la fase anterior, también da `bottle_was_marks` (**Strong**).
- Investiga la puerta del sótano (`inv_basement_lock`) → da `basement_lock` (**Strong**).

### Día 3, Fase 1 (`d3p1_started`)
- Investiga la escena otra vez (`inv_crime_scene_fiber`) **antes** de confrontar a Ernesto →
  da `carpet_fiber` (**Strong**).
- Confronta a **Ernesto** ("Confront him about that night") → con `carpet_fiber` ya en mano,
  da `ernesto_false_alibi` (**Strong**) + `ernesto_hand_cut`.
- Habla con **Elena** → "Ask her about the basement" → da `basement_exclusive_access` (**Strong**).

### Día 3, Fase 2 (`d3p2_started`)
- Investiga la esquina junto al sótano (`inv_near_basement_carla`) → da `carla_belongings`
  (**Strong**, apunta a `robert` y `elena`).
- Habla con **Mark** otra vez ("Ask him about that night again") → da `mark_no_memory`
  (solo si no lo cortaste en Día 2 Fase 2).

### Día 3, Fase 3 (`d3p3_started`)
- Confronta a **Robert** ("Confront him with the motel evidence") → flavor final, sin pista
  nueva, pero cierra su arco de diálogo antes de acusar.

### Día 4
- El caso se cierra solo (`GameFlowController.EndInvestigationOverlay`) y se abre la
  acusación automáticamente. Usa la tabla de la sección 2 para elegir sospechoso + evidencia.
- **Esta ruta no lleva a un solo final.** Si seguiste todos los pasos de arriba, para el
  Día 4 ya tienes en el inventario las pistas Strong de los 4 sospechosos a la vez
  (`basement_lock`/`basement_exclusive_access` de Robert, `bottle_was_marks`/`glass_matches_bottle`
  de Mark, `elena_master_keys` de Elena, `carpet_fiber`/`ernesto_false_alibi` de Ernesto).
  La pantalla de acusación es repetible: `AccusationController.BeginAccusation()` no marca el
  caso como "ya resuelto", así que tras ver un desenlace puedes volver a activar el mismo
  interactable `OpenAccusation` del mundo y elegir otra fila de la tabla de la sección 2.
  Recorriendo las 8 combinaciones ahí mismo ves los 8 finales, sin recargar la partida.
