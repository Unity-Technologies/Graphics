using System;
using System.Collections;
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

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetBundleHelper.Unload(m_AssetBundle);
        }
    }
}
