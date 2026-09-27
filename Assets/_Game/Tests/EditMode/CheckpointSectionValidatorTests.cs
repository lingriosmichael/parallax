using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-090 (D-091): LevelLayoutValidator.ValidateSections, the layout half of the section rules. Each fixture is Trap Lab
    // room 11 with its sections changed in one way; the room as declared passes, and a room with none is untouched.
    public sealed class CheckpointSectionValidatorTests
    {
        static readonly Type Section = Type.GetType("Parallax.Editor.Setup.CheckpointSection, Parallax.Editor");
        static readonly string[] Act1 = { "Collapse_1", "Spear_1" }, Act2 = { "StormCloud", "Geyser" }, Act3 = { "Inverter", "Vine_Real" };
        static readonly Rect Gate2 = new(23.4f, -1f, .2f, 8f), Gate3 = new(41.4f, -1f, .2f, 8f);

        static object Start(string name, Vector2 checkpoint, string[] owns) => Call(Section, "Start", name, checkpoint, owns);
        static object Gated(string name, Vector2 checkpoint, Rect gate, string[] owns) => Activator.CreateInstance(Section, name, checkpoint, gate, owns, false);

        static List<string> Check(params object[] sections)
        {
            object room = TrapLabRoom11Tests.Room();
            Array array = Array.CreateInstance(Section, sections.Length);
            for (int i = 0; i < sections.Length; i++) array.SetValue(sections[i], i);
            object changed = room.GetType().GetMethod("WithCheckpointSections").Invoke(room, new object[] { array });
            return Rule("ValidateSections", "Fixture", changed);
        }

        static void AssertError(List<string> errors, string fragment) =>
            Assert.IsTrue(errors.Any(e => e.Contains(fragment)), $"expected an error containing '{fragment}', got:\n{string.Join("\n", errors)}");

        static object A1 => Start("Act1", new Vector2(2.5f, 0f), Act1);
        static object A2 => Gated("Act2", new Vector2(28.2f, 0f), Gate2, Act2);
        static object A3 => Gated("Act3", new Vector2(42.5f, 0f), Gate3, Act3);

        [Test] public void Room11AsDeclared_Passes() => CollectionAssert.IsEmpty(Check(A1, A2, A3));

        [Test]
        public void ARoomWithNoSections_HasNothingToCheck()
        {
            IList rooms = (IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null);
            for (int i = 0; i < 11; i++) CollectionAssert.IsEmpty(Rule("ValidateSections", "TrapLab" + i, rooms[i]));
        }

        [Test] public void OneSection_IsRejected() => AssertError(Check(Start("Act1", new Vector2(2.5f, 0f), Act1.Concat(Act2).Concat(Act3).ToArray())), "declare none instead");

        [Test] public void SectionZeroWithAGate_IsRejected() => AssertError(Check(Gated("Act1", new Vector2(2.5f, 0f), new Rect(1f, -1f, .2f, 8f), Act1), A2, A3), "is the start and has no gate");

        [Test] public void SectionZeroAwayFromTheRoomsCheckpoint_IsRejected() => AssertError(Check(Start("Act1", new Vector2(4f, 0f), Act1), A2, A3), "is not the room's Checkpoint element");

        [Test] public void ALaterSectionWithNoGate_IsRejected() => AssertError(Check(A1, Start("Act2", new Vector2(28.2f, 0f), Act2), A3), "has no gate");

        [Test] public void ACheckpointInMidAir_IsRejected() => AssertError(Check(A1, Gated("Act2", new Vector2(28.2f, .5f), Gate2, Act2), A3), "isn't standable");

        [Test] public void ACheckpointOutsideTheRoom_IsRejected() => AssertError(Check(A1, A2, Gated("Act3", new Vector2(70f, 0f), Gate3, Act3)), "is outside the room");

        [Test] public void ACheckpointInsideItsGate_IsRejected() => AssertError(Check(A1, Gated("Act2", new Vector2(28.2f, 0f), new Rect(27f, -1f, 3f, 8f), Act2), A3), "inside its own gate");

        [Test] public void ACheckpointOnTheNearSideOfItsGate_IsRejected() => AssertError(Check(A1, Gated("Act2", new Vector2(15f, 0f), Gate2, Act2), A3), "on the same side of its gate");

        [Test] public void AnUnownedTrap_IsRejected() => AssertError(Check(A1, A2, Gated("Act3", new Vector2(42.5f, 0f), Gate3, new[] { "Inverter" })), "'Vine_Real' is owned by no checkpoint section");

        [Test] public void ATrapOwnedTwice_IsRejected() => AssertError(Check(A1, A2, Gated("Act3", new Vector2(42.5f, 0f), Gate3, Act3.Concat(new[] { "Geyser" }).ToArray())), "'Geyser' is owned by both");

        [Test] public void OwningStaticGeometryOrAMissingName_IsRejected()
        {
            List<string> errors = Check(A1, A2, Gated("Act3", new Vector2(42.5f, 0f), Gate3, Act3.Concat(new[] { "Cliff", "Nothing" }).ToArray()));
            AssertError(errors, "a Wall with no runtime state");
            AssertError(errors, "'Nothing', which isn't an element");
        }

        // ---------- the route half (RouteValidator.ValidateSections) ----------

        static List<string> RouteRules(object room, object solution)
        {
            using IDisposable session = OpenSession();
            object routes = Activator.CreateInstance(T("RoomRoutes"), solution, Array.CreateInstance(T("Betrayal"), 0));
            object report = Call(T("RouteValidator"), "ValidateSections", session, "Fixture", room, routes, null);
            return (List<string>)F(report, "Errors");
        }

        static object Sectioned(params object[] sections)
        {
            object room = TrapLabRoom11Tests.Room();
            Array array = Array.CreateInstance(Section, sections.Length);
            for (int i = 0; i < sections.Length; i++) array.SetValue(sections[i], i);
            return room.GetType().GetMethod("WithCheckpointSections").Invoke(room, new object[] { array });
        }

        static object Solution() => F(TrapLabRoom11Tests.Routes(), "Solution");

        [TearDown] public void RestoreScene() => FreshScratchScene();

        // Gates pass in order only: with Act3's gate before Act2's along the solution, the solution never passes Act3's.
        [Test]
        public void GatesOutOfOrderAlongTheSolution_AreRejected() =>
            AssertError(RouteRules(Sectioned(A1, Gated("Act2", new Vector2(42.5f, 0f), Gate3, Act2), Gated("Act3", new Vector2(28.2f, 0f), Gate2, Act3)), Solution()),
                "never passes the gate of section 'Act3'");

        // The solution with 800 more ticks under the Overhang (safe cover): Act2 takes over 1000 ticks.
        [Test]
        public void ASectionOver1000Ticks_IsRejected()
        {
            object solution = Solution();
            var steps = ((IEnumerable)F(solution, "Steps")).Cast<object>().ToList();
            int at = steps.FindIndex(x => (string)F(x, "Label") == "Until(X>=27.8)");
            Assert.Greater(at, 0, "precondition: the Overhang stop");
            steps.Insert(at + 3, Call(T("R"), "For", 800));
            Array array = Array.CreateInstance(T("RouteStep"), steps.Count);
            for (int i = 0; i < steps.Count; i++) array.SetValue(steps[i], i);
            object slow = Activator.CreateInstance(T("Route"), "slow solution", array);
            AssertError(RouteRules(Sectioned(A1, A2, A3), slow), "over 1000");
        }

        // Act3's checkpoint on Spikes_Back: standing still there dies at once.
        [Test]
        public void ACheckpointThatKillsAStandingCat_IsRejected() =>
            AssertError(RouteRules(Sectioned(A1, A2, Gated("Act3", new Vector2(45.5f, 0f), Gate3, Act3)), Solution()),
                "standing still at section 'Act3''s checkpoint dies");

        [Test] public void DuplicateOrElementNames_AreRejected()
        {
            AssertError(Check(A1, A2, Gated("Act2", new Vector2(42.5f, 0f), Gate3, Act3)), "two checkpoint sections are named 'Act2'");
            AssertError(Check(A1, Gated("Geyser", new Vector2(28.2f, 0f), Gate2, Act2), A3), "has an element's name");
        }
    }
}
