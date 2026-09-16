using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Investigation;

namespace Investigation.Tests
{
    [TestFixture]
    public class ContentValidationTests
    {
        private DialogueDatabase database;

        [SetUp]
        public void SetUp()
        {
            database = DialogueDatabase.Instance;
        }

        [Test]
        public void CluesDatabase_HasClues_AndNoDuplicateIds()
        {
            var clues = database.AllClues.ToList();
            Assert.IsNotEmpty(clues, "La base de datos de pistas no debe estar vacía.");

            var seenIds = new HashSet<string>();
            foreach (var clue in clues)
            {
                Assert.IsFalse(string.IsNullOrEmpty(clue.id), "Ninguna pista debe tener ID vacío.");
                Assert.IsFalse(string.IsNullOrEmpty(clue.displayName), $"La pista '{clue.id}' debe tener displayName.");
                Assert.IsTrue(seenIds.Add(clue.id), $"ID de pista duplicado encontrado: '{clue.id}'");
            }
        }

        [Test]
        public void Characters_AreLoaded_WithValidTopicsAndNoDuplicateIds()
        {
            var characters = database.AllCharacters.ToList();
            Assert.IsNotEmpty(characters, "Debe haber personajes cargados en la base de datos.");

            var seenCharIds = new HashSet<string>();
            foreach (var c in characters)
            {
                Assert.IsFalse(string.IsNullOrEmpty(c.id), "ID de personaje no debe ser nulo o vacío.");
                Assert.IsFalse(string.IsNullOrEmpty(c.displayName), $"Personaje '{c.id}' debe tener displayName.");
                Assert.IsTrue(seenCharIds.Add(c.id), $"ID de personaje duplicado: '{c.id}'");

                var seenTopicIds = new HashSet<string>();
                if (c.topics != null)
                {
                    foreach (var t in c.topics)
                    {
                        Assert.IsFalse(string.IsNullOrEmpty(t.id), $"El personaje '{c.id}' tiene un tema con ID vacío.");
                        Assert.IsTrue(seenTopicIds.Add(t.id), $"El personaje '{c.id}' tiene tema duplicado: '{t.id}'");
                    }
                }
            }
        }

        [Test]
        public void Confrontations_ReferenceExistingClues()
        {
            var knownClues = new HashSet<string>(database.AllClues.Select(c => c.id));
            Assert.IsNotEmpty(knownClues, "Deben existir pistas registradas para validar confrontaciones.");

            foreach (var character in database.AllCharacters)
            {
                if (character.confrontations == null) continue;

                foreach (var conf in character.confrontations)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(conf.clueId), $"Personaje '{character.id}' tiene confrontación con clueId vacío.");
                    Assert.IsTrue(knownClues.Contains(conf.clueId),
                        $"Personaje '{character.id}' referencia clueId inexistente en pistas: '{conf.clueId}'");

                    if (conf.alternativeClueIds != null)
                    {
                        foreach (var altId in conf.alternativeClueIds)
                        {
                            Assert.IsTrue(knownClues.Contains(altId),
                                $"Personaje '{character.id}' tiene alternativeClueId inexistente: '{altId}'");
                        }
                    }

                    Assert.IsNotEmpty(conf.lines, $"La confrontación para '{conf.clueId}' en '{character.id}' debe tener líneas de diálogo.");
                }
            }
        }

        [Test]
        public void InvestigateSpots_AreLoaded_AndHaveVariants()
        {
            var spots = database.AllInvestigateSpots.ToList();
            Assert.IsNotEmpty(spots, "Debe haber spots de investigación cargados.");

            var seenSpotIds = new HashSet<string>();
            foreach (var spot in spots)
            {
                Assert.IsFalse(string.IsNullOrEmpty(spot.id), "ID de investigate spot no debe ser vacío.");
                Assert.IsTrue(seenSpotIds.Add(spot.id), $"ID de spot duplicado: '{spot.id}'");
                Assert.IsNotEmpty(spot.variants, $"El spot '{spot.id}' debe tener al menos una variante.");
            }
        }

        [Test]
        public void CounterEffects_HaveValidIntegerPayloads()
        {
            foreach (var character in database.AllCharacters)
            {
                if (character.topics == null) continue;
                foreach (var topic in character.topics)
                {
                    if (topic.variants == null) continue;
                    foreach (var variant in topic.variants)
                    {
                        ValidateEffects(variant.effects, $"Personaje '{character.id}', Topic '{topic.id}', Variant '{variant.id}'");
                    }
                }

                if (character.confrontations == null) continue;
                foreach (var conf in character.confrontations)
                {
                    ValidateEffects(conf.effects, $"Personaje '{character.id}', Confrontation '{conf.clueId}'");
                }
            }
        }

        private void ValidateEffects(List<EffectData> effects, string context)
        {
            if (effects == null) return;
            foreach (var eff in effects)
            {
                if (eff.kind == EffectKind.IncrementCounter)
                {
                    if (!string.IsNullOrEmpty(eff.b))
                    {
                        Assert.IsTrue(int.TryParse(eff.b, out _),
                            $"{context}: IncrementCounter debe tener un payload 'b' numérico válido o vacío, valor actual: '{eff.b}'");
                    }
                }
                else if (eff.kind == EffectKind.CollectClue)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(eff.a), $"{context}: CollectClue debe tener un clueId en 'a'.");
                }
            }
        }
    }
}
