using NUnit.Framework;
using UnityEngine;
using Investigation;

namespace Investigation.Tests
{
    [TestFixture]
    public class DialogueDatabaseTests
    {
        private DialogueDatabase database;

        [SetUp]
        public void SetUp()
        {
            database = DialogueDatabase.Instance;
        }

        [Test]
        public void ResolveSpeakerDisplayName_ExpandsKnownCharacters_PreservesUnknowns()
        {
            // Personajes registrados deben resolver su nombre completo
            string robertName = database.ResolveSpeakerDisplayName("robert");
            Assert.AreEqual("Robert Hale", robertName);

            string elenaName = database.ResolveSpeakerDisplayName("elena");
            Assert.AreEqual("Elena Marchetti", elenaName);

            // Hablantes no registrados (protagonista, sheriff, etc.) deben pasar sin mutación
            Assert.AreEqual("Gabe", database.ResolveSpeakerDisplayName("Gabe"));
            Assert.AreEqual("Sheriff", database.ResolveSpeakerDisplayName("Sheriff"));
            Assert.AreEqual("", database.ResolveSpeakerDisplayName(""));
        }

        [Test]
        public void GetConfrontation_ResolvesDirectAndAlternativeClues()
        {
            // Ernesto confrontado con carpet_fiber
            var ernestoConf = database.GetConfrontation("ernesto", "carpet_fiber");
            Assert.IsNotNull(ernestoConf);
            Assert.IsNotEmpty(ernestoConf.lines);

            // Mark confrontado con pista alternativa glass_matches_bottle
            var markConf = database.GetConfrontation("mark", "glass_matches_bottle");
            Assert.IsNotNull(markConf);
            Assert.AreEqual("bottle_was_marks", markConf.clueId);

            // Pista irrelevante debe devolver null
            var irrelevantConf = database.GetConfrontation("ernesto", "basement_lock");
            Assert.IsNull(irrelevantConf);
        }

        [Test]
        public void GetAmbientGreeting_ResolvesCorrectPriorityAndFallbacks()
        {
            // Robert Día 2 Fase 1 (coincidencia exacta)
            string d2p1 = database.GetAmbientGreeting("robert", 2, 1);
            Assert.IsTrue(d2p1.Contains("Good morning") || d2p1.Contains("calm"));

            // Robert Día 2 Fase 3
            string d2p3 = database.GetAmbientGreeting("robert", 2, 3);
            Assert.IsTrue(d2p3.Contains("late"));

            // Robert Día 3 (coincidencia de día con phase 0)
            string d3 = database.GetAmbientGreeting("robert", 3, 2);
            Assert.IsTrue(d3.Contains("Last day") || d3.Contains("conclusions"));

            // Personaje inexistente devuelve fallback seguro
            string unknown = database.GetAmbientGreeting("ghost_npc", 2, 1);
            Assert.IsFalse(string.IsNullOrEmpty(unknown));
        }

        [Test]
        public void GetDefaultRejection_ReturnsCustomOrGenericFallback()
        {
            string ernestoRejection = database.GetDefaultRejection("ernesto");
            Assert.IsTrue(ernestoRejection.Contains("buy") || ernestoRejection.Contains("Buy") || ernestoRejection.Contains("time"));

            string genericRejection = database.GetDefaultRejection("non_existent");
            Assert.IsTrue(genericRejection.Contains("nothing to say"));
        }
    }
}
