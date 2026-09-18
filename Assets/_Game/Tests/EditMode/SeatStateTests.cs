using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    public class SeatStateTests
    {
        [Test]
        public void SitOnEmptyStation_SetsCatAndStation()
        {
            var state = new SeatState();
            object cat = new object();
            object station = new object();

            Assert.IsTrue(state.TryOccupy(cat, station));
            Assert.AreSame(cat, state.Occupant);
            Assert.AreSame(station, state.Station);
        }

        [Test]
        public void InteractWhileSeated_ReleaseClearsBothSides()
        {
            SeatState state = Occupied();

            Assert.IsTrue(state.Release());
            Assert.IsNull(state.Occupant);
            Assert.IsNull(state.Station);
        }

        [Test]
        public void ReleaseUnseated_IsNoOp()
        {
            Assert.IsFalse(new SeatState().Release());
        }

        [Test]
        public void ReleaseTwice_SecondCallIsNoOp()
        {
            SeatState state = Occupied();

            Assert.IsTrue(state.Release());
            Assert.IsFalse(state.Release());
        }

        [Test]
        public void OccupiedStation_RejectsSecondCat()
        {
            var state = new SeatState();
            object firstCat = new object();
            object station = new object();
            state.TryOccupy(firstCat, station);

            Assert.IsFalse(state.TryOccupy(new object(), station));
            Assert.AreSame(firstCat, state.Occupant);
            Assert.AreSame(station, state.Station);
        }

        [Test]
        public void Release_CallbackObservesNoHalfReleasedState()
        {
            SeatState state = Occupied();
            state.Released += () =>
            {
                Assert.IsNull(state.Occupant);
                Assert.IsNull(state.Station);
            };

            state.Release();
        }

        [Test]
        public void SeatCommandFilter_WhileSeatedFiltersMoveAndJumpButKeepsInteract()
        {
            var command = new CatCommand { Move = 1f, JumpPressed = true, JumpHeld = true, InteractPressed = true };
            CatCommand filtered = SeatCommandFilter.Apply(command, true);

            Assert.AreEqual(0f, filtered.Move);
            Assert.IsFalse(filtered.JumpPressed);
            Assert.IsFalse(filtered.JumpHeld);
            Assert.IsTrue(filtered.InteractPressed);
        }

        [Test]
        public void SeatCommandFilter_AfterReleasePassesTheNextReadUnfiltered()
        {
            var command = new CatCommand { Move = 1f, JumpPressed = true };
            SeatState state = Occupied();
            state.Release();

            CatCommand filtered = SeatCommandFilter.Apply(command, state.IsOccupied);
            Assert.AreEqual(1f, filtered.Move);
            Assert.IsTrue(filtered.JumpPressed);
        }

        [Test]
        public void SeatInteractRouting_SeatedInteract_ConsumesEdgeAndRequestsStand()
        {
            var command = new CatCommand { InteractPressed = true };

            Assert.IsTrue(SeatInteractRouting.Route(ref command, true));
            Assert.IsFalse(command.InteractPressed);
        }

        [Test]
        public void SeatInteractRouting_SeatedWithoutInteract_LeavesCommandUnchanged()
        {
            var command = new CatCommand { Move = 1f, InteractPressed = false };

            Assert.IsFalse(SeatInteractRouting.Route(ref command, true));
            Assert.AreEqual(1f, command.Move);
            Assert.IsFalse(command.InteractPressed);
        }

        [Test]
        public void SeatInteractRouting_StandingInteract_PassesThroughForSitting()
        {
            var command = new CatCommand { InteractPressed = true };

            Assert.IsFalse(SeatInteractRouting.Route(ref command, false));
            Assert.IsTrue(command.InteractPressed);
        }

        [Test]
        public void SeatInteractRouting_StandingTickFiltersMotorAndPreventsResit()
        {
            var command = new CatCommand { Move = 1f, JumpPressed = true, JumpHeld = true, InteractPressed = true };

            Assert.IsTrue(SeatInteractRouting.Route(ref command, true));
            CatCommand motorCommand = SeatCommandFilter.Apply(command, true);

            Assert.AreEqual(0f, motorCommand.Move);
            Assert.IsFalse(motorCommand.JumpPressed);
            Assert.IsFalse(motorCommand.JumpHeld);
            Assert.IsFalse(motorCommand.InteractPressed);
        }

        static SeatState Occupied()
        {
            var state = new SeatState();
            state.TryOccupy(new object(), new object());
            return state;
        }
    }
}
