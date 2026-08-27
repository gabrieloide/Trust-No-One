using System;
using System.Collections.Generic;

namespace Investigation
{
    // Nunca se muestra al jugador directamente — solo se infiere jugando.
    public enum ClueWeight
    {
        Weak,
        Strong,
    }

    [Serializable]
    public class ClueData
    {
        public string id = "";
        public string displayName = "";
        public string description = "";
        public ClueWeight hiddenWeight = ClueWeight.Weak;

        // Ids de sospechosos a los que esta pista podría apuntar en la acusación final.
        // Puede tener más de uno cuando la pista es intencionalmente ambigua.
        public List<string> pointsTo = new List<string>();
    }

    [Serializable]
    public class ClueDatabaseFile
    {
        public List<ClueData> clues = new List<ClueData>();
    }
}
