using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Unity.Testing.VisualEffectGraph;
using UnityEngine.TestTools;
using UnityEngine.Rendering;

#if VFX_HAS_TIMELINE
using UnityEngine.Playables;
#endif

namespace UnityEngine.VFX.Test
{
    [TestFixture]
    [PrebuildSetup("SetupGraphicsTestCases")]
    public class VFXRuntimeTests
    {
        AssetBundle m_AssetBundle;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_AssetBundle = AssetBundleHelper.Load("scene_in_assetbundle");
        }

        [UnityTest, Description("Cover UUM-20944")]
        public IEnumerator Indirect_Mesh_Rendering_With_Null_IndexBuffer()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Packages/com.unity.testing.visualeffectgraph/Scenes/022_Repro_Crash_Null_Indexbuffer.unity");
            yield return null;

            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
            Assert.AreEqual(1u, vfxComponents.Length);
            var currentVFX = vfxComponents[0];

            var meshID = Shader.PropertyToID("Mesh");
            Assert.IsTrue(currentVFX.HasMesh(meshID));

            int maxFrame = 32;
            while (currentVFX.aliveParticleCount == 0 && maxFrame-- > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 1, 0)
                }
            };
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new Rendering.SubMeshDescriptor { vertexCount = 3 }, Rendering.MeshUpdateFlags.DontRecalculateBounds);

            currentVFX.SetMesh(meshID, mesh);
            maxFrame = 8;
            while (maxFrame-- > 0)
            {
                //The crash was in this case
                yield return null;
            }
        }

        [UnityTest, Description("Cover Prefab instanciation behavior")
#if UNITY_EDITOR
            , Ignore("See UUM-27159, Load Scene in playmode creates a real VisualEffect instance.")
#endif
        ]
        public IEnumerator Prefab_Instanciation()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Packages/com.unity.testing.visualeffectgraph/Scenes/020_PrefabInstanciation.unity");
            yield return null;

            var references = Resources.FindObjectsOfTypeAll<VFXPrefabReferenceTest>();
            Assert.AreEqual(1u, references.Length);
            var reference = references[0];

            for (int i = 0; i <= 6; ++i)
            {
                var batchEffectInfos = UnityEngine.VFX.VFXManager.GetBatchedEffectInfo(reference.VfxReference);
                Assert.AreEqual(i, batchEffectInfos.activeInstanceCount);
                if (i > 0)
                {
                    Assert.AreEqual(1u, batchEffectInfos.activeBatchCount);

                    var batchInfo = UnityEngine.VFX.VFXManager.GetBatchInfo(reference.VfxReference, 0);
                    Assert.AreEqual(i, batchInfo.activeInstanceCount);

                    Assert.IsFalse(batchInfo.capacity < 6);
                }

                if (i < 6)
                {
                    reference.PrefabReference.GetComponent<VisualEffect>().SetFloat("hue", (float) i / 6.0f);
                    var newVFX = GameObject.Instantiate(reference.PrefabReference);
                    newVFX.transform.eulerAngles = new Vector3(0, 0, 60 * i);
                }

                int frameIndex = Time.frameCount + 1;
                while (Time.frameCount <= frameIndex)
                    yield return null;
            }
        }

        [UnityTest, Description("Cover behavior from UUM-29663, This test is checking if the root material of prefab variant is correctly skipped & also clean up exposed variables")]
        public IEnumerator Cross_Material_Variant_Check_Content()
        {
            var cross_vfx_asset = AssetBundleHelper.Load("cross_vfx_in_bundle");
            var all = cross_vfx_asset.LoadAssetWithSubAssets("Packages/com.unity.testing.visualeffectgraph/Scenes/CrossPipeline_MaterialOverride.vfx");

            var allMaterials = all.OfType<Material>().ToArray();
            var allComputes = all.OfType<ComputeShader>().ToArray();
            var allShaders = all.OfType<Shader>().ToArray();
            var allVFX = all.OfType<VisualEffectAsset>().ToArray();

            Assert.AreEqual(1, allVFX.Length);
            Assert.AreEqual(1, allShaders.Length); //One output
            Assert.AreEqual(3, allComputes.Length); //Init/Update/OutputUpdate
            Assert.AreEqual(1, allMaterials.Length); //_Parent material is strip off & actual
            Assert.AreEqual(6, all.Length);

            var parentMaterial = allMaterials.FirstOrDefault(o => o.name.EndsWith("_Parent"));
            var actualMaterial = allMaterials.FirstOrDefault(o => !o.name.EndsWith("_Parent"));
            Assert.IsNull(parentMaterial);
            Assert.IsNotNull(actualMaterial);

#if UNITY_EDITOR
            //Check if material variant collapsing went fine
            Assert.IsFalse(actualMaterial.isVariant);
#endif

            //Check expected strip of properties in runtime for current SRP (parentMaterial isn't used)
            var hasSurfaceType = actualMaterial.HasFloat("_SurfaceType");
            var hasSurface = actualMaterial.HasFloat("_Surface");
            var hasBlend = actualMaterial.HasFloat("_Blend");
            var hasBlendMode = actualMaterial.HasFloat("_BlendMode");

#if VFX_TESTS_HAS_HDRP && VFX_TESTS_HAS_URP
            Assert.Fail("This suite doesn't support both pipeline yet.");
#endif

#if VFX_TESTS_HAS_HDRP
            Assert.IsTrue(hasSurfaceType);
            Assert.IsFalse(hasSurface);
            Assert.AreEqual(1.0f, actualMaterial.GetFloat("_SurfaceType"));
            Assert.IsFalse(hasBlend);
            Assert.IsTrue(hasBlendMode);
            Assert.AreEqual(0.0f, actualMaterial.GetFloat("_BlendMode"));

            Assert.AreEqual((float)Rendering.BlendMode.One, actualMaterial.GetFloat("_AlphaSrcBlend"));
            Assert.AreEqual((float)Rendering.BlendMode.One, actualMaterial.GetFloat("_SrcBlend")); //N.B.: Conflict with URP
            Assert.AreEqual((float)Rendering.BlendMode.OneMinusSrcAlpha, actualMaterial.GetFloat("_AlphaDstBlend"));
            Assert.AreEqual((float)Rendering.BlendMode.OneMinusSrcAlpha, actualMaterial.GetFloat("_DstBlend"));
#endif

#if VFX_TESTS_HAS_URP
            Assert.IsFalse(hasSurfaceType);
            Assert.IsTrue(hasSurface);
            Assert.AreEqual(1.0f, actualMaterial.GetFloat("_Surface"));
            Assert.IsTrue(hasBlend);
            Assert.IsFalse(hasBlendMode);
            Assert.AreEqual(0.0f, actualMaterial.GetFloat("_Blend"));

            //URP doesn't use alpha independent blending mode with SG integration
            Assert.IsFalse(actualMaterial.HasFloat("_SrcBlendAlpha"));
            Assert.IsFalse(actualMaterial.HasFloat("_DstBlendAlpha"));
            //Only rely on Blend [_SrcBlend] [_DstBlend]
            Assert.AreEqual((float)Rendering.BlendMode.SrcAlpha, actualMaterial.GetFloat("_SrcBlend")); //N.B.: Conflict with HDRP
            Assert.AreEqual((float)Rendering.BlendMode.OneMinusSrcAlpha, actualMaterial.GetFloat("_DstBlend"));
#endif
            yield return null;
            AssetBundleHelper.Unload(cross_vfx_asset);
        }

