using NUnit.Framework;
using UnityEngine;
using Investigation;

namespace Investigation.Tests
{
    [TestFixture]
    public class PhaseControllerTests
    {
        private PhaseController phaseController;

        [SetUp]
        public void SetUp()
        {
            CaseState.ResetForNewGame();
            PhaseController.ResetForNewGame();
            phaseController = PhaseController.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            PhaseController.ResetForNewGame();
            CaseState.ResetForNewGame();
        }

        [Test]
        public void InitialState_StartsAtDay1Phase1_WithActions()
        {
            var cs = CaseState.Instance;
            Assert.AreEqual(1, cs.currentDay);
            Assert.AreEqual(1, cs.currentPhase);
            Assert.Greater(cs.actionsRemainingInPhase, 0);
            Assert.IsFalse(phaseController.IsCaseOver);
        }

        [Test]
        public void SpendAction_DecrementsRemainingActions()
        {
            int startActions = CaseState.Instance.actionsRemainingInPhase;
            phaseController.SpendAction();
            Assert.AreEqual(startActions - 1, CaseState.Instance.actionsRemainingInPhase);
        }

        [Test]
        public void Day1_Phase1Exhausted_SkipsDirectlyToDay2()
        {
            // En Día 1, la noche (fase 2) es cinemática fija del crimen, por lo que el controlador
            // salta directo a Día 2 al agotar las acciones de D1P1.
            phaseController.SetPhase(1, 1, 1);
            Assert.AreEqual(1, CaseState.Instance.currentDay);
            Assert.AreEqual(1, CaseState.Instance.currentPhase);

            phaseController.SpendAction();

            Assert.AreEqual(2, CaseState.Instance.currentDay);
            Assert.AreEqual(1, CaseState.Instance.currentPhase);
            Assert.Greater(CaseState.Instance.actionsRemainingInPhase, 0);
        }

        [Test]
        public void Day2_AdvancesThroughAllThreePhases()
        {
            phaseController.SetPhase(2, 1, 1);
            Assert.AreEqual(2, CaseState.Instance.currentDay);
            Assert.AreEqual(1, CaseState.Instance.currentPhase);

            // Agotar fase 1 -> fase 2
            phaseController.SpendAction();
            Assert.AreEqual(2, CaseState.Instance.currentDay);
            Assert.AreEqual(2, CaseState.Instance.currentPhase);

            // Agotar fase 2 -> fase 3
            CaseState.Instance.actionsRemainingInPhase = 1;
            phaseController.SpendAction();
            Assert.AreEqual(2, CaseState.Instance.currentDay);
            Assert.AreEqual(3, CaseState.Instance.currentPhase);

            // Agotar fase 3 -> Día 3 fase 1
            CaseState.Instance.actionsRemainingInPhase = 1;
            phaseController.SpendAction();
            Assert.AreEqual(3, CaseState.Instance.currentDay);
            Assert.AreEqual(1, CaseState.Instance.currentPhase);
        }

        [Test]
        public void Day3_ExhaustingFinalPhase_SetsIsCaseOver()
        {
            phaseController.SetPhase(3, 3, 1);
            Assert.AreEqual(3, CaseState.Instance.currentDay);
            Assert.AreEqual(3, CaseState.Instance.currentPhase);
            Assert.IsFalse(phaseController.IsCaseOver);

            phaseController.SpendAction();

            Assert.IsTrue(phaseController.IsCaseOver);
            Assert.Greater(CaseState.Instance.currentDay, 3);
        }

        [Test]
        public void AdvanceToNextDay_BypassesRemainingPhases()
        {
            phaseController.SetPhase(2, 1, 4);
            phaseController.AdvanceToNextDay();

            Assert.AreEqual(3, CaseState.Instance.currentDay);
            Assert.AreEqual(1, CaseState.Instance.currentPhase);

            phaseController.AdvanceToNextDay();
            Assert.IsTrue(phaseController.IsCaseOver);
        }
    }
}
