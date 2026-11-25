using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    // URP creates the additional lights constant buffer for the first camera that has an additional light, and
    // skips the reflection probe upload for the cameras that see no probe. These tests render a camera that starts
    // without an additional light or a probe, then gets one, loses it and gets it back, and check each frame: the
    // frame where the light or probe appears is shaded by it, and the frame after it is gone is shaded as before,
    // although the buffer still holds its data.
    [TestFixture]
    class LightAndProbeTransitionTests
    {
        // Layer of the test objects, which only the test camera renders.
        const int k_Layer = 30;
        const int k_TargetSize = 64;
        // In 8-bit color: the least a quad must change when the light or probe appears, and the most two frames
        // that should match may differ.
        const float k_MinChange = 0.25f;
        const float k_MaxDifference = 0.02f;

        // Away from the objects of the scene the tests run in.
        static readonly Vector3 k_Origin = new Vector3(0f, 0f, 1000f);

        // The error a player logs when it lacks a shader variant, with strict variant matching.
        static readonly Regex k_MissingVariant = new Regex(@"\bvariant\b.*\bnot found\b");

        RenderPipelineAsset m_PreviousQualityRenderPipelineAsset;
#if UNITY_EDITOR
        bool m_PreviousAllowAsyncCompilation;
#endif
        readonly List<Light> m_DisabledLights = new();
        readonly List<Camera> m_DisabledCameras = new();
        readonly HashSet<RenderingMode> m_ProjectRenderingModes = new();
        bool m_ProjectUsesReflectionProbeBlending;

        UniversalRenderPipelineAsset m_Asset;
        UniversalRendererData m_RendererData;

        readonly List<Object> m_TestObjects = new();
        Camera m_Camera;
        RenderTexture m_Target;
        Texture2D m_Readback;
        Material m_OpaqueMaterial;
        Material m_TransparentMaterial;
        Renderer m_OpaqueQuad;
        Renderer m_TransparentQuad;
        Light m_Light;
        ReflectionProbe m_Probe;
        // Color at the center of the opaque and the transparent quad in the last frame that Render read back.
        (Color opaque, Color transparent) m_Frame;
        readonly List<string> m_LoggedErrors = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Captured before any Assert.Ignore: OneTimeTearDown runs even when OneTimeSetUp ignores.
            m_PreviousQualityRenderPipelineAsset = QualitySettings.renderPipeline;
#if UNITY_EDITOR
            m_PreviousAllowAsyncCompilation = UnityEditor.ShaderUtil.allowAsyncCompilation;
#endif

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("These tests only run when URP is the active render pipeline.");

            // A player only has the shader variants of the rendering modes and features its URP assets use.
            AddProjectAsset(GraphicsSettings.defaultRenderPipeline);
            for (int i = 0; i < QualitySettings.count; i++)
                AddProjectAsset(QualitySettings.GetRenderPipelineAssetAt(i));

#if UNITY_EDITOR
            // Otherwise the editor draws the first frames with a placeholder shader while it compiles the variants.
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif

            // The tests compare frames with and without one light, so no other light may shade the test objects.
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (!light.enabled)
                    continue;
                light.enabled = false;
                m_DisabledLights.Add(light);
            }

            // The tests let frames pass, during which no other camera may render the renderer under test.
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (!camera.enabled)
                    continue;
                camera.enabled = false;
                m_DisabledCameras.Add(camera);
            }

            m_RendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_Asset.m_RendererDataList[0] = m_RendererData;
            QualitySettings.renderPipeline = m_Asset;
        }

        void AddProjectAsset(RenderPipelineAsset asset)
        {
            if (asset is not UniversalRenderPipelineAsset universalAsset)
                return;

            m_ProjectUsesReflectionProbeBlending |= universalAsset.ShouldUseReflectionProbeBlending();
            foreach (var rendererData in universalAsset.rendererDataList)
            {
                if (rendererData is UniversalRendererData universalRendererData)
                    m_ProjectRenderingModes.Add(universalRendererData.renderingMode);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            QualitySettings.renderPipeline = m_PreviousQualityRenderPipelineAsset;
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = m_PreviousAllowAsyncCompilation;
#endif

            foreach (var light in m_DisabledLights)
            {
                if (light != null)
                    light.enabled = true;
            }
            m_DisabledLights.Clear();

            foreach (var camera in m_DisabledCameras)
            {
                if (camera != null)
                    camera.enabled = true;
            }
            m_DisabledCameras.Clear();

            if (m_Asset != null)
                Object.DestroyImmediate(m_Asset);
            if (m_RendererData != null)
                Object.DestroyImmediate(m_RendererData);
        }

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.Lit));
            if (shader == null)
                Assert.Ignore("The Lit shader is not in this build.");

            m_Target = new RenderTexture(k_TargetSize, k_TargetSize, GraphicsFormat.R8G8B8A8_UNorm, CoreUtils.GetDefaultDepthOnlyFormat());
            m_Target.Create();
            m_Readback = new Texture2D(k_TargetSize, k_TargetSize, TextureFormat.RGBA32, false, true);
            m_TestObjects.Add(m_Readback);

            var cameraObject = CreateObject("Camera", Vector3.zero);
            m_Camera = cameraObject.AddComponent<Camera>();
            m_Camera.enabled = false; // Rendered only by the render requests of the tests.
            m_Camera.cullingMask = 1 << k_Layer;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = Color.black;
            m_Camera.aspect = 1f;
            m_Camera.fieldOfView = 60f;
            m_Camera.nearClipPlane = 0.1f;
            m_Camera.farClipPlane = 10f;
            m_Camera.allowHDR = false;
            m_Camera.allowMSAA = false;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.SetRenderer(0);
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;

            // A transparent object goes through the forward path in every rendering mode, and so reads the constant
            // buffers also with Deferred, where the opaque objects are lit by the deferred lights.
            var quad = CreateQuadMesh();
            m_OpaqueMaterial = CreateMaterial(shader, transparent: false);
            m_TransparentMaterial = CreateMaterial(shader, transparent: true);
            m_OpaqueQuad = CreateQuad("Opaque Quad", new Vector3(-0.85f, 0f, 3f), quad, m_OpaqueMaterial);
            m_TransparentQuad = CreateQuad("Transparent Quad", new Vector3(0.85f, 0f, 3f), quad, m_TransparentMaterial);

            // Between the camera and the quads, which it lights from the front.
            m_Light = CreateObject("Point Light", new Vector3(0f, 0f, 2f)).AddComponent<Light>();
            m_Light.type = LightType.Point;
            m_Light.color = Color.red;
            m_Light.intensity = 4f;
            m_Light.range = 5f;
            m_Light.shadows = LightShadows.None;
            m_Light.enabled = false;

            // Its box holds both quads.
            m_Probe = CreateObject("Reflection Probe", new Vector3(0f, 0f, 3f)).AddComponent<ReflectionProbe>();
            m_Probe.mode = ReflectionProbeMode.Custom;
            m_Probe.customBakedTexture = CreateRedCubemap();
            m_Probe.size = new Vector3(6f, 6f, 6f);
            m_Probe.boxProjection = false;
            m_Probe.enabled = false;

            m_LoggedErrors.Clear();
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            LogAssert.ignoreFailingMessages = false;

            if (m_Asset != null)
                m_Asset.reflectionProbeBlending = false;

            foreach (var obj in m_TestObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            m_TestObjects.Clear();

            if (m_Target != null)
            {
                m_Target.Release();
                Object.DestroyImmediate(m_Target);
                m_Target = null;
            }
        }

        [UnityTest]
        public IEnumerator AdditionalLightAppearsAndDisappears_EachFrameIsShadedByTheLightsItHas(
            [Values(RenderingMode.Forward, RenderingMode.ForwardPlus, RenderingMode.Deferred, RenderingMode.DeferredPlus)] RenderingMode renderingMode)
        {
            UseRenderingMode(renderingMode);

            // Two frames without additional lights, so the new renderer has no additional lights buffer yet.
            yield return Render();
            yield return Render();
            var unlit = m_Frame;

            // With persistent constant buffers, this frame creates the buffer. A light is culled from the frame it is
            // enabled in, so the render request follows it without letting a frame pass.
            m_Light.enabled = true;
            yield return Render();
            AssertChanged(unlit, m_Frame, "the first frame with the additional light");

            // The buffer stays bound and still holds the light, which the shaders must not read anymore.
            m_Light.enabled = false;
            yield return Render();
            AssertUnchanged(unlit, m_Frame, "the first frame after the additional light is gone");

            m_Light.enabled = true;
            yield return Render();
            AssertChanged(unlit, m_Frame, "the frame where the additional light comes back");
        }

        [UnityTest]
        public IEnumerator ReflectionProbeAppearsAndDisappears_EachFrameReflectsTheProbesItHas(
            [Values(RenderingMode.ForwardPlus, RenderingMode.DeferredPlus)] RenderingMode renderingMode)
        {
            if (!Application.isEditor && !m_ProjectUsesReflectionProbeBlending)
                Assert.Ignore("No URP asset of this project blends reflection probes, so the shader variants of the reflection probe atlas are not in the build.");

            // These rendering modes use the reflection probe atlas, and its constant buffer, with probe blending.
            m_Asset.reflectionProbeBlending = true;
            UseRenderingMode(renderingMode);

            // Mirrors, which show only the reflection.
            foreach (var material in new[] { m_OpaqueMaterial, m_TransparentMaterial })
            {
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);
            }

            // With persistent constant buffers, the first frame creates the buffer and uploads it empty, and the
            // second one skips the upload.
            yield return Render();
            yield return Render();
            var unreflected = m_Frame;

            // The engine adds an enabled probe to its culling data, and removes a disabled one, at its next frame update.
            m_Probe.enabled = true;
            yield return null;
            yield return Render();
            AssertChanged(unreflected, m_Frame, "the first frame with the reflection probe");

            // This frame skips the upload: the buffer still holds the probe, which the shaders must not read anymore.
            m_Probe.enabled = false;
            yield return null;
            yield return Render();
            AssertUnchanged(unreflected, m_Frame, "the first frame after the reflection probe is gone");

            m_Probe.enabled = true;
            yield return null;
            yield return Render();
            AssertChanged(unreflected, m_Frame, "the frame where the reflection probe comes back");
        }

        void UseRenderingMode(RenderingMode renderingMode)
        {
            if (!Application.isEditor && !m_ProjectRenderingModes.Contains(renderingMode))
                Assert.Ignore($"No URP asset of this project uses {renderingMode}, so its shader variants are not in the build.");

            // Setting the mode invalidates the renderer data, so the next frame creates a new renderer, without buffers.
            m_RendererData.renderingMode = renderingMode;
        }

        // Renders one frame of the test camera, and reads the color at the center of the quads back into m_Frame.
        IEnumerator Render()
        {
            // The errors are checked below, to tell a shader variant missing from the build apart.
            LogAssert.ignoreFailingMessages = true;
            RenderPipeline.SubmitRenderRequest(m_Camera, new RenderPipeline.StandardRequest { destination = m_Target });

            // ReadPixels needs a synchronous readback, which WebGPU has only in browsers with JSPI.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
            {
                var request = AsyncGPUReadback.Request(m_Target, 0, TextureFormat.RGBA32);
                while (!request.done)
                    yield return null;
                Assert.IsFalse(request.hasError, "The readback of the frame failed.");
                m_Readback.SetPixelData(request.GetData<byte>(), 0);
            }
            else
            {
                var previous = RenderTexture.active;
                RenderTexture.active = m_Target;
                m_Readback.ReadPixels(new Rect(0, 0, k_TargetSize, k_TargetSize), 0, 0, false);
                RenderTexture.active = previous;
            }

            m_Frame = (ReadCenterColor(m_OpaqueQuad), ReadCenterColor(m_TransparentQuad));

            // A player only has the shader variants its URP assets use, and draws a frame that needs another one wrong:
            // the test can't judge that frame. Any other error fails the test, as it would without this check.
            string[] errors;
            lock (m_LoggedErrors)
                errors = m_LoggedErrors.ToArray();
            foreach (var error in errors)
            {
                if (k_MissingVariant.IsMatch(error))
                    Assert.Ignore($"This build lacks a shader variant the test needs: {error}");
            }
            if (errors.Length > 0)
                Assert.Fail($"Errors were logged: {string.Join(" | ", errors)}");
        }

        void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Assert or LogType.Exception)
            {
                lock (m_LoggedErrors)
                    m_LoggedErrors.Add(message);
            }
        }

        // Mean color of the 3x3 pixels at the center of the quad.
        Color ReadCenterColor(Renderer quad)
        {
            var center = m_Camera.WorldToViewportPoint(quad.transform.position);
            int x = Mathf.FloorToInt(center.x * k_TargetSize);
            int y = Mathf.FloorToInt(center.y * k_TargetSize);
            var sum = Color.clear;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                    sum += m_Readback.GetPixel(x + dx, y + dy);
            }
            return sum / 9f;
        }

        // The light and the probe are red, so the quads they shade get redder, whatever else lights the scene.
        static float Redness(Color color) => color.r - 0.5f * (color.g + color.b);

        static float MaxDifference(Color a, Color b) => Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)));

        static void AssertChanged((Color opaque, Color transparent) before, (Color opaque, Color transparent) frame, string frameName)
        {
            Assert.That(Redness(frame.opaque) - Redness(before.opaque), Is.GreaterThan(k_MinChange),
                $"The opaque quad isn't shaded in {frameName}: {frame.opaque}, against {before.opaque} before.");
            Assert.That(Redness(frame.transparent) - Redness(before.transparent), Is.GreaterThan(k_MinChange),
                $"The transparent quad isn't shaded in {frameName}: {frame.transparent}, against {before.transparent} before.");
        }

        static void AssertUnchanged((Color opaque, Color transparent) before, (Color opaque, Color transparent) frame, string frameName)
        {
            Assert.That(MaxDifference(frame.opaque, before.opaque), Is.LessThan(k_MaxDifference),
                $"The opaque quad is still shaded in {frameName}: {frame.opaque}, against {before.opaque} before.");
            Assert.That(MaxDifference(frame.transparent, before.transparent), Is.LessThan(k_MaxDifference),
                $"The transparent quad is still shaded in {frameName}: {frame.transparent}, against {before.transparent} before.");
        }

        GameObject CreateObject(string name, Vector3 position)
        {
            var gameObject = new GameObject($"{nameof(LightAndProbeTransitionTests)} {name}") { layer = k_Layer };
            gameObject.transform.position = k_Origin + position;
            m_TestObjects.Add(gameObject);
            return gameObject;
        }

        Renderer CreateQuad(string name, Vector3 position, Mesh mesh, Material material)
        {
            var gameObject = CreateObject(name, position);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return renderer;
        }

        // A 1x1 quad facing the camera. Built here rather than with CreatePrimitive, which also adds a collider.
        Mesh CreateQuadMesh()
        {
            var mesh = new Mesh { name = "Transition Test Quad" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 3, 1, 3, 0, 2 };
            mesh.RecalculateTangents();
            m_TestObjects.Add(mesh);
            return mesh;
        }

        // White and rough, so the quads take the color of the light. Both faces are drawn, so the winding of the quad
        // doesn't matter.
        Material CreateMaterial(Shader shader, bool transparent)
        {
            var material = new Material(shader) { name = transparent ? "Transition Test Transparent" : "Transition Test Opaque" };
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            if (transparent)
            {
                // What the material inspector sets for an alpha blended surface. Alpha 1 keeps the lit color as is.
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            m_TestObjects.Add(material);
            return material;
        }

        Cubemap CreateRedCubemap()
        {
            const int size = 16;
            var cubemap = new Cubemap(size, TextureFormat.RGBA32, true) { name = "Transition Test Red Reflection" };
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.red;
            for (int face = 0; face < 6; face++)
                cubemap.SetPixels(pixels, (CubemapFace)face);
            cubemap.Apply(true, true);
            m_TestObjects.Add(cubemap);
            return cubemap;
        }
    }
}
