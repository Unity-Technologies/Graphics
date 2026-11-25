using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.SceneManagement;
using UnityEditor.ShaderGraph;

namespace UnityEngine.Rendering.Universal.Tests
{
    // UUM-161407: a light using the untouched default Light2D Shader Graph must be as bright as the
    // same light drawn with the built-in Light2D shader when HDR Emulation Scale is above 1. With the
    // Alpha Blend overlap (SrcAlpha, OneMinusSrcAlpha), any scaling of the light's alpha darkens it.
#if ENABLE_CORECLR
    [Explicit("MultiJson-serialized ShaderGraph written to disk fails to reimport on CoreCLR (JSON parse error: The document root must not follow by other values), so no Shader or Material sub-assets are produced, see https://jira.unity3d.com/browse/UUM-150436")]
#endif
    class Light2DShaderGraphHdrEmulationTests
    {
        const string k_Folder = "Assets/__Light2DShaderGraphHdrEmulationTests";
        const string k_GraphPath = k_Folder + "/DefaultLight2D.shadergraph";
        const string k_RendererPath = k_Folder + "/Renderer2D.asset";
        const string k_PipelinePath = k_Folder + "/URP2D.asset";
        const int k_Size = 300;

        Material m_GraphMaterial;
        RenderPipelineAsset m_OriginalDefaultPipeline;
        RenderPipelineAsset m_OriginalQualityPipeline;
        bool m_OriginalAllowAsyncCompilation;
        RenderTexture m_Target;
        readonly List<Object> m_CreatedObjects = new List<Object>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets", k_Folder.Substring("Assets/".Length));
            m_GraphMaterial = CreateDefaultLight2DGraphMaterial();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(k_Folder);
        }

