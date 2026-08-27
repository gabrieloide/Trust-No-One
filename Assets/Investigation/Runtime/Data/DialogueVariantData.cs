using System;
using System.Collections.Generic;

namespace Investigation
{
    [Serializable]
    public class DialogueLineData
    {
        public string speaker = "";
        public string text = "";
    }

    [Serializable]
    public class DialogueVariantData
    {
        public string id = "";

        // Variantes con mayor prioridad se evalúan primero (para las líneas alternativas
        // que se activan por una traba específica, que deben ganarle a la línea "limpia").
        public int priority = 0;

        // AND: todas deben cumplirse. Vacío = variante default/fallback.
        public List<ConditionData> conditions = new List<ConditionData>();

        public List<DialogueLineData> lines = new List<DialogueLineData>();

        // Se aplican después de reproducir las líneas.
        public List<EffectData> effects = new List<EffectData>();
    }
}
