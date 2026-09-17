using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Anchors;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Transport;
using UnityEngine;

namespace Parallax.Gameplay.Sensors
{
    public sealed class PressurePlateSensor : MonoBehaviour
    {
        [SerializeField] AnchorDefinition definition;
        [SerializeField] TransportHost transportHost;
        [SerializeField] ObserverSet observers;
        [SerializeField] Vector2 sensorSize = new Vector2(1.2f, 0.6f);
        [SerializeField] Vector2 sensorOffset = new Vector2(0f, 0.3f);
        [SerializeField] SpriteRenderer indicator;

        readonly List<Collider2D> results = new List<Collider2D>();
        RealityRoot root;
        SensorLatch latch;
        ContactFilter2D filter;
        AnchorRequester requester;
        CatMotor2D cachedCat;
        Collider2D cachedCollider;
        Color indicatorColor;
        bool initialized;
        bool missingLogged;
        bool requesterErrorLogged;
        bool hasIndicatorColor;

        void OnEnable()
        {
            EnsureInit();
            if (observers != null) observers.Stepped += OnStepped;
        }

        void OnDisable()
        {
            if (observers != null) observers.Stepped -= OnStepped;
        }

        void EnsureInit()
        {
            if (initialized) return;
            root = GetComponentInParent<RealityRoot>();
            if (definition == null || transportHost == null || observers == null || root == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogError($"PressurePlateSensor '{name}' needs an AnchorDefinition, TransportHost, ObserverSet, and RealityRoot parent.", this);
                }
                return;
            }
            latch = new SensorLatch(definition.InitialValue);
            filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = root.PhysicsMask,
                useTriggers = false,
            };
            initialized = true;
        }

        void OnStepped(int tick)
        {
            EnsureInit();
            if (!initialized) return;
            ObserverContext context = observers.Get(root.Id);
            bool occupied = IsOccupied(context);
            SetIndicator(occupied);
            if (!EnsureRequester()) return;
            if (!latch.Update(occupied, out float target)) return;
            requester.Request(definition.Id, target);
        }

        bool EnsureRequester()
        {
            if (requester != null) return true;
            if (transportHost.Transport == null || transportHost.Sequencer == null)
            {
                if (!requesterErrorLogged)
                {
                    requesterErrorLogged = true;
                    Debug.LogError($"PressurePlateSensor '{name}' cannot create its requester because TransportHost is incomplete.", this);
                }
                return false;
            }
            requester = new AnchorRequester(transportHost.Transport, transportHost.Sequencer, EventOrigins.Sensor(root.Id));
            return true;
        }

        bool IsOccupied(ObserverContext context)
        {
            if (context == null || context.Cat == null || context.Driver == null || !SensorPolicy.Counts(context.Driver.Kind)) return false;
            if (cachedCat != context.Cat)
            {
                cachedCat = context.Cat;
                cachedCollider = cachedCat.GetComponent<Collider2D>();
            }
            if (cachedCollider == null) return false;
            results.Clear();
            Vector2 center = transform.position + (Vector3)(Quaternion.Euler(0f, 0f, transform.eulerAngles.z) * sensorOffset);
            int count = Physics2D.OverlapBox(center, sensorSize, transform.eulerAngles.z, filter, results);
            for (int i = 0; i < count; i++)
                if (results[i] == cachedCollider) return true;
            return false;
        }

        void SetIndicator(bool occupied)
        {
            if (indicator == null) return;
            Color color = occupied ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            if (hasIndicatorColor && indicatorColor == color) return;
            indicatorColor = color;
            hasIndicatorColor = true;
            indicator.color = color;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(sensorOffset, sensorSize);
            Gizmos.matrix = old;
        }
    }
}
