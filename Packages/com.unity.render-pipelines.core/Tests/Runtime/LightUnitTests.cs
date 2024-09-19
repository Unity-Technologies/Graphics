using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class LightUnitTests
    {
        GameObject go;
        const float epsilon = 0.001f;

        [SetUp]
        public void Setup()
        {
            GameObject go = new GameObject("Light", typeof(Light));
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(go);
        }

        [Test]
        public void LightUnitSupport()
        {
            // Directional and box lights should only support Lux
            LightType t = LightType.Directional;
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lumen));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Candela));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lux));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Nits));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Ev100));
            t = LightType.Box;
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lumen));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Candela));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lux));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Nits));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Ev100));
            // Point lights should only support Lumen, Candela, Lux, EV100
            t = LightType.Point;
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lumen));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Candela));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lux));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Nits));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Ev100));
            // Spot lights should only support Lumen, Candela, Lux, EV100
            t = LightType.Spot;
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lumen));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Candela));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lux));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Nits));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Ev100));
            // Area lights should only support Lumen, Nits, EV100
            t = LightType.Rectangle;
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lumen));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Candela));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lux));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Nits));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Ev100));
            t = LightType.Disc;
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lumen));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Candela));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lux));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Nits));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Ev100));
            t = LightType.Tube;
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lumen));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Candela));
            Assert.False(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Lux));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Nits));
            Assert.True(LightUnitUtils.IsLightUnitSupported(t, LightUnit.Ev100));
        }

        [Test]
        public void DirectionalAndBoxLightUnitConversion()
        {
            Light l = GameObject.FindAnyObjectByType<Light>().GetComponent<Light>();

            l.type = LightType.Directional;
            Assert.AreEqual(3f, LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Lux));

            l.type = LightType.Box;
            Assert.AreEqual(3f, LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Lux));
        }

        [Test]
        public void PointLightUnitConversion()
        {
            // Point light
            Light l = GameObject.FindAnyObjectByType<Light>().GetComponent<Light>();

            l.type = LightType.Point;
            l.luxAtDistance = 3f;
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Candela), Is.EqualTo(3f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Candela), Is.EqualTo(0.238732412f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Lumen), Is.EqualTo(37.6991119f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Lux), Is.EqualTo(0.0265258234f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Lumen), Is.EqualTo(339.292023f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Ev100), Is.EqualTo(0.933466434f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lumen), Is.EqualTo(12.566371f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Lux), Is.EqualTo(0.333333343f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Candela), Is.EqualTo(27.0f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Ev100), Is.EqualTo(4.58496237f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Candela), Is.EqualTo(1f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Ev100), Is.EqualTo(7.75488758f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lux), Is.EqualTo(0.111111112f).Within(epsilon));
        }

        [Test]
        public void SpotLightUnitConversion()
        {
            Light l = GameObject.FindAnyObjectByType<Light>().GetComponent<Light>();

            l.type = LightType.Spot;
            l.enableSpotReflector = false;
            l.luxAtDistance = 3f;
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Candela), Is.EqualTo(3f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Candela), Is.EqualTo(0.238732412f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Lumen), Is.EqualTo(37.6991119f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Lux), Is.EqualTo(0.0265258234f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Lumen), Is.EqualTo(339.292023f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Ev100), Is.EqualTo(0.933466434f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lumen), Is.EqualTo(12.566371f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Lux), Is.EqualTo(0.333333343f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Candela), Is.EqualTo(27.0f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Ev100), Is.EqualTo(4.58496237f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Candela), Is.EqualTo(1f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Ev100), Is.EqualTo(7.75488758f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lux), Is.EqualTo(0.111111112f).Within(epsilon));

            l.enableSpotReflector = true;
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Candela), Is.EqualTo(3f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Candela), Is.EqualTo(14.0125141f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Lumen), Is.EqualTo(0.642283022f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Lux), Is.EqualTo(1.55694604f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Lumen), Is.EqualTo(5.78054714f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Ev100), Is.EqualTo(6.80864382f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lumen), Is.EqualTo(0.214094341f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Lux), Is.EqualTo(0.333333343f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Candela), Is.EqualTo(27.0f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Candela, LightUnit.Ev100), Is.EqualTo(4.58496237f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Candela), Is.EqualTo(1f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lux, LightUnit.Ev100), Is.EqualTo(7.75488758f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lux), Is.EqualTo(0.111111112f).Within(epsilon));
        }

        [Test]
        public void AreaLightUnitConversion()
        {
            Light l = GameObject.FindAnyObjectByType<Light>().GetComponent<Light>();

            l.areaSize = new Vector2(4f, 5f);

            l.type = LightType.Rectangle;
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Nits), Is.EqualTo(0.0477464795f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Nits, LightUnit.Lumen), Is.EqualTo(188.495575f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Ev100), Is.EqualTo(-1.38846159f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lumen), Is.EqualTo(62.8318558f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Nits, LightUnit.Ev100), Is.EqualTo(4.58496237f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Nits), Is.EqualTo(1f).Within(epsilon));

            l.type = LightType.Disc;
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Nits), Is.EqualTo(0.0189977214f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Nits, LightUnit.Lumen), Is.EqualTo(473.741028f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Ev100), Is.EqualTo(-2.71802998f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lumen), Is.EqualTo(157.913681f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Nits, LightUnit.Ev100), Is.EqualTo(4.58496237f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Nits), Is.EqualTo(1f).Within(epsilon));

            l.type = LightType.Tube;
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Nits), Is.EqualTo(0.0596831031f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Nits, LightUnit.Lumen), Is.EqualTo(150.796448f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Lumen, LightUnit.Ev100), Is.EqualTo(-1.06653357f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Lumen), Is.EqualTo(50.2654839f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Nits, LightUnit.Ev100), Is.EqualTo(4.58496237f).Within(epsilon));
            Assert.That(LightUnitUtils.ConvertIntensity(l, 3f, LightUnit.Ev100, LightUnit.Nits), Is.EqualTo(1f).Within(epsilon));
        }
    }
}
