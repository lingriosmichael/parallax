using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests
{
    public class AnchorRegistryTests
    {
        static readonly AnchorId Id = new AnchorId(1);

        [Test]
        public void Register_ThenTryGet_ReturnsInitialValueAndRevisionZero()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0.5f);

            bool found = registry.TryGet(Id, out AnchorState state);

            Assert.IsTrue(found);
            Assert.AreEqual(0.5f, state.Value);
            Assert.AreEqual(0u, state.Revision);
        }

        [Test]
        public void Register_Twice_ReturnsFalseAndLeavesStateUnchanged()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0.5f);

            bool second = registry.Register(Id, 0.9f);

            Assert.IsFalse(second);
            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(0.5f, state.Value);
            Assert.AreEqual(0u, state.Revision);
        }

        [Test]
        public void Commit_Applies_SetsValueRevisionAndRaisesChangedOnce()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);

            int changedCount = 0;
            AnchorState lastChanged = default;
            registry.Changed += (id, state) =>
            {
                changedCount++;
                lastChanged = state;
            };

            var request = new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1);
            CommitResult result = registry.Commit(request);

            Assert.AreEqual(CommitResult.Applied, result);
            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(1f, lastChanged.Value);
            Assert.AreEqual(1u, lastChanged.Revision);
        }

        [Test]
        public void Commit_SameOriginSequenceTwice_SecondIsDuplicate_NoRevisionChangeNoEvent()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);
            registry.Commit(new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1));

            int changedCount = 0;
            registry.Changed += (id, state) => changedCount++;

            CommitResult result = registry.Commit(new AnchorRequest(Id, 0f, EventOrigin.HumanA, 1));

            Assert.AreEqual(CommitResult.Duplicate, result);
            Assert.AreEqual(0, changedCount);
            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(1u, state.Revision);
        }

        [Test]
        public void Commit_OlderSequenceAfterNewer_IsDuplicate_ValueStaysAtNewerTarget()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);
            registry.Commit(new AnchorRequest(Id, 0.5f, EventOrigin.HumanA, 5));

            CommitResult result = registry.Commit(new AnchorRequest(Id, 0.9f, EventOrigin.HumanA, 3));

            Assert.AreEqual(CommitResult.Duplicate, result);
            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(0.5f, state.Value);
        }

        [Test]
        public void Commit_DifferentOrigins_SameSequenceNumber_AreIndependent()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);

            CommitResult resultA = registry.Commit(new AnchorRequest(Id, 0.3f, EventOrigin.HumanA, 1));
            CommitResult resultB = registry.Commit(new AnchorRequest(Id, 0.6f, EventOrigin.HumanB, 1));

            Assert.AreEqual(CommitResult.Applied, resultA);
            Assert.AreEqual(CommitResult.Applied, resultB);
        }

        [Test]
        public void Commit_TargetEqualsCurrent_NoChange_ButSequenceRecorded()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0.5f);

            int changedCount = 0;
            registry.Changed += (id, state) => changedCount++;

            CommitResult result = registry.Commit(new AnchorRequest(Id, 0.5f, EventOrigin.HumanA, 1));

            Assert.AreEqual(CommitResult.NoChange, result);
            Assert.AreEqual(0, changedCount);
            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(0u, state.Revision);

            CommitResult resend = registry.Commit(new AnchorRequest(Id, 0.5f, EventOrigin.HumanA, 1));
            Assert.AreEqual(CommitResult.Duplicate, resend);
        }

        [Test]
        public void Commit_UnknownAnchor_ReturnsUnknownAnchor()
        {
            var registry = new AnchorRegistry();

            CommitResult result = registry.Commit(new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1));

            Assert.AreEqual(CommitResult.UnknownAnchor, result);
        }

        [Test]
        public void Commit_NaNTarget_ReturnsInvalidValue_AndSequenceNotRecorded()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);

            CommitResult result = registry.Commit(new AnchorRequest(Id, float.NaN, EventOrigin.HumanA, 1));
            Assert.AreEqual(CommitResult.InvalidValue, result);

            // Sequence 1 was not recorded, so it can still be applied normally.
            CommitResult followUp = registry.Commit(new AnchorRequest(Id, 0.7f, EventOrigin.HumanA, 1));
            Assert.AreEqual(CommitResult.Applied, followUp);
        }

        [Test]
        public void Commit_InfiniteTarget_ReturnsInvalidValue_AndSequenceNotRecorded()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);

            CommitResult positiveInfinity = registry.Commit(new AnchorRequest(Id, float.PositiveInfinity, EventOrigin.HumanA, 1));
            Assert.AreEqual(CommitResult.InvalidValue, positiveInfinity);

            CommitResult negativeInfinity = registry.Commit(new AnchorRequest(Id, float.NegativeInfinity, EventOrigin.HumanA, 1));
            Assert.AreEqual(CommitResult.InvalidValue, negativeInfinity);

            // Sequence 1 was not recorded by either invalid commit, so it can still be applied normally.
            CommitResult followUp = registry.Commit(new AnchorRequest(Id, 0.7f, EventOrigin.HumanA, 1));
            Assert.AreEqual(CommitResult.Applied, followUp);
        }

        [Test]
        public void ThreeAppliedCommits_RevisionIsThree()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);

            registry.Commit(new AnchorRequest(Id, 0.1f, EventOrigin.HumanA, 1));
            registry.Commit(new AnchorRequest(Id, 0.2f, EventOrigin.HumanA, 2));
            registry.Commit(new AnchorRequest(Id, 0.3f, EventOrigin.HumanA, 3));

            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(3u, state.Revision);
        }

        [Test]
        public void ApplyReplicated_NewerRevision_AppliesAndRaisesEvent()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);

            int changedCount = 0;
            registry.Changed += (id, state) => changedCount++;

            registry.ApplyReplicated(Id, new AnchorState { Value = 0.8f, Revision = 2 });

            Assert.AreEqual(1, changedCount);
            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(0.8f, state.Value);
            Assert.AreEqual(2u, state.Revision);
        }

        [Test]
        public void ApplyReplicated_EqualOrOlderRevision_Ignored_NoEvent()
        {
            var registry = new AnchorRegistry();
            registry.Register(Id, 0f);
            registry.ApplyReplicated(Id, new AnchorState { Value = 0.8f, Revision = 2 });

            int changedCount = 0;
            registry.Changed += (id, state) => changedCount++;

            registry.ApplyReplicated(Id, new AnchorState { Value = 0.9f, Revision = 2 });
            registry.ApplyReplicated(Id, new AnchorState { Value = 0.1f, Revision = 1 });

            Assert.AreEqual(0, changedCount);
            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(0.8f, state.Value);
            Assert.AreEqual(2u, state.Revision);
        }

        [Test]
        public void ApplyReplicated_UnknownAnchor_IsCreated()
        {
            var registry = new AnchorRegistry();

            registry.ApplyReplicated(Id, new AnchorState { Value = 0.4f, Revision = 1 });

            bool found = registry.TryGet(Id, out AnchorState state);
            Assert.IsTrue(found);
            Assert.AreEqual(0.4f, state.Value);
            Assert.AreEqual(1u, state.Revision);
        }

        [Test]
        public void EventSequencer_StartsAtOne_PerOriginIndependent_Monotonic()
        {
            var sequencer = new EventSequencer();

            Assert.AreEqual(1u, sequencer.Next(EventOrigin.HumanA));
            Assert.AreEqual(2u, sequencer.Next(EventOrigin.HumanA));
            Assert.AreEqual(1u, sequencer.Next(EventOrigin.HumanB));
            Assert.AreEqual(3u, sequencer.Next(EventOrigin.HumanA));
        }
    }
}
