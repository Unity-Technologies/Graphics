using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class Light2DCookieMigrationTests
    {
        const int k_Version2 = 2;
        const int k_Version3 = 3;

        readonly List<UnityEngine.Object> m_Created = new List<UnityEngine.Object>();

        Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(4, 4);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);
            sprite.name = name;
            m_Created.Add(texture);
            m_Created.Add(sprite);
            return sprite;
        }

        Light2D CreatePointLight()
        {
            var go = new GameObject("Point Light 2D");
            m_Created.Add(go);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            return light;
        }

        // Writes the serialized state of an older light and deserializes it, which runs the OnAfterDeserialize upgrade.
        static void DeserializeAsVersion(Light2D light, int componentVersion, Sprite lightCookieSprite, Sprite deprecatedPointLightCookieSprite)
        {
            var serializedObject = new SerializedObject(light);
            serializedObject.FindProperty("m_ComponentVersion").intValue = componentVersion;
            serializedObject.FindProperty("m_LightCookieSprite").objectReferenceValue = lightCookieSprite;
            serializedObject.FindProperty("m_DeprecatedPointLightCookieSprite").objectReferenceValue = deprecatedPointLightCookieSprite;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static UnityEngine.Object GetSerializedReference(Light2D light, string propertyName)
        {
            return new SerializedObject(light).FindProperty(propertyName).objectReferenceValue;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in m_Created)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            m_Created.Clear();
        }

        // UUM-156931: Version_3 data on this branch already holds the migrated cookie in m_LightCookieSprite.
        [Test]
        public void OnAfterDeserialize_Version3WithMigratedCookie_KeepsCookie()
        {
            var light = CreatePointLight();
            var cookie = CreateSprite("Cookie");

            DeserializeAsVersion(light, k_Version3, cookie, null);

            Assert.AreEqual(cookie, light.lightCookieSprite, "The upgrade cleared a cookie that was already migrated.");
        }

        // The Version_3 migration did not clear the deprecated field, so it can still hold the cookie from before a later change.
        [Test]
        public void OnAfterDeserialize_Version3WithStaleDeprecatedCookie_KeepsCookie()
        {
            var light = CreatePointLight();
            var cookie = CreateSprite("Cookie");

            DeserializeAsVersion(light, k_Version3, cookie, CreateSprite("Stale Cookie"));

            Assert.AreEqual(cookie, light.lightCookieSprite, "The stale deprecated cookie replaced the current cookie.");
        }

        [Test]
        public void OnAfterDeserialize_Version2WithDeprecatedCookie_MigratesCookie()
        {
            var light = CreatePointLight();
            var cookie = CreateSprite("Cookie");

            DeserializeAsVersion(light, k_Version2, null, cookie);

            Assert.AreEqual(cookie, light.lightCookieSprite, "The cookie in the deprecated field was not migrated.");
        }

        [Test]
        public void OnAfterDeserialize_Version2WithDeprecatedCookie_ClearsDeprecatedCookie()
        {
            var light = CreatePointLight();

            DeserializeAsVersion(light, k_Version2, null, CreateSprite("Cookie"));

            Assert.IsNull(GetSerializedReference(light, "m_DeprecatedPointLightCookieSprite"),
                "The deprecated cookie must be cleared so a later upgrade cannot apply it again.");
        }

        // Before Version_3 a point light always used the deprecated cookie, so a sprite left from an earlier light type must not become its cookie.
        [Test]
        public void OnAfterDeserialize_Version2WithoutDeprecatedCookie_HasNoCookie()
        {
            var light = CreatePointLight();

            DeserializeAsVersion(light, k_Version2, CreateSprite("Leftover Sprite"), null);

            Assert.IsTrue(light.lightCookieSprite == null, "A sprite the point light did not use became its cookie.");
        }
    }
}