#if VFX_HAS_TIMELINE
        private static readonly string kTimeline_Off = "Timeline_Off";
        private static readonly string kIn = "In";
        private static readonly string kMiddle = "Middle";
        private static readonly string kOut = "Out";

        private static readonly int kTimeline_OffID = Shader.PropertyToID(kTimeline_Off);
        private static readonly int kInID = Shader.PropertyToID(kIn);
        private static readonly int kMiddleID = Shader.PropertyToID(kMiddle);
        private static readonly int kOutID = Shader.PropertyToID(kOut);

        private static int m_FrameOffset;
        private static Queue<(char source, int frame, string evt)> m_ReceivedEvents = new();
        private static void OnOutputEventReceived(char source, VFXOutputEventArgs args)
        {
            var frameIndex = Time.frameCount - m_FrameOffset;

            var evtName = string.Empty;
            if (args.nameId == kTimeline_OffID)
                evtName = kTimeline_Off;
            else if (args.nameId == kInID)
                evtName = kIn;
            else if (args.nameId == kMiddleID)
                evtName = kMiddle;
            else if (args.nameId == kOutID)
                evtName = kOut;
            else throw new NotImplementedException();
            m_ReceivedEvents.Enqueue((source, frameIndex, evtName));
        }


        string PrintTimelineStack(Queue<(char source, int frame, string evt)> queue)
        {
            var stringBuilders = new SortedDictionary<char, StringBuilder>();
            while (queue.Count > 0)
            {
                var entry = queue.Dequeue();
                if (!stringBuilders.TryGetValue(entry.source, out var stringBuilder))
                {
                    stringBuilder = new StringBuilder();
                    stringBuilders.Add(entry.source, stringBuilder);
                }
                stringBuilder.AppendFormat("({0}, {1});", entry.frame, entry.evt);
            }

            var final = new StringBuilder();
            foreach (var stringBuilder in stringBuilders)
            {
                final.AppendFormat(stringBuilder.ToString());
                final.AppendLine();
            }

            return final.ToString();

        }

        public struct Timeline_Exact_Frame_Case
        {
            public int startFrame;
            public bool pause;
            public Queue<(char source, int frame, string evt)> expectedQueue;
            public override string ToString()
            {
                return "Case_" + startFrame + (pause ? "_Paused" : String.Empty);
            }
        }

        private static Timeline_Exact_Frame_Case[] kTimeline_Exact_Frame_Matching_Cases = new[]
        {
            //N.B: A/B/C are equivalent with D/E/F, first are with scrubbing, second is disabled
            new Timeline_Exact_Frame_Case()
            {
                startFrame = 0,
                pause = false,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('A', 60, kMiddle),
                    ('A', 120, kOut),
                    ('B', 60, kIn),
                    ('B', 120, kMiddle),
                    ('B', 180, kOut),
                    ('C', 1, kIn),
                    ('C', 61, kMiddle),
                    ('C', 121, kOut),

                    ('D', 0, kIn),
                    ('D', 60, kMiddle),
                    ('D', 120, kOut),
                    ('E', 60, kIn),
                    ('E', 120, kMiddle),
                    ('E', 180, kOut),
                    ('F', 1, kIn),
                    ('F', 61, kMiddle),
                    ('F', 121, kOut),
                })
            },

            new Timeline_Exact_Frame_Case()
            {
                startFrame = 1,
                pause = false,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('A', 59, kMiddle),
                    ('A', 119, kOut),
                    ('B', 59, kIn),
                    ('B', 119, kMiddle),
                    ('B', 179, kOut),
                    ('C', 0, kIn),
                    ('C', 60, kMiddle),
                    ('C', 120, kOut),

                    ('D', 0, kIn),
                    ('D', 59, kMiddle),
                    ('D', 119, kOut),
                    ('E', 59, kIn),
                    ('E', 119, kMiddle),
                    ('E', 179, kOut),
                    ('F', 0, kIn),
                    ('F', 60, kMiddle),
                    ('F', 120, kOut),
                })
            },

            new Timeline_Exact_Frame_Case()
            {
                startFrame = 59,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('A', 1, kMiddle),
                    ('A', 61, kOut),
                    ('B', 1, kIn),
                    ('B', 61, kMiddle),
                    ('B', 121, kOut),
                    ('C', 0, kIn),
                    ('C', 2, kMiddle),
                    ('C', 62, kOut),

                    ('D', 0, kIn),
                    ('D', 1, kMiddle),
                    ('D', 61, kOut),
                    ('E', 1, kIn),
                    ('E', 61, kMiddle),
                    ('E', 121, kOut),
                    ('F', 0, kIn),
                    ('F', 2, kMiddle),
                    ('F', 62, kOut),
                })
            },

            new Timeline_Exact_Frame_Case()
            {
                startFrame = 60,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('A', 0, kMiddle),
                    ('A', 60, kOut),
                    ('B', 0, kIn),
                    ('B', 60, kMiddle),
                    ('B', 120, kOut),
                    ('C', 0, kIn),
                    ('C', 1, kMiddle),
                    ('C', 61, kOut),

                    ('D', 0, kIn),
                    ('D', 0, kMiddle),
                    ('D', 60, kOut),
                    ('E', 0, kIn),
                    ('E', 60, kMiddle),
                    ('E', 120, kOut),
                    ('F', 0, kIn),
                    ('F', 1, kMiddle),
                    ('F', 61, kOut),
                })
            },

            //The paused cases simulates the holding status in timeline
            new Timeline_Exact_Frame_Case()
            {
                pause = true,
                startFrame = 0,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('D', 0, kIn),
                })
            },

            new Timeline_Exact_Frame_Case()
            {
                pause = true,
                startFrame = 1,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('C', 0, kIn),
                    ('D', 0, kIn),
                    ('F', 0, kIn),
                })
            },

            new Timeline_Exact_Frame_Case()
            {
                pause = true,
                startFrame = 59,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('C', 0, kIn),
                    ('D', 0, kIn),
                    ('F', 0, kIn),
                })
            },

            new Timeline_Exact_Frame_Case()
            {
                pause = true,
                startFrame = 60,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('B', 0, kIn),
                    ('A', 0, kMiddle),
                    ('C', 0, kIn),
                    ('D', 0, kIn),
                    ('D', 0, kMiddle),
                    ('E', 0, kIn),
                    ('F', 0, kIn),
                })
            },

            new Timeline_Exact_Frame_Case()
            {
                pause = true,
                startFrame = 61,
                expectedQueue = new Queue<(char source, int frame, string evt)>(new[]
                {
                    ('A', 0, kIn),
                    ('B', 0, kIn),
                    ('A', 0, kMiddle),
                    ('C', 0, kIn),
                    ('C', 0, kMiddle),
                    ('D', 0, kIn),
                    ('D', 0, kMiddle),
                    ('E', 0, kIn),
                    ('F', 0, kIn),
                    ('F', 0, kMiddle),
                })
            },

        };

        [UnityTest, Description("Cover unexpected behavior UUM-42283")]
        public IEnumerator Timeline_Exact_Frame_Matching([ValueSource("kTimeline_Exact_Frame_Matching_Cases")] Timeline_Exact_Frame_Case timelineCase)
        {
            var kScenePath = "Packages/com.unity.testing.visualeffectgraph/Scenes/Timeline_FirstFrame.unity";
            UnityEngine.SceneManagement.SceneManager.LoadScene(kScenePath);
            yield return null;

            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
            var directors = Resources.FindObjectsOfTypeAll<PlayableDirector>();
            Assert.AreEqual(6, vfxComponents.Length);
            Assert.AreEqual(1, directors.Length);

            var director = directors[0];
            director.initialTime = timelineCase.startFrame * 1.0 / 60.0;
            Assert.AreEqual(false, director.enabled);

            foreach (var vfx in vfxComponents)
                vfx.outputEventReceived += (args) => OnOutputEventReceived(vfx.name[^1], args);
            m_FrameOffset = Time.frameCount;
            m_ReceivedEvents.Clear();

            var previousCaptureFrameRate = Time.captureFramerate;
            var previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            var previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;

            Time.captureFramerate = 60;
            VFXManager.fixedTimeStep = 1.0f / 60.0f;
            VFXManager.maxDeltaTime = 1.0f / 60.0f;

            //Check VFX is alive
            int maxFrame = 64;
            while (maxFrame-- > 0 && m_ReceivedEvents.Count == 0)
                yield return new WaitForEndOfFrame();
            Assert.Greater(maxFrame, 0);

            m_FrameOffset = Time.frameCount + 1;
            m_ReceivedEvents.Clear();

            director.enabled = true;

            if (timelineCase.pause)
            {
                director.timeUpdateMode = DirectorUpdateMode.Manual;
                director.time = director.initialTime;
                director.Play();
                director.Evaluate();
                director.Pause();
            }

            for (int frame = timelineCase.startFrame; frame < 250; ++frame)
            {
                yield return new WaitForEndOfFrame();
            }

            Time.captureFramerate = previousCaptureFrameRate;
            VFXManager.fixedTimeStep = previousFixedTimeStep;
            VFXManager.maxDeltaTime = previousMaxDeltaTime;
            yield return new WaitForEndOfFrame();

            var currentStack = PrintTimelineStack(m_ReceivedEvents);
            var expectedStack = PrintTimelineStack(timelineCase.expectedQueue);
            Assert.AreEqual(expectedStack, currentStack, $"Expected:\n{expectedStack}\nActual:\n{currentStack}\n");
        }
