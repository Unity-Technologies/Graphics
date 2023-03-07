using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Test
{
    public class VisualEffectTest : VFXPlayModeTest
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest, Description("Regression test UUM-6234")]
        public IEnumerator Delete_Mesh_While_Rendering()
        {
            yield return new EnterPlayMode();

            var graph = VFXTestCommon.MakeTemporaryGraph();
            string meshFilePath;
            {
                //Create Mesh Asset
                Mesh mesh;
                {
                    var resourceMesh = new Mesh()
                    {
                        vertices = new[]
                        {
                            new Vector3(0, 0, 0),
                            new Vector3(1, 1, 0),
                            new Vector3(1, 0, 0),
                        },
                        triangles = new[] {0, 1, 2}
                    };
                    var guid = System.Guid.NewGuid().ToString();
                    meshFilePath = string.Format(VFXTestCommon.tempBasePath + "Mesh_{0}.asset", guid);
                    AssetDatabase.CreateAsset(resourceMesh, meshFilePath);
                    AssetDatabase.SaveAssets();
                    mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshFilePath);
                }
                Assert.IsNotNull(mesh);

                //Create VFXAsset
                {
                    var staticMeshOutput = ScriptableObject.CreateInstance<VFXStaticMeshOutput>();
                    var slots = staticMeshOutput.inputSlots.Where(o => o.value is Mesh).ToArray();
                    Assert.IsTrue(slots.Any());
                    foreach (var slot in slots)
                    {
                        if (slot.value is Mesh)
                            slot.value = mesh;
                    }
                    graph.AddChild(staticMeshOutput);

                    var particleOutput = ScriptableObject.CreateInstance<VFXMeshOutput>();
                    particleOutput.SetSettingValue("castShadows", true);
                    slots = particleOutput.inputSlots.Where(o => o.value is Mesh).ToArray();
                    Assert.IsTrue(slots.Any());
                    foreach (var slot in slots)
                    {
                        if (slot.value is Mesh)
                            slot.value = mesh;
                    }
                    graph.AddChild(particleOutput);

                    var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                    contextInitialize.LinkTo(particleOutput);
                    graph.AddChild(contextInitialize);

                    var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                    spawner.LinkTo(contextInitialize);
                    graph.AddChild(spawner);

                    var burst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
                    spawner.AddChild(burst);
                    burst.inputSlots[0].value = 1.0f;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
                }
            }

            var cameraTransform = Camera.main.transform;
            cameraTransform.localPosition = Vector3.one;
            cameraTransform.LookAt(Vector3.zero);

            //Create object and wait to have visible particles
            GameObject currentObject = new GameObject("Delete_Mesh_While_Rendered_With_Output", /*typeof(Transform),*/ typeof(VisualEffect));
            var vfx = currentObject.GetComponent<VisualEffect>();
            vfx.visualEffectAsset = graph.visualEffectResource.asset;
            int maxFrame = 64;
            while (vfx.aliveParticleCount == 0 && maxFrame-- > 0)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            //Delete Mesh & Wait a few frame
            File.Delete(meshFilePath);
            File.Delete(meshFilePath + ".meta");
            for (int i = 0; i < 4; ++i)
                yield return null;
            AssetDatabase.Refresh();
            for (int i = 0; i < 4; ++i)
                yield return null;

            //Check content from VFX
            {
                var meshOutput = graph.children.OfType<VFXMeshOutput>().First();
                var meshSlot = meshOutput.inputSlots.First(o => o.property.type == typeof(Mesh));

                var mesh = meshSlot.value as UnityEngine.Object;
                Assert.IsTrue(mesh == null); //Mesh should be deleted at this point...
                Assert.IsFalse(ReferenceEquals(mesh, null)); //... but expected missing reference
            }

            yield return new ExitPlayMode();
        }
    }
}
