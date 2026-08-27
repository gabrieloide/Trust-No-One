# 🌉 Agent Bridge: Trust No One

## 📋 Estado del Trabajo
- **Orquestador:** Gemini CLI
- **Especialista Técnico:** Claude CLI
- **Tarea Actual:** Limpieza y dinamismo del HUD y Hotspots en Unity
- **Estado:** EN_PROGRESO (Esperando propuesta de Claude)

---

## 🎯 Contexto y Requerimiento Técnico
En el juego de investigación point & click 2D "Trust No One", la UI del mundo (`WorldBuilder.cs`, `LocationController.cs`, `EvidencePanelController.cs`) actualmente muestra todos los elementos al mismo tiempo desde el inicio, saturando la pantalla.

Necesitamos diseñar e implementar las siguientes mejoras en los scripts de Unity:

1. **Visibilidad dinámica de Hotspots de Investigación (`InvestigateSpot`):**
   - En locaciones como `crime_scene` (Zona del Sótano), hay varios hotspots (`inv_basement_lock`, `inv_scene_glass`, `inv_crime_scene_fiber`, `inv_basement_revisit`, `inv_near_basement_carla`).
   - Cada spot tiene sus `unlockConditions` en `investigate.json`.
   - Deben ocultarse si sus `unlockConditions` no se cumplen en la fase/día actual.

2. **Desbloqueo Progresivo de Locaciones en la NavBar:**
   - **Día 1:** Solo accesibles `motel` y `road`. (El crimen aún no ocurre, la zona del sótano y la cafetería no deben mostrarse).
   - **Día 2 y 3:** Se desbloquean `gas_station`, `cafeteria`, `ernesto_shop`, `crime_scene`.

3. **Botón "ACUSAR" (Accusation):**
   - Debe permanecer oculto durante Día 1 y Día 2.
   - Solo debe mostrarse en Día 3 (cuando `CaseState.Instance.currentDay >= 3` o `PhaseController.Instance.IsCaseOver`).

4. **Panel de Evidencias / Pistas (`EvidencePanel`):**
   - Solo debe mostrarse si el jugador tiene al menos 1 pista recolectada (`CaseState.Instance.CollectedClues.Count > 0`).

5. **Ocultamiento de HUD durante cinemáticas/intros:**
   - Cuando `LocationController.HideAll()` se ejecuta (cutscenes de cambio de día en `GameFlowController`), toda la UI del mundo (`NavBar`, `HUD`, `EvidencePanel`, `AccuseButton`) debe ocultarse limpiamente.

---

## 💬 Respuesta y Propuesta Técnica de Claude
*(Claude escribirá su análisis y código aquí)*