#endif

        struct VFXBatchedEffectInfoContent
        {
            public string assetName;
            public VFXBatchedEffectInfo infos;
        }

        VFXBatchedEffectInfoContent ConvertBatchInfo(VFXBatchedEffectInfo batchInfo)
        {
            return new VFXBatchedEffectInfoContent()
            {
                assetName = batchInfo.vfxAsset.name,
                infos = batchInfo
            };
        }

        string DumpBatchInfo(IEnumerable<VFXBatchedEffectInfoContent> batchInfos)
        {
            var str = new StringBuilder();
            foreach (var batchInfo in batchInfos.OrderBy(o => o.assetName))
            {
                str.Append($"{batchInfo.assetName}");
                str.Append($" - activeBatchCount:{batchInfo.infos.activeBatchCount}");
                str.Append($" - inactiveBatchCount:{batchInfo.infos.inactiveBatchCount}");
                str.Append($" - activeInstanceCount:{batchInfo.infos.activeInstanceCount}");
                str.Append($" - unbatchedInstanceCount:{batchInfo.infos.unbatchedInstanceCount}");
                str.Append($" - totalInstanceCapacity:{batchInfo.infos.totalInstanceCapacity}");
                str.Append($" - maxInstancePerBatchCapacity:{batchInfo.infos.maxInstancePerBatchCapacity}");
                //Not relevant for this test:
                //str.Append($" - totalGPUSizeInBytes:{batchInfo.totalGPUSizeInBytes}");
                //str.Append($" - totalCPUSizeInBytes:{batchInfo.totalCPUSizeInBytes}");
                str.AppendLine();
            }
            return str.ToString();
        }

