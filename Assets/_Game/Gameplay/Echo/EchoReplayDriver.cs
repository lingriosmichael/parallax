using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Transport;
using UnityEngine;

namespace Parallax.Gameplay.Echo
{
    public sealed class EchoReplayDriver : IObserverDriver
    {
        readonly EchoPlayback playback;
        readonly IAnchorRequester requester;
        readonly IRealityTransport transport;
        readonly List<EchoAnchorEvent> due = new List<EchoAnchorEvent>();
        readonly List<EchoControlEvent> dueControls = new List<EchoControlEvent>();
        ObserverContext observer;
        Rigidbody2D body;
        CatMotor2D cat;
        bool loggedMissingDependencies;

        public InputSourceKind Kind => InputSourceKind.EchoReplay;
        public int Cursor => playback.Cursor;
        public int FrameCount => playback.FrameCount;
        public bool IsHolding => playback.IsHolding;

        public EchoReplayDriver(EchoPlayback playback, IAnchorRequester requester, IRealityTransport transport)
        {
            this.playback = playback ?? throw new ArgumentNullException(nameof(playback));
            this.requester = requester ?? throw new ArgumentNullException(nameof(requester));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public void Activate(ObserverContext observer)
        {
            this.observer = observer;
            cat = observer != null ? observer.Cat : null;
            body = cat != null ? cat.GetComponent<Rigidbody2D>() : null;
            if (body == null || observer == null || observer.Reality == null)
            {
                LogMissingDependencies();
                return;
            }
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            Apply(playback.Current, true);
        }

        public void FixedTick(int tick)
        {
            if (body == null || observer == null || observer.Reality == null) return;
            EchoFrame frame = playback.Advance(due, dueControls);
            Apply(frame, false);
            for (int i = 0; i < due.Count; i++) requester.Request(due[i].Anchor, due[i].TargetValue);
            for (int i = 0; i < dueControls.Count; i++)
                transport.PublishControl(new ControlSample(dueControls[i].Channel, dueControls[i].Target, dueControls[i].Value, transport.Tick));
        }

        public void Deactivate()
        {
            if (body == null) return;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            if (observer != null && observer.Gravity != null) observer.Gravity.SetTargetDirection(playback.Current.GravityDirection);
        }

        void Apply(EchoFrame frame, bool snap)
        {
            Vector2 world = observer.Reality.ToWorld(frame.Position);
            if (snap)
            {
                body.position = world;
                body.rotation = frame.Rotation;
            }
            else
            {
                body.MovePosition(world);
                body.MoveRotation(frame.Rotation);
            }
            if (cat != null) cat.SetFacing(frame.FacingRight);
        }

        void LogMissingDependencies()
        {
            if (loggedMissingDependencies) return;
            loggedMissingDependencies = true;
            Debug.LogError("EchoReplayDriver.Activate: missing ObserverContext, CatMotor2D/Rigidbody2D, or RealityRoot.");
        }
    }
}
