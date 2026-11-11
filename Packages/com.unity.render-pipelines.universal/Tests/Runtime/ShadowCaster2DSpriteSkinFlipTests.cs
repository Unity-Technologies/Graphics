#if U2D_ANIMATION_INSTALLED && UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace UnityEngine.Rendering.Universal.Tests
{
    // UUM-159819: with GPU skinning, a flipped Sprite Skin and its Shadow Caster 2D shadow drifted
    // apart while the bones were posed. The sprite shaders flipped before GPU skinning, but the
    // shadow paths skin first and then flip, and F*R(a) == R(-a)*F.
    //
    // SelfShadowSilhouette_MatchesSprite draws one GPU-skinned SpriteRenderer twice with
    // DrawRenderer, the way URP does: once with a built-in sprite shader pass, and once with the
    // "Self" caster pass of Hidden/Shadow2D (the pass ShadowRendering.RenderSpriteShadow uses).
    // The two silhouettes must cover the same pixels.
    //
    // FlippedSprite_IsMirrorOfUnflippedSprite checks that the result is also right, not only
    // consistent: a flipped, posed sprite must be the mirror image of the same sprite unflipped.
    // That is what CPU skinning and the CPU-outline projected shadow produce.
    class ShadowCaster2DSpriteSkinFlipTests : IPrebuildSetup, IPostBuildCleanup
    {
        public enum SpriteShader
        {
            Unlit,
            Lit,
        }

        public struct Case
        {
            public SpriteShader shader;
            public bool flipX;
            public bool flipY;
            public float boneAngle;

            public override string ToString() => $"{shader},flipX={flipX},flipY={flipY},bone={boneAngle}";
        }

        static readonly Case[] k_ShadowCases =
        {
            new Case { shader = SpriteShader.Unlit, flipX = false, flipY = false, boneAngle = 30f },
            new Case { shader = SpriteShader.Unlit, flipX = true,  flipY = false, boneAngle = 0f },
            new Case { shader = SpriteShader.Unlit, flipX = true,  flipY = false, boneAngle = 30f },
            new Case { shader = SpriteShader.Unlit, flipX = false, flipY = true,  boneAngle = 30f },
            new Case { shader = SpriteShader.Unlit, flipX = true,  flipY = true,  boneAngle = 30f },
            new Case { shader = SpriteShader.Unlit, flipX = true,  flipY = false, boneAngle = -45f },
            new Case { shader = SpriteShader.Lit,   flipX = false, flipY = false, boneAngle = 30f },
            new Case { shader = SpriteShader.Lit,   flipX = true,  flipY = false, boneAngle = 30f },
            new Case { shader = SpriteShader.Lit,   flipX = false, flipY = true,  boneAngle = 30f },
        };

        static readonly Case[] k_MirrorCases =
        {
            new Case { shader = SpriteShader.Unlit, flipX = true,  flipY = false, boneAngle = 30f },
            new Case { shader = SpriteShader.Unlit, flipX = false, flipY = true,  boneAngle = 30f },
            new Case { shader = SpriteShader.Unlit, flipX = true,  flipY = true,  boneAngle = 30f },
            new Case { shader = SpriteShader.Lit,   flipX = true,  flipY = false, boneAngle = 30f },
        };

        const int k_Size = 128;
        const int k_MaxFrames = 30;
        const float k_MaxMismatch = 0.02f;
        const string k_SkinnedSpriteKeyword = "SKINNED_SPRITE";
        const string k_SpriteCasterKeyword = "SHADOW_SPRITE_CASTER";
        const string k_PrevMeshDeformationKey = "UUM-159819.PrevMeshDeformation";

        RenderPipelineAsset m_PrevDefaultPipeline;
        RenderPipelineAsset m_PrevQualityPipeline;
        Renderer2DData m_RendererData;
        UniversalRenderPipelineAsset m_PipelineAsset;
        GameObject m_CameraGO;
        Camera m_Camera;
        GameObject m_SpriteGO;
        SpriteRenderer m_Renderer;
        Texture2D m_Texture;
        Sprite m_Sprite;
        Material m_SpriteMaterial;
        int m_SpritePass;
        Material m_ShadowMaterial;
        int m_SelfPass;
        RenderTexture m_CameraRT;
        RenderTexture m_SpriteRT;
        RenderTexture m_ShadowRT;
        bool m_CaptureRequested;
        bool m_Captured;

        // GPU skinning is a player setting. Set it before the test enters Play Mode.
        public void Setup()
        {
            UnityEditor.SessionState.SetInt(k_PrevMeshDeformationKey, (int)UnityEditor.PlayerSettings.meshDeformation);
            UnityEditor.PlayerSettings.meshDeformation = UnityEditor.MeshDeformation.GPU;
        }

        public void Cleanup()
        {
            int prev = UnityEditor.SessionState.GetInt(k_PrevMeshDeformationKey, -1);
            if (prev >= 0)
                UnityEditor.PlayerSettings.meshDeformation = (UnityEditor.MeshDeformation)prev;
        }

        [SetUp]
        public void SetUpScene()
        {
            m_PrevDefaultPipeline = GraphicsSettings.defaultRenderPipeline;
            m_PrevQualityPipeline = QualitySettings.renderPipeline;

            m_RendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            m_PipelineAsset = UniversalRenderPipelineAsset.Create(m_RendererData);
            m_PipelineAsset.useSRPBatcher = true;
            GraphicsSettings.defaultRenderPipeline = m_PipelineAsset;
            QualitySettings.renderPipeline = null;

            m_CameraRT = new RenderTexture(k_Size, k_Size, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None);
            m_SpriteRT = new RenderTexture(k_Size, k_Size, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None);
            m_ShadowRT = new RenderTexture(k_Size, k_Size, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None);

            m_CameraGO = new GameObject("UUM-159819 Camera");
            m_CameraGO.transform.position = new Vector3(0, 0, -10);
            m_Camera = m_CameraGO.AddComponent<Camera>();
            m_Camera.orthographic = true;
            m_Camera.orthographicSize = 2f;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = Color.clear;
            m_Camera.targetTexture = m_CameraRT;

            var shadowShader = Shader.Find("Hidden/Shadow2D");
            Assert.IsNotNull(shadowShader, "Hidden/Shadow2D not found.");
            m_ShadowMaterial = new Material(shadowShader);
            m_ShadowMaterial.EnableKeyword(k_SpriteCasterKeyword);
            m_ShadowMaterial.EnableKeyword(k_SkinnedSpriteKeyword);
            m_SelfPass = m_ShadowMaterial.FindPass("Self");

            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        [TearDown]
        public void TearDownScene()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            Object.DestroyImmediate(m_SpriteGO);
            Object.DestroyImmediate(m_CameraGO);
            Object.DestroyImmediate(m_Sprite);
            Object.DestroyImmediate(m_Texture);
            Object.DestroyImmediate(m_SpriteMaterial);
            Object.DestroyImmediate(m_ShadowMaterial);
            foreach (var rt in new[] { m_CameraRT, m_SpriteRT, m_ShadowRT })
            {
                if (rt == null)
                    continue;
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            GraphicsSettings.defaultRenderPipeline = m_PrevDefaultPipeline;
            QualitySettings.renderPipeline = m_PrevQualityPipeline;
            Object.DestroyImmediate(m_PipelineAsset);
            Object.DestroyImmediate(m_RendererData);
        }

        // Unlit: pass 0 of Sprite-Unlit-Default. Lit: the UniversalForward pass of Sprite-Lit-Default,
        // which shares the skin-then-flip vertex code with the other Lit passes but does not need
        // 2D light textures, so its output is the plain sprite color.
        void CreateSpriteMaterial(SpriteShader shader)
        {
            if (shader == SpriteShader.Unlit)
            {
                m_SpriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                m_SpritePass = 0;
                return;
            }

            m_SpriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            m_SpritePass = -1;
            var lightMode = new ShaderTagId("LightMode");
            for (int i = 0; i < m_SpriteMaterial.passCount; ++i)
            {
                if (m_SpriteMaterial.shader.FindPassTagValue(i, lightMode).name == "UniversalForward")
                {
                    m_SpritePass = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(m_SpritePass, 0, "Sprite-Lit-Default has no UniversalForward pass.");
        }

        // A 1x2 unit white quad with one bone at its centre. Every vertex is fully weighted to it.
        void CreateSkinnedSprite(Case c)
        {
            CreateSpriteMaterial(c.shader);

            m_Texture = new Texture2D(32, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[32 * 64];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = new Color32(255, 255, 255, 255);
            m_Texture.SetPixels32(pixels);
            m_Texture.Apply();

            m_Sprite = Sprite.Create(m_Texture, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
            m_Sprite.SetBones(new[]
            {
                new SpriteBone { name = "bone", position = Vector3.zero, rotation = Quaternion.identity, length = 1f, parentId = -1 }
            });

            using (var bindPoses = new NativeArray<Matrix4x4>(1, Allocator.Temp))
            {
                var poses = bindPoses;
                poses[0] = Matrix4x4.identity;
                m_Sprite.SetBindPoses(poses);
            }

            int vertexCount = m_Sprite.GetVertexCount();
            using (var weights = new NativeArray<BoneWeight>(vertexCount, Allocator.Temp))
            {
                var w = weights;
                for (int i = 0; i < vertexCount; ++i)
                    w[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1f };
                m_Sprite.SetVertexAttribute(VertexAttribute.BlendWeight, w);
            }

            m_SpriteGO = new GameObject("UUM-159819 Sprite");
            m_Renderer = m_SpriteGO.AddComponent<SpriteRenderer>();
            m_Renderer.sprite = m_Sprite;
            m_Renderer.sharedMaterial = m_SpriteMaterial;
            m_Renderer.flipX = c.flipX;
            m_Renderer.flipY = c.flipY;

            var bone = new GameObject("bone").transform;
            bone.SetParent(m_SpriteGO.transform, false);
            bone.localRotation = Quaternion.Euler(0, 0, c.boneAngle);

            var skin = m_SpriteGO.AddComponent<SpriteSkin>();
            skin.alwaysUpdate = true;
            skin.SetRootTransform(m_SpriteGO.transform);
            var state = skin.SetBoneTransforms(new[] { bone });
            Assert.AreEqual(SpriteSkinState.Ready, state, "SpriteSkin setup failed.");
        }

        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != m_Camera || !m_CaptureRequested)
                return;
            m_CaptureRequested = false;

            var cmd = new CommandBuffer { name = "UUM-159819 capture" };
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            cmd.SetRenderTarget(m_SpriteRT);
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.DrawRenderer(m_Renderer, m_Renderer.sharedMaterial, 0, m_SpritePass);

            cmd.SetRenderTarget(m_ShadowRT);
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.DrawRenderer(m_Renderer, m_ShadowMaterial, 0, m_SelfPass);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            cmd.Release();
            m_Captured = true;
        }

        static bool[] ReadMask(RenderTexture rt)
        {
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var pixels = tex.GetPixels32();
            Object.DestroyImmediate(tex);

            var mask = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; ++i)
                mask[i] = pixels[i].r > 127;
            return mask;
        }

        static int Count(bool[] mask)
        {
            int n = 0;
            foreach (var m in mask)
                if (m) ++n;
            return n;
        }

        // Mismatched pixels divided by pixels covered by either mask.
        static float MismatchRatio(bool[] a, bool[] b, out int mismatch, out int union)
        {
            mismatch = 0;
            union = 0;
            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] || b[i]) ++union;
                if (a[i] != b[i]) ++mismatch;
            }
            return union == 0 ? 0f : (float)mismatch / union;
        }

        // The sprite pivot is at the world origin, which is the centre of the render target, so a
        // flip about the pivot maps pixel (x, y) to (k_Size-1-x, y) for X and (x, k_Size-1-y) for Y.
        static bool[] Mirror(bool[] mask, bool flipX, bool flipY)
        {
            var result = new bool[mask.Length];
            for (int y = 0; y < k_Size; ++y)
            {
                for (int x = 0; x < k_Size; ++x)
                {
                    int sx = flipX ? k_Size - 1 - x : x;
                    int sy = flipY ? k_Size - 1 - y : y;
                    result[y * k_Size + x] = mask[sy * k_Size + sx];
                }
            }
            return result;
        }

        void AssertPrerequisites()
        {
            Assume.That(SystemInfo.supportsComputeShaders, "GPU skinning needs compute shaders.");
            Assert.GreaterOrEqual(m_SelfPass, 0, "Hidden/Shadow2D has no Self pass.");
            Assert.AreEqual(UnityEditor.MeshDeformation.GPU, UnityEditor.PlayerSettings.meshDeformation, "GPU skinning is off.");
        }

        // Let 2D Animation move the Sprite Skin to its GPU deformation system. That needs the
        // renderer's SRP-batcher state, which is only known after it has been rendered.
        IEnumerator WaitForGpuDeformation()
        {
            for (int frame = 0; frame < k_MaxFrames; ++frame)
            {
                m_Camera.Render();
                yield return null;
                if (frame >= 5 && m_SpriteMaterial.IsKeywordEnabled(k_SkinnedSpriteKeyword))
                    break;
            }
            Assert.IsTrue(m_SpriteMaterial.IsKeywordEnabled(k_SkinnedSpriteKeyword),
                "The Sprite Skin never switched to GPU deformation (SKINNED_SPRITE is off on its material).");
        }

        void Capture()
        {
            m_Captured = false;
            m_CaptureRequested = true;
            m_Camera.Render();
            Assert.IsTrue(m_Captured, "endCameraRendering capture did not run.");
        }

        [UnityTest]
        public IEnumerator SelfShadowSilhouette_MatchesSprite_ForGpuSkinnedSprite([ValueSource(nameof(k_ShadowCases))] Case c)
        {
            AssertPrerequisites();
            CreateSkinnedSprite(c);
            yield return WaitForGpuDeformation();

            Capture();
            var spriteMask = ReadMask(m_SpriteRT);
            var shadowMask = ReadMask(m_ShadowRT);

            int spriteCount = Count(spriteMask);
            int shadowCount = Count(shadowMask);
            Assert.Greater(spriteCount, 100, "The sprite drew almost nothing.");
            Assert.Greater(shadowCount, 100, "The shadow caster Self pass drew almost nothing.");

            float ratio = MismatchRatio(spriteMask, shadowMask, out int mismatch, out int union);
            Assert.LessOrEqual(ratio, k_MaxMismatch,
                $"{c}: the Shadow Caster 2D silhouette does not match the GPU-skinned sprite " +
                $"({mismatch} of {union} pixels differ, sprite={spriteCount}, shadow={shadowCount}). " +
                "The sprite shader and Shadow2DCasterVertex.hlsl apply flip and GPU skinning in a different order.");
        }

        [UnityTest]
        public IEnumerator FlippedSprite_IsMirrorOfUnflippedSprite_ForGpuSkinnedSprite([ValueSource(nameof(k_MirrorCases))] Case c)
        {
            AssertPrerequisites();

            // Start unflipped, capture, then flip the same renderer and capture again.
            var unflipped = c;
            unflipped.flipX = false;
            unflipped.flipY = false;
            CreateSkinnedSprite(unflipped);
            yield return WaitForGpuDeformation();

            Capture();
            var unflippedSprite = ReadMask(m_SpriteRT);
            var unflippedShadow = ReadMask(m_ShadowRT);
            Assert.Greater(Count(unflippedSprite), 100, "The unflipped sprite drew almost nothing.");

            m_Renderer.flipX = c.flipX;
            m_Renderer.flipY = c.flipY;
            m_Camera.Render();
            yield return null;
            Assert.IsTrue(m_SpriteMaterial.IsKeywordEnabled(k_SkinnedSpriteKeyword),
                "The Sprite Skin left GPU deformation after the flip changed.");

            Capture();
            var flippedSprite = ReadMask(m_SpriteRT);
            var flippedShadow = ReadMask(m_ShadowRT);

            var expected = Mirror(unflippedSprite, c.flipX, c.flipY);
            float spriteRatio = MismatchRatio(expected, flippedSprite, out int spriteMismatch, out int spriteUnion);
            Assert.LessOrEqual(spriteRatio, k_MaxMismatch,
                $"{c}: the flipped GPU-skinned sprite is not the mirror image of the unflipped one " +
                $"({spriteMismatch} of {spriteUnion} pixels differ). Skinning must run before the flip.");

            var expectedShadow = Mirror(unflippedShadow, c.flipX, c.flipY);
            float shadowRatio = MismatchRatio(expectedShadow, flippedShadow, out int shadowMismatch, out int shadowUnion);
            Assert.LessOrEqual(shadowRatio, k_MaxMismatch,
                $"{c}: the flipped shadow caster silhouette is not the mirror image of the unflipped one " +
                $"({shadowMismatch} of {shadowUnion} pixels differ).");
        }
    }
}
#endif
