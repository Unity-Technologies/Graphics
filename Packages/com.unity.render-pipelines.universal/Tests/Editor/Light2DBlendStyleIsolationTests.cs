using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // UUM-161536: a 2D light must not change when a light that does not overlap it switches to
    // another blend style. On OpenGLCore with NVIDIA drivers, the MRT light pass wrote the
    // Multiply light's colour into the Additive light texture as well, so the Multiply light got brighter.
    class Light2DBlendStyleIsolationTests
    {
        const string k_Dir = "Assets/Light2DBlendStyleIsolationTests";
        const int k_Size = 256;

        RenderPipelineAsset m_PrevDefault;
        RenderPipelineAsset m_PrevQuality;
        Camera m_Camera;
        RenderTexture m_RT;
        Texture2D m_Readback;
        Texture2D m_SpriteTex;
        Sprite m_Sprite;

        [SetUp]
        public void SetUp()
        {
            m_PrevDefault = GraphicsSettings.defaultRenderPipeline;
            m_PrevQuality = QualitySettings.renderPipeline;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Cannot run: there is no graphics device to draw with.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (!AssetDatabase.IsValidFolder(k_Dir))
                AssetDatabase.CreateFolder("Assets", "Light2DBlendStyleIsolationTests");
            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(rendererData, k_Dir + "/Renderer2D.asset");
            var urp = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(urp, k_Dir + "/URP.asset");
            AssetDatabase.SaveAssets();
            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
        }

        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_PrevDefault;
            QualitySettings.renderPipeline = m_PrevQuality;
            if (m_Camera != null) Object.DestroyImmediate(m_Camera.gameObject);
            if (m_RT != null) { m_RT.Release(); Object.DestroyImmediate(m_RT); }
            if (m_Readback != null) Object.DestroyImmediate(m_Readback);
            if (m_Sprite != null) Object.DestroyImmediate(m_Sprite);
            if (m_SpriteTex != null) Object.DestroyImmediate(m_SpriteTex);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(k_Dir);
        }

        [Test]
        public void MultiplyLight_IsUnchanged_WhenNonOverlappingLightSwitchesToAdditive()
        {
            var cam = SetUpScene(out var lightB, out int ax, out int ay, out _, out _);

            var withMultiply = RenderAndSample(cam, ax, ay);
            lightB.blendStyleIndex = 1; // Additive
            var withAdditive = RenderAndSample(cam, ax, ay);

            const float tolerance = 2f / 255f;
            string msg = $"Light A centre changed when non-overlapping light B switched Multiply -> Additive " +
                         $"(device {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}). " +
                         $"B Multiply: {withMultiply}, B Additive: {withAdditive}";
            Assert.That(withAdditive.r, Is.EqualTo(withMultiply.r).Within(tolerance), msg);
            Assert.That(withAdditive.g, Is.EqualTo(withMultiply.g).Within(tolerance), msg);
            Assert.That(withAdditive.b, Is.EqualTo(withMultiply.b).Within(tolerance), msg);
        }

        // Each blend style must still light its own area when two styles are active in one frame.
        [Test]
        public void BothLights_StillLightTheirOwnArea_WhenTwoBlendStylesAreActive()
        {
            var cam = SetUpScene(out var lightB, out int ax, out int ay, out int bx, out int by);

            lightB.enabled = false;
            var bOff = RenderAndSample(cam, bx, by);
            lightB.enabled = true;
            lightB.blendStyleIndex = 1; // Additive
            var aLit = RenderAndSample(cam, ax, ay);
            var bLit = m_Readback.GetPixel(bx, by);

            string msg = $"(device {SystemInfo.graphicsDeviceType}) A: {aLit}, B off: {bOff}, B Additive: {bLit}";
            // A is an orange Multiply light on a 0.1 global light, so its centre is well above 0.1 in red.
            Assert.Greater(aLit.r, 0.3f, "Multiply light A did not render. " + msg);
            // B is a white Additive light, so its centre must be brighter than with B disabled.
            Assert.Greater(bLit.r, bOff.r + 0.1f, "Additive light B did not render. " + msg);
            Assert.Greater(bLit.g, bOff.g + 0.1f, "Additive light B did not render. " + msg);
        }

        Camera SetUpScene(out Light2D lightB, out int ax, out int ay, out int bx, out int by)
        {
            var cam = m_Camera = new GameObject("Camera").AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0.3f);
            m_RT = new RenderTexture(k_Size, k_Size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = m_RT;

            // A white Sprite-Lit sprite that covers the whole view.
            m_SpriteTex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            m_SpriteTex.SetPixels(px);
            m_SpriteTex.Apply();
            var sr = new GameObject("Background").AddComponent<SpriteRenderer>();
            sr.sprite = m_Sprite = Sprite.Create(m_SpriteTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f / 20f);
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

            var global = new GameObject("Global").AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.1f;

            // Light A (Multiply) at x=-3, light B at x=+3. Radius 1.5, so they never overlap.
            MakeLight("LightA", new Vector3(-3, 0, 0), 0, new Color(1f, 0.5f, 0.25f));
            lightB = MakeLight("LightB", new Vector3(3, 0, 0), 0, Color.white);

            // Light centres: x=-3 and x=+3 in [-5,5] -> u=0.2 and u=0.8.
            ax = (int)(0.2f * k_Size); ay = k_Size / 2;
            bx = (int)(0.8f * k_Size); by = k_Size / 2;
            return cam;
        }

        static Light2D MakeLight(string name, Vector3 pos, int blendStyle, Color color)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.pointLightOuterRadius = 1.5f;
            l.pointLightInnerRadius = 1.0f;
            l.blendStyleIndex = blendStyle;
            l.color = color;
            l.intensity = 0.5f;
            return l;
        }

        Color RenderAndSample(Camera cam, int x, int y)
        {
            // LateUpdate does not run in EditMode tests, so build light meshes and bounds by hand.
            foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
            {
                if (l.lightType == Light2D.LightType.Global) continue;
                l.UpdateMesh(true);
                l.UpdateBoundingSphere();
            }

            for (int i = 0; i < 3; i++)
                cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = m_RT;
            if (m_Readback == null)
                m_Readback = new Texture2D(k_Size, k_Size, TextureFormat.RGBA32, false);
            m_Readback.ReadPixels(new Rect(0, 0, k_Size, k_Size), 0, 0);
            m_Readback.Apply();
            RenderTexture.active = prev;
            return m_Readback.GetPixel(x, y);
        }
    }

    // Kept apart from Light2DBlendStyleIsolationTests so it skips that fixture's asset and scene setup,
    // and still runs without a graphics device.
    class Renderer2DSupportsMRTTests
    {
        [Test]
        public void SupportsMRT_IsFalseOnlyOnOpenGLDevices()
        {
            var device = SystemInfo.graphicsDeviceType;
            bool isGL = device == GraphicsDeviceType.OpenGLCore || device == GraphicsDeviceType.OpenGLES3;
            Assert.AreEqual(isGL, Renderer2D.IsGLDevice(), $"device {device}");
            Assert.AreEqual(!isGL, Renderer2D.supportsMRT, $"device {device}");
        }
    }
}
