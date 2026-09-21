using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Parallax.Tests
{
    public sealed class CatColliderConfigTests
    {
        [Test]
        public void DefaultColliderGeometry_IsHorizontalAndKeepsThePawLine()
        {
            CatMotorConfig config = ScriptableObject.CreateInstance<CatMotorConfig>();

            Assert.That(config.ColliderSize, Is.EqualTo(new Vector2(1f, 0.56f)));
            Assert.That(config.ColliderOffset, Is.EqualTo(new Vector2(0f, -0.12f)));
            Assert.That(config.ColliderBottom, Is.EqualTo(-0.4f).Within(0.0001f));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void SetupAppliesConfiguredColliderGeometryToBothCats()
        {
            CatMotorConfig config = ScriptableObject.CreateInstance<CatMotorConfig>();
            var first = new GameObject("Cat A").AddComponent<CapsuleCollider2D>();
            var second = new GameObject("Cat B").AddComponent<CapsuleCollider2D>();
            var changes = new List<string>();

            ApplyConfiguredCollider(first, config, changes);
            ApplyConfiguredCollider(second, config, changes);

            AssertColliderMatchesConfig(first, config);
            AssertColliderMatchesConfig(second, config);

            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SetupMissingSerializedProperty_LogsAContextualErrorAndReturnsFalse()
        {
            var body = new GameObject("Missing Config").AddComponent<Rigidbody2D>();
            var serialized = new SerializedObject(body);

            LogAssert.Expect(LogType.Error, "CatPlayerSetup: Rigidbody2D on 'Assets/_Game/Gameplay/Player/Cat_Player.prefab' has no serialized 'config' field. Stopping without saving.");
            bool found = TryGetRequiredProperty(serialized, "config");

            Assert.That(found, Is.False);
            Object.DestroyImmediate(body.gameObject);
        }

        static void ApplyConfiguredCollider(CapsuleCollider2D collider, CatMotorConfig config, List<string> changes)
        {
            var type = System.Type.GetType("Parallax.Editor.CatPlayerSetup, Parallax.Editor");
            var method = type.GetMethod("ApplyConfiguredCollider", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, new object[] { collider, config, changes });
        }

        static bool TryGetRequiredProperty(SerializedObject serialized, string propertyName)
        {
            var type = System.Type.GetType("Parallax.Editor.CatPlayerSetup, Parallax.Editor");
            var method = type.GetMethod("TryGetRequiredProperty", BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = { serialized, propertyName, null };
            return (bool)method.Invoke(null, arguments);
        }

        static void AssertColliderMatchesConfig(CapsuleCollider2D collider, CatMotorConfig config)
        {
            Assert.That(collider.direction, Is.EqualTo(CapsuleDirection2D.Horizontal));
            Assert.That(collider.size, Is.EqualTo(config.ColliderSize));
            Assert.That(collider.offset, Is.EqualTo(config.ColliderOffset));
        }
    }
}