//See UUM-6235
#if !(VFX_TESTS_HAS_URP && UNITY_STANDALONE_OSX)
        //@gabriel: See VFXG-414, the following test is inspecting the instancing status of the test 025_ShaderKeywords.
        [UnityTest]
        public IEnumerator Load_Keyword_Scene_With_Instancing()
        {
            SceneManagement.SceneManager.LoadScene("Packages/com.unity.testing.visualeffectgraph/Scenes/025_ShaderKeywords.unity");
            yield return null;

            List<VisualEffectAsset> vfxAssets = new();
            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
            foreach (var vfx in vfxComponents)
            {
                if (!vfxAssets.Contains(vfx.visualEffectAsset))
                {
                    vfxAssets.Add(vfx.visualEffectAsset);
                }
            }

            List<VFXBatchedEffectInfoContent> batchInfos = new();
            foreach (var asset in vfxAssets)
            {
                var batchInfo = VFXManager.GetBatchedEffectInfo(asset);
                batchInfos.Add(ConvertBatchInfo(batchInfo));
            }

            var expectedBatchInfos = new[]
            {
                new VFXBatchedEffectInfoContent() { assetName = "025_ShaderKeywords_Constant_MultiCompile", infos = new VFXBatchedEffectInfo() { activeBatchCount = 1, inactiveBatchCount = 0, activeInstanceCount = 1, unbatchedInstanceCount = 0, totalInstanceCapacity = 64, maxInstancePerBatchCapacity = 64 } },
                new VFXBatchedEffectInfoContent() { assetName = "025_ShaderKeywords_Constant_ShaderFeature", infos = new VFXBatchedEffectInfo() { activeBatchCount = 1, inactiveBatchCount = 0, activeInstanceCount = 1, unbatchedInstanceCount = 0, totalInstanceCapacity = 64, maxInstancePerBatchCapacity = 64 } },
                new VFXBatchedEffectInfoContent() { assetName = "025_ShaderKeywords_Dynamic_Exposed", infos = new VFXBatchedEffectInfo() { activeBatchCount = 1, inactiveBatchCount = 0, activeInstanceCount = 48, unbatchedInstanceCount = 0, totalInstanceCapacity = 64, maxInstancePerBatchCapacity = 64 } },
                new VFXBatchedEffectInfoContent() { assetName = "025_ShaderKeywords_Dynamic_Random", infos = new VFXBatchedEffectInfo() { activeBatchCount = 1, inactiveBatchCount = 0, activeInstanceCount = 12, unbatchedInstanceCount = 0, totalInstanceCapacity = 64, maxInstancePerBatchCapacity = 64 } },
                new VFXBatchedEffectInfoContent() { assetName = "025_ShaderKeywords_Dynamic_Random_Animate", infos = new VFXBatchedEffectInfo() { activeBatchCount = 1, inactiveBatchCount = 0, activeInstanceCount = 12, unbatchedInstanceCount = 0, totalInstanceCapacity = 64, maxInstancePerBatchCapacity = 64 } },
            };

            var expectedBatchInfosDump = DumpBatchInfo(expectedBatchInfos);
            var actualBatchInfosDump = DumpBatchInfo(batchInfos);
            Assert.AreEqual(expectedBatchInfosDump, actualBatchInfosDump, $"{actualBatchInfosDump}\nvs.\n\n{expectedBatchInfosDump}");
        }
