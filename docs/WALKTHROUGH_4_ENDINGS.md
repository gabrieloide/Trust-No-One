# Cómo llegar a cada final (los 4 finales reales)

Este documento es una guía de **playthrough legítimo** (sin debug, jugando normal) para
llegar a cada uno de los 4 desenlaces que existen hoy en `AccusationController.ResolveOutcome`.

Técnicamente hay 8 combinaciones posibles de sospechoso + evidencia fuerte (una por cada
sospechoso que no es Robert, más Robert), pero el juego solo tiene **4 textos de final
distintos**, porque las 3 variantes de "acusé al sospechoso equivocado" comparten el mismo
texto (solo cambia el nombre):

| Rótulo en pantalla | Condición | Quiénes pueden dar este resultado |
|---|---|---|
| **THE TRUTH COMES OUT** | Acusás a **Robert** + evidencia *Strong* que apunta a Robert | Solo Robert |
| **NOT ENOUGH** | Acusás a **Robert** + evidencia floja o ninguna | Solo Robert |
| **WRONG MAN** | Acusás a **otro sospechoso** + evidencia *Strong* que apunta a esa persona | Mark, Elena o Ernesto |
| **UNRESOLVED** | Acusás a **otro sospechoso** + evidencia floja o ninguna | Mark, Elena o Ernesto |

No importa qué hiciste el resto de la partida: lo único que decide el final son las dos
elecciones de la pantalla de acusación (a quién señalás + qué pista mostrás). Lo que sigue
es la forma más natural de llegar con la pista correcta ya en el bolsillo para cada caso.

Estructura del juego: Día 1 es prólogo fijo (sin acciones libres). Día 2 tiene 3 fases, Día 3
tiene 3 fases. El botón **ACUSAR** del menú de ubicación recién aparece cuando la
investigación termina de verdad (Día 4, `PhaseController.IsCaseOver`) — no antes.

---

## Final 1 — "THE TRUTH COMES OUT" (el final verdadero)

Acusás al culpable real, Robert, con una prueba que realmente lo compromete.

**Día 2, Fase 3:**
- Investigá la puerta del sótano (**"Basement door"**) → obtenés **`Basement padlock`**
  (evidencia fuerte, apunta a Robert). Con esto ya te alcanza.

*(Opcional, para llegar con más contexto y más de una opción de evidencia fuerte):*
- Día 2, Fase 1 — hablá con **Robert** (**"Ask him about what happened"**).
- Día 3, Fase 1 — hablá con **Elena** (**"Ask her about the basement"**) → suma
  **`Exclusive basement access`** (también fuerte, también apunta a Robert).
- Día 3, Fase 2 — investigá la esquina junto al sótano (**"Corner by the basement"**) → suma
  **`Carla's travel bag and escape cash`** (fuerte, apunta a Robert *y* Elena — sirve para este
  final o para el final 3 si en cambio acusás a Elena con ella).

**Pantalla de acusación:**
- Sospechoso: **Robert Hale**.
- Evidencia: **Basement padlock** (o `Exclusive basement access` / `Carla's travel bag and
  escape cash`).

---

## Final 2 — "NOT ENOUGH" (tenías razón, pero no alcanza)

Acusás a Robert (el culpable real) pero sin nada que realmente lo comprometa.

**Día 2, Fase 1:**
- Hablá con **Mark** (**"Ask him what he saw or heard"**) → da `Matching basement noise
  reports` (floja).
- Hablá con **Robert** (**"Ask him about what happened"**) → da `Robert's suspiciously quick
  arrival` (floja).

Con eso alcanza. **No** investigues la puerta del sótano ni hables con Elena sobre el sótano
en Día 3 — esas son las que dan evidencia fuerte y te llevarían al final 1 en cambio.

**Pantalla de acusación:**
- Sospechoso: **Robert Hale**.
- Evidencia: **Matching basement noise reports** (o `Robert's suspiciously quick arrival`, o
  directamente el botón **"Accuse without evidence"**).

---

## Final 3 — "WRONG MAN" (caso cerrado mal, con "pruebas" convincentes)

Acusás a alguien que no es Robert, pero con evidencia que parece sólida. Ejemplo con
**Ernesto** (funciona igual con Mark o Elena, ver alternativas al final):

**Día 2, Fase 2:**
- Investigá el mostrador de la alfombrería (**"Carpet shop counter"**) → da `Carpet shop
  receipt and debt` (floja). Con solo tener esta pista en el inventario alcanza para
  desbloquear la variante rica del siguiente paso.

**Día 3, Fase 1:**
- Investigá la escena del crimen de nuevo (**"Scene re-examination (Day 3)"**) → como ya
  tenés `Carpet shop receipt and debt`, da directamente **`Carpet fiber`** (fuerte, apunta a
  Ernesto).
- Confrontá a **Ernesto** (**"Confront him about that night"**) → con `Carpet fiber` ya en
  mano, suma **`Ernesto's false alibi`** (también fuerte).

**Pantalla de acusación:**
- Sospechoso: **Ernesto Vidal**.
- Evidencia: **Carpet fiber** (o `Ernesto's false alibi`).

**Alternativas equivalentes (mismo final, mismo texto, solo cambia el nombre):**
- **Mark:** Día 2 Fase 2 hablá con Frank (**"Ask if he saw anything odd last night"**) → da
  `frank_saw_mark_and_carla`; Día 2 Fase 3 investigá la escena del crimen
  (**"Crime scene perimeter"**) → con Frank ya hablado, da **`The bottle was Mark's`** y
  **`The glass matches the bottle`** (ambas fuertes). Acusá a Mark con cualquiera de las dos.
- **Elena:** Día 2, Fase 3 — hablá con Elena (**"Ask her about the master keys"**) → da
  **`Elena's master key access`** (fuerte, sola alcanza). Acusá a Elena con esa pista (o con
  `Carla's travel bag and escape cash` si la conseguiste en Día 3 Fase 2).

---

## Final 4 — "UNRESOLVED" (acusación floja a la persona equivocada)

El más fácil de los cuatro: acusás a cualquiera que no sea Robert, sin nada fuerte que lo
comprometa.

**Camino mínimo:** no hace falta juntar ninguna pista en particular. Alcanza con llegar al
Día 3 sin haber conseguido una pista *Strong* del sospechoso que vas a acusar.

*(Para no ir con las manos totalmente vacías, un paso opcional):*
- Día 2, Fase 2 — hablá con **Frank** (**"Ask if he saw anything odd last night"**) → da
  `Frank saw Mark cross paths with Carla` (floja, apunta a Mark).

**Pantalla de acusación:**
- Sospechoso: **Mark Doss** (o Elena/Ernesto).
- Evidencia: **Frank saw Mark cross paths with Carla** (o cualquier otra pista floja, o
  directamente **"Accuse without evidence"**).

---

## Nota sobre repetibilidad

`AccusationController.BeginAccusation()` no marca el caso como resuelto — el interactable de
acusación en el mundo sigue disponible después de ver un final. Si en una sola partida vas
juntando varias pistas fuertes (Robert + uno de los otros tres), podés volver a abrir la
pantalla de acusación y probar más de un final sin tener que rejugar desde cero. Con
`EndingCreditsUI` ahora hay una pantalla de créditos con "Play Again" al final del todo, pero
mientras no cliqueás ese botón el estado de la partida (Día, pistas) no se resetea.
