using NUnit.Framework;
using VisualNovelSystem;

namespace Investigation.Tests
{
    [TestFixture]
    public class StoryTextEffectsTests
    {
        [Test]
        public void Parse_NullOrEmpty_ReturnsEmptyResult()
        {
            var r1 = StoryTextEffects.Parse(null, true);
            Assert.IsNotNull(r1);
            Assert.AreEqual("", r1.cleanText);
            Assert.AreEqual(0, r1.charTimings.Count);

            var r2 = StoryTextEffects.Parse("", true);
            Assert.IsNotNull(r2);
            Assert.AreEqual("", r2.cleanText);
            Assert.AreEqual(0, r2.charTimings.Count);
        }

        [Test]
        public void Parse_Invariant_TimingsCountMatchesCleanTextLength()
        {
            string raw = "Hello, detective... Did you find [shake]the body[/shake] at the motel?";
            var parsed = StoryTextEffects.Parse(raw, true);

            Assert.AreEqual(parsed.cleanText.Length, parsed.charTimings.Count,
                "Invariante fundamental: La cantidad de timings debe ser exactamente igual a la longitud del texto limpio.");
        }

        [Test]
        public void Parse_StripsTagsFromCleanText()
        {
            string raw = "Look at [shake]this[/shake] right [pause:0.4]now!";
            var parsed = StoryTextEffects.Parse(raw, false);

            Assert.IsFalse(parsed.cleanText.Contains("[shake]"));
            Assert.IsFalse(parsed.cleanText.Contains("[/shake]"));
            Assert.IsFalse(parsed.cleanText.Contains("[pause:0.4]"));
            Assert.AreEqual("Look at this right now!", parsed.cleanText);
        }

        [Test]
        public void Parse_ExtractsStartingEmotion()
        {
            string raw = "[tremble]I'm so afraid of what's down there...";
            var parsed = StoryTextEffects.Parse(raw, true);

            Assert.AreEqual(PortraitEmotion.Tremble, parsed.startingPortraitEmotion);
            Assert.IsFalse(parsed.cleanText.Contains("[tremble]"));
        }

        [Test]
        public void Parse_AppliesVertexEffectsFlag()
        {
            string rawWithout = "Normal dialogue line without effects.";
            var p1 = StoryTextEffects.Parse(rawWithout, false);
            Assert.IsFalse(p1.hasVertexEffects);

            string rawWith = "A spooky [wave]ghostly voice[/wave] echoing.";
            var p2 = StoryTextEffects.Parse(rawWith, false);
            Assert.IsTrue(p2.hasVertexEffects);
        }
    }
}