#endif

        private static Vector4[] s_SampleGradient_Branch_Instancing_Readback = null;

        static void SampleGradient_Branch_Instancing_Readback(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
                Debug.LogError("SampleGradient_Branch_Instancing_Readback failure.");

            var data = request.GetData<Vector4>();
            s_SampleGradient_Branch_Instancing_Readback = new Vector4[2];
            s_SampleGradient_Branch_Instancing_Readback[0] = data[0];
            s_SampleGradient_Branch_Instancing_Readback[1] = data[1];
        }

        [UnityTest, Description("Regression test UUM-58615")]
        public IEnumerator SampleGradient_Branch_Instancing()
        {
            var structuredBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, 2, 16);
            Shader.SetGlobalBuffer("Repro_SampleGradient_Branch_Instancing_Buffer", structuredBuffer);

            SceneManagement.SceneManager.LoadScene("Packages/com.unity.testing.visualeffectgraph/Scenes/Repro_SampleGradient_Branch_Instancing.unity");
            yield return null;
            
            s_SampleGradient_Branch_Instancing_Readback = new Vector4[2];
            var request = AsyncGPUReadback.Request(structuredBuffer, SampleGradient_Branch_Instancing_Readback);
            int maxFrame = 64;
            while (--maxFrame > 0)
            {
                if (Vector4.Magnitude(s_SampleGradient_Branch_Instancing_Readback[0] - new Vector4(1, 0, 0, 1)) < 1e-3f
                    && Vector4.Magnitude(s_SampleGradient_Branch_Instancing_Readback[1] - new Vector4(0, 1, 0, 1)) < 1e-3f)
                    break;

                if (request.done)
                    request = AsyncGPUReadback.Request(structuredBuffer, SampleGradient_Branch_Instancing_Readback);

                yield return null;
            }

            Assert.IsTrue(maxFrame > 0, $"Didn't received expected readback content: {s_SampleGradient_Branch_Instancing_Readback[0].ToString()}, {s_SampleGradient_Branch_Instancing_Readback[1].ToString()}");

            //Disable VFX before exiting this scene
            var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
            foreach (var vfx in vfxComponents)
            {
                vfx.enabled = false;
            }

            Shader.SetGlobalBuffer("Repro_SampleGradient_Branch_Instancing_Buffer", (GraphicsBuffer)null);
            structuredBuffer.Release();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetBundleHelper.Unload(m_AssetBundle);
        }
    }
}