        [SetUp]
        public void SetUp()
        {
            m_OriginalDefaultPipeline = GraphicsSettings.defaultRenderPipeline;
            m_OriginalQualityPipeline = QualitySettings.renderPipeline;
            m_OriginalAllowAsyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_OriginalDefaultPipeline;
            QualitySettings.renderPipeline = m_OriginalQualityPipeline;
            ShaderUtil.allowAsyncCompilation = m_OriginalAllowAsyncCompilation;
            if (m_Target != null)
            {
                RenderTexture.active = null;
                foreach (var cam in Object.FindObjectsByType<Camera>())
                    cam.targetTexture = null;
                m_Target.Release();
                Object.DestroyImmediate(m_Target);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            foreach (var obj in m_CreatedObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            m_CreatedObjects.Clear();
            AssetDatabase.DeleteAsset(k_PipelinePath);
            AssetDatabase.DeleteAsset(k_RendererPath);
        }

        T Track<T>(T obj) where T : Object
        {
            m_CreatedObjects.Add(obj);
            return obj;
        }

        static Material CreateDefaultLight2DGraphMaterial()
        {
            // Same graph the "Light2D Shader Graph" Create menu writes.
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalLight2DSubTarget)));
            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { target }, UniversalLight2DSubTarget.kActiveBlocks);
            graph.OnEnable();
            graph.ValidateGraph();

            FileUtilities.WriteShaderGraphToDisk(k_GraphPath, graph);
            AssetDatabase.ImportAsset(k_GraphPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var assets = AssetDatabase.LoadAllAssetsAtPath(k_GraphPath);
            var shader = assets.OfType<Shader>().FirstOrDefault();
            Assert.IsNotNull(shader, "The default Light2D Shader Graph produced no shader.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "The default Light2D Shader Graph has compile errors.");
            // The Material sub-asset the importer adds beside the shader, as a user would assign it.
            var material = assets.OfType<Material>().FirstOrDefault();
            Assert.IsNotNull(material, "The default Light2D Shader Graph produced no Material sub-asset.");
            return material;
        }

        static Renderer2DData CreatePipeline(float hdrEmulationScale)
        {
            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(rendererData, k_RendererPath);
            var so = new SerializedObject(rendererData);
            so.FindProperty("m_HDREmulationScale").floatValue = hdrEmulationScale;
            so.ApplyModifiedPropertiesWithoutUndo();

            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, k_PipelinePath);
            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            return rendererData;
        }

        static Light2D CreateLight(Light2D.LightType type, float x, Sprite cookie, Material material)
        {
            var go = new GameObject(type + (material != null ? "_Graph" : "_BuiltIn"));
            go.transform.position = new Vector3(x, 0, 0);
            go.transform.localScale = type == Light2D.LightType.Sprite ? new Vector3(0.5f, 0.5f, 1) : new Vector3(1.5f, 1.5f, 1);
            var light = go.AddComponent<Light2D>();
            light.lightType = type;
            light.intensity = 0.5f;
            light.color = Color.white;
            light.blendStyleIndex = 0;
            light.overlapOperation = Light2D.OverlapOperation.AlphaBlend;
            if (type == Light2D.LightType.Sprite)
                light.lightCookieSprite = cookie;
            light.material = material;
            // LateUpdate does not run in batch mode; build the light mesh directly.
            light.UpdateMesh(true);
            return light;
        }

        Sprite WhiteSprite()
        {
            var tex = Track(new Texture2D(4, 4));
            tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            tex.Apply();
            return Track(Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1));
        }

        static float BrightestGreen(Texture2D tex, int cx, int cy, int radius)
        {
            float best = 0;
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    best = Mathf.Max(best, tex.GetPixel(x, y).g);
            return best;
        }

        // Freeform and Sprite are the reported cases. Point lights already matched (built-in
        // frag_point scales RGBA too) and must keep matching. Scale 1 is the control.
        [TestCase(Light2D.LightType.Freeform, 2f)]
        [TestCase(Light2D.LightType.Sprite, 2f)]
        [TestCase(Light2D.LightType.Freeform, 4f)]
        [TestCase(Light2D.LightType.Sprite, 4f)]
        [TestCase(Light2D.LightType.Point, 2f)]
        [TestCase(Light2D.LightType.Freeform, 1f)]
        public void DefaultLight2DShaderGraph_MatchesBuiltInBrightness_AtHdrEmulationScale(Light2D.LightType type, float hdrEmulationScale)
        {
            var rendererData = CreatePipeline(hdrEmulationScale);
            Assert.AreEqual(hdrEmulationScale, rendererData.hdrEmulationScale);

            // Orthographic size 3 over 300 px: 50 px per unit, origin at the center.
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            // A white 8x8-unit lit sprite fills the view, so each pixel shows the light that reaches it.
            var sprite = WhiteSprite();
            var background = new GameObject("Background").AddComponent<SpriteRenderer>();
            background.sprite = sprite;
            background.sharedMaterial = Track(new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")));
            background.transform.localScale = new Vector3(2, 2, 1);

            // Built-in light on the left, graph light on the right. Each light, falloff included,
            // stays within 1.5 units (75 px) of its center, so the two never overlap.
            CreateLight(type, -1.5f, sprite, null);
            CreateLight(type, 1.5f, sprite, m_GraphMaterial);

            m_Target = new RenderTexture(k_Size, k_Size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = m_Target;
            for (int i = 0; i < 3; i++)
                cam.Render();

            RenderTexture.active = m_Target;
            var read = Track(new Texture2D(k_Size, k_Size, TextureFormat.RGBA32, false));
            read.ReadPixels(new Rect(0, 0, k_Size, k_Size), 0, 0);
            read.Apply();
            RenderTexture.active = null;

            float builtIn = BrightestGreen(read, k_Size / 2 - 75, k_Size / 2, 70);
            float graph = BrightestGreen(read, k_Size / 2 + 75, k_Size / 2, 70);

            Assert.Greater(builtIn, 0.05f, "The built-in light did not light the sprite; the scene did not render.");
            Assert.AreEqual(builtIn, graph, 2f / 255f,
                $"{type} light: the default Light2D Shader Graph rendered {graph * 255:F0}/255 but the built-in light rendered {builtIn * 255:F0}/255 at HDR Emulation Scale {hdrEmulationScale}.");
        }
    }
}
