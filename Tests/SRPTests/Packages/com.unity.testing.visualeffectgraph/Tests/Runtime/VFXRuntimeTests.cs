using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Testing.VisualEffectGraph;
using UnityEngine.TestTools;


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

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetBundleHelper.Unload(m_AssetBundle);
        }
    }
}
