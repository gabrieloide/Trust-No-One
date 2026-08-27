using System;
using System.Collections.Generic;

namespace Investigation
{
    // Define la presencia física y horarios de los 7 personajes a lo largo de los días y fases.
    public static class NPCSchedule
    {
        public static bool IsCharacterPresent(string characterId, string locationId, int day, int phase)
        {
            // Día 1 (Prólogo): controlado por los StoryGraphs de intro
            if (day == 1)
            {
                if (locationId == "road" && characterId == "gus") return false; // Gus se presenta en D2
                if (locationId == "motel" && (characterId == "elena" || characterId == "robert")) return true;
                return false;
            }

            switch (characterId)
            {
                case "elena":
                    // Elena trabaja en la recepción del motel en Mañana (1) y Tarde (2).
                    // De Noche (3) su turno finaliza y descansa.
                    if (locationId != "motel") return false;
                    return phase == 1 || phase == 2;

                case "robert":
                    // Robert siempre está en el motel administrando y vigilando.
                    if (locationId != "motel") return false;
                    return true;

                case "gus":
                    // Gus (camionero):
                    // D2P1: en su camión averiado en la Carretera.
                    // D2P2: tomando café en la Cafetería.
                    // D2P3: durmiendo en su camión (no interactivo).
                    // D3P1: terminando de cargar en la Carretera.
                    // D3P2 / D3P3: ya se fue del pueblo.
                    if (day == 2)
                    {
                        if (phase == 1 && locationId == "road") return true;
                        if (phase == 2 && locationId == "cafeteria") return true;
                        return false;
                    }
                    if (day == 3)
                    {
                        if (phase == 1 && locationId == "road") return true;
                        return false;
                    }
                    return false;

                case "ernesto":
                    // Ernesto: su tienda de alfombras abre en Mañana (1) y Tarde (2).
                    // De Noche (3) la tienda está cerrada con persiana metálica.
                    if (locationId != "ernesto_shop") return false;
                    return phase == 1 || phase == 2;

                case "frank":
                    // Frank siempre atiende la gasolinera las 24 horas.
                    if (locationId != "gas_station") return false;
                    return true;

                case "mark":
                    // Mark (vagabundo):
                    // D2P1: deambula por el pueblo (no disponible).
                    // D2P2: merodea por la Carretera / Ruta.
                    // D2P3: refugiado de noche contra la pared de la Gasolinera.
                    // D3P1: deambula.
                    // D3P2 / D3P3: en la Gasolinera.
                    if (day == 2)
                    {
                        if (phase == 2 && locationId == "road") return true;
                        if (phase == 3 && locationId == "gas_station") return true;
                        return false;
                    }
                    if (day == 3)
                    {
                        if ((phase == 2 || phase == 3) && locationId == "gas_station") return true;
                        return false;
                    }
                    return false;

                case "marta":
                    // Marta atiende la cafetería durante el día y la tarde, y cierra de noche.
                    if (locationId != "cafeteria") return false;
                    return phase == 1 || phase == 2 || (day == 2 && phase == 3);

                default:
                    return true;
            }
        }

        public static string GetLocationStatusNotice(string locationId, int day, int phase)
        {
            if (locationId == "ernesto_shop" && phase == 3)
            {
                return "[The carpet shop is closed for the night, roll-down shutter locked.]";
            }
            if (locationId == "motel" && phase == 3)
            {
                return "[Elena's shift at the front desk has ended. Only Robert is around.]";
            }
            if (locationId == "road" && day == 3 && phase >= 2)
            {
                return "[Gus's truck is gone. He left down the highway.]";
            }
            return "";
        }
    }
}
