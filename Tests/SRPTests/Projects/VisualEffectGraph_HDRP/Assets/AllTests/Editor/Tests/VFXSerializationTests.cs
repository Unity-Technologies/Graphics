#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using UnityEditor.ShaderGraph.Internal;
using NUnit.Framework;

using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;

using Task = System.Threading.Tasks.Task;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSerializationTests
    {
        private readonly static string kTestAssetDir = "Assets/Tests";
        private readonly static string kTestAssetName = "TestAsset";
        private readonly static string kTestAssetPath = kTestAssetDir + "/" + kTestAssetName + ".vfx";

        private VisualEffectAsset CreateAssetAtPath(string path)
        {
            return VisualEffectAssetEditorUtility.CreateNewAsset(path);
        }

        [Test]
        public void VFXMemorySerializer_Dont_Crash_But_Trigger_Exception_On_Invalid_Usage()
        {
            var vfxGraph = VFXTestCommon.MakeTemporaryGraph();
            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            vfxGraph.AddChild(spawner);

            var dependencies = new HashSet<ScriptableObject>(new [] { vfxGraph });
            vfxGraph.CollectDependencies(dependencies);
            dependencies.Add(null); //Voluntary add an invalid element

            Byte[] backup = null;
            Assert.Throws<NullReferenceException>( () => backup = VFXMemorySerializer.StoreObjectsToByteArray(dependencies.ToArray()));
            Assert.IsNull(backup);
        }

        [Test]
        public void Sanitize_GetSpawnCount()
        {
            string kSourceAsset = "Assets/AllTests/Editor/Tests/VFXSerializationTests_GetSpawnCount.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);

            Assert.AreEqual(5, graph.children.OfType<VFXAttributeParameter>().Count());
            Assert.AreEqual(5, graph.children.OfType<VFXInlineOperator>().Where(o => o.type == typeof(uint)).Count());
            Assert.AreEqual(1, graph.children.OfType<VFXInlineOperator>().Where(o => o.type == typeof(float)).Count());
            Assert.AreEqual(1, graph.children.OfType<Operator.Add>().Count());
            Assert.AreEqual(1, graph.children.OfType<VFXInlineOperator>().Where(o => o.type == typeof(Color)).Count());

            foreach (var attribute in graph.children.OfType<VFXAttributeParameter>())
                Assert.IsTrue(attribute.outputSlots[0].HasLink());

            foreach (var inlineUInt in graph.children.OfType<VFXInlineOperator>().Where(o => o.type == typeof(uint)))
                Assert.IsTrue(inlineUInt.inputSlots[0].HasLink());

            foreach (var inlineFloat in graph.children.OfType<VFXInlineOperator>().Where(o => o.type == typeof(float)))
                Assert.IsTrue(inlineFloat.inputSlots[0].HasLink());

            foreach (var inlineColor in graph.children.OfType<VFXInlineOperator>().Where(o => o.type == typeof(Color)))
                Assert.IsTrue(inlineColor.inputSlots[0].HasLink());

            foreach (var add in graph.children.OfType<Operator.Add>())
            {
                Assert.AreEqual(2, add.operandCount);
                Assert.IsTrue(add.inputSlots[0].HasLink());
                Assert.IsTrue(add.inputSlots[1].HasLink());
            }
        }

        [Test]
        public void Sanitize_Shape_To_TShape()
        {
            var kSourceAsset = "Assets/AllTests/Editor/Tests/VFXSanitizeTShape.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);

            //Sphere Volume
            {
                var volumes = graph.children.OfType<Operator.SphereVolume>().ToArray();
                Assert.AreEqual(3, volumes.Count());

                var volume_A = volumes.FirstOrDefault(o => !o.inputSlots[0].HasLink(true));
                var volume_B = volumes.FirstOrDefault(o => o.inputSlots[0].HasLink(true) && o.inputSlots[0][1].LinkedSlots.First().owner is VFXParameter);
                var volume_C = volumes.FirstOrDefault(o => o.inputSlots[0].HasLink(true) && o.inputSlots[0][1].LinkedSlots.First().owner is VFXInlineOperator);

                Assert.IsNotNull(volume_A);
                Assert.IsNotNull(volume_B);
                Assert.IsNotNull(volume_C);
                Assert.AreNotEqual(volume_A, volume_B);
                Assert.AreNotEqual(volume_B, volume_C);

                var tSphere = (TSphere)volume_A.inputSlots[0].value;
                Assert.AreEqual(1.0f, tSphere.transform.position.x);
                Assert.AreEqual(2.0f, tSphere.transform.position.y);
                Assert.AreEqual(3.0f, tSphere.transform.position.z);
                Assert.AreEqual(4.0f, tSphere.radius);

                Assert.AreEqual(volume_B.inputSlots[0].space, volume_B.inputSlots[0][1].LinkedSlots.First().space);
                Assert.IsTrue(volume_B.inputSlots[0][0][0].HasLink());
                Assert.IsTrue(volume_B.inputSlots[0][1].HasLink());
                Assert.IsTrue(volume_B.outputSlots[0].HasLink());

                Assert.IsTrue(volume_C.inputSlots[0][0][0].HasLink());
                Assert.IsTrue(volume_C.inputSlots[0][1].HasLink());
                Assert.IsTrue(volume_C.outputSlots[0].HasLink());
            }

            //Cylinder & Cone Volume
            {
                var volumes = graph.children.OfType<Operator.ConeVolume>().ToArray();
                Assert.AreEqual(6, volumes.Count());

                var volume_cyl_A = volumes.FirstOrDefault(o => !o.inputSlots[0].HasLink(true) && ((float)o.inputSlots[0][0][0][1].value) == -0.5f);
                var volume_cone_A = volumes.FirstOrDefault(o => !o.inputSlots[0].HasLink(true) && ((float)o.inputSlots[0][0][0][1].value) != -0.5f);

                var tCone_cyl = (TCone)volume_cyl_A.inputSlots[0].value;
                Assert.AreEqual(1.0f, tCone_cyl.transform.position.x);
                Assert.AreEqual(-0.5f, tCone_cyl.transform.position.y); //has been corrected by half height
                Assert.AreEqual(3.0f, tCone_cyl.transform.position.z);
                Assert.AreEqual(4.0f, tCone_cyl.baseRadius);
                Assert.AreEqual(4.0f, tCone_cyl.topRadius);
                Assert.AreEqual(5.0f, tCone_cyl.height);

                var tCone_cone = (TCone)volume_cone_A.inputSlots[0].value;
                Assert.AreEqual(1.0f, tCone_cone.transform.position.x);
                Assert.AreEqual(2.0f, tCone_cone.transform.position.y);
                Assert.AreEqual(3.0f, tCone_cone.transform.position.z);
                Assert.AreEqual(4.0f, tCone_cone.baseRadius);
                Assert.AreEqual(5.0f, tCone_cone.topRadius);
                Assert.AreEqual(6.0f, tCone_cone.height);

                //It isn't obvious to make difference between old cylinder & old cone, basic check on other operators
                foreach (var volume in volumes)
                {
                    if (volume == volume_cyl_A || volume == volume_cone_A)
                        continue;

                    foreach (var subslot in volume.inputSlots[0].children)
                    {
                        if (subslot.property.type == typeof(Transform))
                        {
                            Assert.IsTrue(subslot[0].HasLink());
                            Assert.IsFalse(subslot[1].HasLink());
                            Assert.IsFalse(subslot[2].HasLink());
                        }
                        else
                        {
                            Assert.IsTrue(subslot.HasLink());
                        }
                    }
                }
            }

            //Circle Area
            {
                var volumes = graph.children.OfType<Operator.CircleArea>().ToArray();
                Assert.AreEqual(3, volumes.Count());

                var volume_A = volumes.FirstOrDefault(o => !o.inputSlots[0].HasLink(true));
                var volume_B = volumes.FirstOrDefault(o => o.inputSlots[0].HasLink(true) && o.inputSlots[0][1].LinkedSlots.First().owner is VFXParameter);
                var volume_C = volumes.FirstOrDefault(o => o.inputSlots[0].HasLink(true) && o.inputSlots[0][1].LinkedSlots.First().owner is VFXInlineOperator);

                Assert.IsNotNull(volume_A);
                Assert.IsNotNull(volume_B);
                Assert.IsNotNull(volume_C);
                Assert.AreNotEqual(volume_A, volume_B);
                Assert.AreNotEqual(volume_B, volume_C);

                var tCircle = (TCircle)volume_A.inputSlots[0].value;
                Assert.AreEqual(1.0f, tCircle.transform.position.x);
                Assert.AreEqual(2.0f, tCircle.transform.position.y);
                Assert.AreEqual(3.0f, tCircle.transform.position.z);
                Assert.AreEqual(4.0f, tCircle.radius);

                Assert.AreEqual(volume_B.inputSlots[0].space, volume_B.inputSlots[0][1].LinkedSlots.First().space);
                Assert.IsTrue(volume_B.inputSlots[0][0][0].HasLink());
                Assert.IsTrue(volume_B.inputSlots[0][1].HasLink());
                Assert.IsTrue(volume_B.outputSlots[0].HasLink());

                Assert.IsTrue(volume_C.inputSlots[0][0][0].HasLink());
                Assert.IsTrue(volume_C.inputSlots[0][1].HasLink());
                Assert.IsTrue(volume_C.outputSlots[0].HasLink());
            }

            //Torus Volume
            {
                var volumes = graph.children.OfType<Operator.TorusVolume>().ToArray();
                Assert.AreEqual(3, volumes.Count());

                var volume_A = volumes.FirstOrDefault(o => !o.inputSlots[0].HasLink(true));
                var volume_B = volumes.FirstOrDefault(o => o.inputSlots[0].HasLink(true) && o.inputSlots[0][1].LinkedSlots.First().owner is VFXParameter);
                var volume_C = volumes.FirstOrDefault(o => o.inputSlots[0].HasLink(true) && o.inputSlots[0][1].LinkedSlots.First().owner is VFXInlineOperator);

                Assert.IsNotNull(volume_A);
                Assert.IsNotNull(volume_B);
                Assert.IsNotNull(volume_C);
                Assert.AreNotEqual(volume_A, volume_B);
                Assert.AreNotEqual(volume_B, volume_C);

                var tTorus = (TTorus)volume_A.inputSlots[0].value;
                Assert.AreEqual(1.0f, tTorus.transform.position.x);
                Assert.AreEqual(2.0f, tTorus.transform.position.y);
                Assert.AreEqual(3.0f, tTorus.transform.position.z);
                Assert.AreEqual(4.0f, tTorus.majorRadius);
                Assert.AreEqual(5.0f, tTorus.minorRadius);

                Assert.AreEqual(volume_B.inputSlots[0].space, volume_B.inputSlots[0][1].LinkedSlots.First().space);
                Assert.IsTrue(volume_B.inputSlots[0][0][0].HasLink());
                Assert.IsTrue(volume_B.inputSlots[0][1].HasLink());
                Assert.IsTrue(volume_B.inputSlots[0][2].HasLink());
                Assert.IsTrue(volume_B.outputSlots[0].HasLink());

                Assert.IsTrue(volume_C.inputSlots[0][0][0].HasLink());
                Assert.IsTrue(volume_C.inputSlots[0][1].HasLink());
                Assert.IsTrue(volume_C.inputSlots[0][2].HasLink());
                Assert.IsTrue(volume_C.outputSlots[0].HasLink());
            }

            //Position Sphere
            {
                var initialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault(o => o.label == "position_sphere");
                Assert.IsNotNull(initialize);

                var sphereBlocks = initialize.children.OfType<Block.PositionSphere>().ToArray();
                Assert.AreEqual(4, sphereBlocks.Length);
                foreach (var block in sphereBlocks)
                {
                    Assert.AreEqual(3, block.inputSlots.Count);

                    if (block != sphereBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][0][1].HasLink()); //radius

                        if (block == sphereBlocks.First())
                            Assert.IsTrue(block.inputSlots[0][1]); //arc
                    }
                    else
                    {
                        var tArcSphere = (TArcSphere)block.inputSlots[0].value;
                        Assert.AreEqual(1.0f, tArcSphere.sphere.transform.position.x);
                        Assert.AreEqual(2.0f, tArcSphere.sphere.transform.position.y);
                        Assert.AreEqual(3.0f, tArcSphere.sphere.transform.position.z);
                        Assert.AreEqual(4.0f, tArcSphere.sphere.radius);
                        Assert.AreEqual(5.0f, tArcSphere.arc);
                    }

                    Assert.AreEqual(6.0f, block.inputSlots[1].value);
                    Assert.AreEqual(0.7f, block.inputSlots[2].value);
                }
            }

            //Position Circle
            {
                var initialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault(o => o.label == "position_circle");
                Assert.IsNotNull(initialize);

                var circleBlock = initialize.children.OfType<Block.PositionCircle>().ToArray();
                Assert.AreEqual(4, circleBlock.Length);
                foreach (var block in circleBlock)
                {
                    Assert.AreEqual(3, block.inputSlots.Count);

                    if (block != circleBlock.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][0][1].HasLink()); //radius

                        if (block == circleBlock.First())
                            Assert.IsTrue(block.inputSlots[0][1]); //arc
                    }
                    else
                    {
                        var tArcCircle = (TArcCircle)block.inputSlots[0].value;
                        Assert.AreEqual(1.0f, tArcCircle.circle.transform.position.x);
                        Assert.AreEqual(2.0f, tArcCircle.circle.transform.position.y);
                        Assert.AreEqual(3.0f, tArcCircle.circle.transform.position.z);
                        Assert.AreEqual(4.0f, tArcCircle.circle.radius);
                        Assert.AreEqual(5.0f, tArcCircle.arc);
                    }

                    Assert.AreEqual(6.0f, block.inputSlots[1].value);
                    Assert.AreEqual(0.7f, block.inputSlots[2].value);
                }
            }

            //Position Cone
            {
                var initialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault(o => o.label == "position_cone");
                Assert.IsNotNull(initialize);

                var coneBlocks = initialize.children.OfType<Block.PositionCone>().ToArray();
                Assert.AreEqual(3, coneBlocks.Length);
                foreach (var block in coneBlocks)
                {
                    Assert.AreEqual(4, block.inputSlots.Count);

                    if (block != coneBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][0][1].HasLink()); //baseradius
                        Assert.IsTrue(block.inputSlots[0][0][2].HasLink()); //topRadius
                        Assert.IsTrue(block.inputSlots[0][0][3].HasLink()); //height

                        if (block == coneBlocks.First())
                            Assert.IsTrue(block.inputSlots[0][1]); //arc
                    }
                    else
                    {
                        var tArcCone = (TArcCone)block.inputSlots[0].value;
                        Assert.AreEqual(1.0f, tArcCone.cone.transform.position.x);
                        Assert.AreEqual(2.0f, tArcCone.cone.transform.position.y);
                        Assert.AreEqual(3.0f, tArcCone.cone.transform.position.z);
                        Assert.AreEqual(4.0f, tArcCone.cone.baseRadius);
                        Assert.AreEqual(5.0f, tArcCone.cone.topRadius);
                        Assert.AreEqual(6.0f, tArcCone.cone.height);
                        Assert.AreEqual(0.7f, tArcCone.arc);
                    }

                    Assert.AreEqual(8.0f, block.inputSlots[1].value);
                    Assert.AreEqual(0.9f, block.inputSlots[2].value);
                    Assert.AreEqual(1.0f, block.inputSlots[3].value);
                }
            }

            //Position Torus
            {
                var initialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault(o => o.label == "position_torus");
                Assert.IsNotNull(initialize);

                var torusBlocks = initialize.children.OfType<Block.PositionTorus>().ToArray();
                Assert.AreEqual(3, torusBlocks.Length);
                foreach (var block in torusBlocks)
                {
                    Assert.AreEqual(3, block.inputSlots.Count);

                    if (block != torusBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][0][1].HasLink()); //majorRadius
                        Assert.IsTrue(block.inputSlots[0][0][2].HasLink()); //minorRadius

                        if (block == torusBlocks.First())
                            Assert.IsTrue(block.inputSlots[0][1]); //arc
                    }
                    else
                    {
                        var tArcTorus = (TArcTorus)block.inputSlots[0].value;
                        Assert.AreEqual(1.0f, tArcTorus.torus.transform.position.x);
                        Assert.AreEqual(2.0f, tArcTorus.torus.transform.position.y);
                        Assert.AreEqual(3.0f, tArcTorus.torus.transform.position.z);
                        Assert.AreEqual(4.0f, tArcTorus.torus.majorRadius);
                        Assert.AreEqual(5.0f, tArcTorus.torus.minorRadius);
                        Assert.AreEqual(0.6f, tArcTorus.arc);
                    }

                    Assert.AreEqual(7.0f, block.inputSlots[1].value);
                    Assert.AreEqual(0.8f, block.inputSlots[2].value);
                }
            }

            //Kill Sphere
            {
                var initialize = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.label == "kill_sphere");
                Assert.IsNotNull(initialize);

                var sphereBlocks = initialize.children.OfType<Block.KillSphere>().ToArray();
                Assert.AreEqual(3, sphereBlocks.Length);
                foreach (var block in sphereBlocks)
                {
                    Assert.AreEqual(1, block.inputSlots.Count);

                    if (block != sphereBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][1].HasLink()); //radius
                    }
                    else
                    {
                        var tSphere = (TSphere)block.inputSlots[0].value;
                        Assert.AreEqual(1.0f, tSphere.transform.position.x);
                        Assert.AreEqual(2.0f, tSphere.transform.position.y);
                        Assert.AreEqual(3.0f, tSphere.transform.position.z);
                        Assert.AreEqual(4.0f, tSphere.radius);
                    }
                }
            }

            //Collide Sphere
            {
                var initialize = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.label == "collision_sphere");
                Assert.IsNotNull(initialize);

                var sphereBlocks = initialize.children.OfType<Block.CollisionSphere>().ToArray();
                Assert.AreEqual(3, sphereBlocks.Length);
                foreach (var block in sphereBlocks)
                {
                    Assert.AreEqual(6, block.inputSlots.Count);

                    if (block != sphereBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][1].HasLink()); //radius
                    }
                    else
                    {
                        var tSphere = (TSphere)block.inputSlots[0].value;
                        Assert.AreEqual(1.0f, tSphere.transform.position.x);
                        Assert.AreEqual(2.0f, tSphere.transform.position.y);
                        Assert.AreEqual(3.0f, tSphere.transform.position.z);
                        Assert.AreEqual(4.0f, tSphere.radius);
                    }

                    Assert.AreEqual(0.2f, block.inputSlots[1].value);
                    Assert.AreEqual(0.3f, block.inputSlots[2].value);
                    Assert.AreEqual(0.4f, block.inputSlots[3].value);
                    Assert.AreEqual(0.5f, block.inputSlots[4].value);
                    Assert.AreEqual(0.6f, block.inputSlots[5].value);
                }
            }

            //Collide Cylinder (migration to cone)
            {
                var initialize = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.label == "collision_cylinder");
                Assert.IsNotNull(initialize);

                var coneBlocks = initialize.children.OfType<Block.CollisionCone>().ToArray();
                Assert.AreEqual(3, coneBlocks.Length);
                foreach (var block in coneBlocks)
                {
                    Assert.AreEqual(6, block.inputSlots.Count);

                    if (block != coneBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][1].HasLink()); //baseradius
                        Assert.IsTrue(block.inputSlots[0][2].HasLink()); //topRadius
                        Assert.IsTrue(block.inputSlots[0][3].HasLink()); //height
                    }
                    else
                    {
                        var tCone = (TCone)block.inputSlots[0].value;
                        Assert.AreEqual(1.0f, tCone.transform.position.x);
                        Assert.AreEqual(-0.5f, tCone.transform.position.y);
                        Assert.AreEqual(3.0f, tCone.transform.position.z);
                        Assert.AreEqual(4.0f, tCone.baseRadius); //<= That is the trick of cylinder migration
                        Assert.AreEqual(4.0f, tCone.topRadius);
                        Assert.AreEqual(5.0f, tCone.height);
                    }

                    Assert.AreEqual(0.2f, block.inputSlots[1].value);
                    Assert.AreEqual(0.3f, block.inputSlots[2].value);
                    Assert.AreEqual(0.4f, block.inputSlots[3].value);
                    Assert.AreEqual(0.5f, block.inputSlots[4].value);
                    Assert.AreEqual(0.6f, block.inputSlots[5].value);
                }
            }
        }

        [Test]
        public void Sanitize_MeshSampling_To_OutOfExperimental_MeshSampling()
        {
            string kSourceAsset = "Assets/AllTests/Editor/Tests/VFXSanitizeMeshSampling.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);

            Assert.AreEqual(2, graph.children.OfType<VFXBasicUpdate>().Count());
            Assert.AreEqual(4, graph.children.OfType<Operator.SampleMesh>().Count());

            var basicUpdateInLocal = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.space == VFXCoordinateSpace.Local);
            var basicUpdateInWorld = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.space == VFXCoordinateSpace.World);

            //N.B. Tangent is transform into Tangent (vec3) + BitangentSigne (float), if sanitize has been done, we have another input
            var sampleMeshAll = graph.children.OfType<Operator.SampleMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("source") == Operator.SampleMesh.SourceType.Mesh && o.outputSlots.Count != 4);
            var sampleSkinnedAll = graph.children.OfType<Operator.SampleMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("source") == Operator.SampleMesh.SourceType.SkinnedMeshRenderer && o.outputSlots.Count != 4);

            var sampleMeshSet = graph.children.OfType<Operator.SampleMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("source") == Operator.SampleMesh.SourceType.Mesh && o.outputSlots.Count == 4);
            var sampleSkinnedSet = graph.children.OfType<Operator.SampleMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("source") == Operator.SampleMesh.SourceType.SkinnedMeshRenderer && o.outputSlots.Count == 4);

            Assert.IsNotNull(basicUpdateInLocal);
            Assert.IsNotNull(basicUpdateInWorld);

            Assert.IsNotNull(sampleMeshAll);
            Assert.IsNotNull(sampleSkinnedAll);

            Assert.IsNotNull(sampleMeshSet);
            Assert.IsNotNull(sampleSkinnedSet);

            var positionMeshLocal = basicUpdateInLocal.children.OfType<Block.PositionMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("sourceMesh") == Operator.SampleMesh.SourceType.Mesh);
            var positionSkinnedLocal = basicUpdateInLocal.children.OfType<Block.PositionMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("sourceMesh") == Operator.SampleMesh.SourceType.SkinnedMeshRenderer);

            var positionMeshWorld = basicUpdateInWorld.children.OfType<Block.PositionMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("sourceMesh") == Operator.SampleMesh.SourceType.Mesh);
            var positionSkinnedWorld = basicUpdateInWorld.children.OfType<Block.PositionMesh>().FirstOrDefault(o => (Operator.SampleMesh.SourceType)o.GetSettingValue("sourceMesh") == Operator.SampleMesh.SourceType.SkinnedMeshRenderer);

            Assert.IsNotNull(positionMeshLocal);
            Assert.IsNotNull(positionSkinnedLocal);

            Assert.IsNotNull(positionMeshWorld);
            Assert.IsNotNull(positionSkinnedWorld);

            //Sanitization should insure identity transform to be consistent with previous behavior
            Assert.AreEqual(Operator.SampleMesh.SkinnedRootTransform.None, positionMeshLocal.GetSettingValue("skinnedTransform"));
            Assert.AreEqual(Operator.SampleMesh.SkinnedRootTransform.None, positionSkinnedLocal.GetSettingValue("skinnedTransform"));
            Assert.AreEqual(Operator.SampleMesh.SkinnedRootTransform.None, positionMeshWorld.GetSettingValue("skinnedTransform"));
            Assert.AreEqual(Operator.SampleMesh.SkinnedRootTransform.None, positionSkinnedWorld.GetSettingValue("skinnedTransform"));

            Assert.AreEqual(VFXCoordinateSpace.Local, positionMeshLocal.inputSlots.Last().space);
            Assert.AreEqual(VFXCoordinateSpace.Local, positionSkinnedLocal.inputSlots.Last().space);
            Assert.AreEqual(VFXCoordinateSpace.World, positionMeshWorld.inputSlots.Last().space);
            Assert.AreEqual(VFXCoordinateSpace.World, positionSkinnedWorld.inputSlots.Last().space);

            //Check where expected link
            var meshSlotAll = sampleMeshAll.outputSlots.Where(o => o.name != "Tangent");
            meshSlotAll = meshSlotAll.Concat(sampleMeshAll.outputSlots.First(o => o.name == "Tangent")[0].children);
            var skinnedMeshSlotAll = sampleSkinnedAll.outputSlots.Where(o => o.name != "Tangent");
            skinnedMeshSlotAll = skinnedMeshSlotAll.Concat(sampleSkinnedAll.outputSlots.First(o => o.name == "Tangent")[0].children);

            //The first two are Position & Normal and they have been redirected to an inline Vector3
            var meshSlotSet = new[]{ sampleMeshSet.outputSlots[0], sampleMeshSet.outputSlots[1], sampleMeshSet.outputSlots[2][0][0], sampleMeshSet.outputSlots[2][0][1], sampleMeshSet.outputSlots[2][0][2], sampleMeshSet.outputSlots[3] };
            var skinnedMeshSlotSet = new[] { sampleSkinnedSet.outputSlots[0], sampleSkinnedSet.outputSlots[1], sampleSkinnedSet.outputSlots[2][0][0], sampleSkinnedSet.outputSlots[2][0][1], sampleSkinnedSet.outputSlots[2][0][2], sampleSkinnedSet.outputSlots[3] };

            Assert.IsTrue(meshSlotAll.All(o => o.HasLink()));
            Assert.IsTrue(skinnedMeshSlotAll.All(o => o.HasLink()));

            Assert.IsTrue(meshSlotSet.All(o => o.HasLink()));
            Assert.IsTrue(skinnedMeshSlotSet.All(o => o.HasLink()));

            //Check dest link
            var addOperator = graph.children.OfType<Operator.Add>().FirstOrDefault();
            var mulOperator = graph.children.OfType<Operator.Multiply>().FirstOrDefault();
            var substractOperator = graph.children.OfType<Operator.Subtract>().FirstOrDefault();
            var divideOperator = graph.children.OfType<Operator.Subtract>().FirstOrDefault();

            Assert.IsNotNull(addOperator);
            Assert.IsNotNull(mulOperator);
            Assert.IsNotNull(substractOperator);
            Assert.IsNotNull(divideOperator);

            Assert.IsFalse(addOperator.outputSlots[0].spaceable);
            Assert.IsFalse(mulOperator.outputSlots[0].spaceable);
            Assert.IsFalse(substractOperator.outputSlots[0].spaceable);
            Assert.IsFalse(divideOperator.outputSlots[0].spaceable);

            Assert.AreEqual(typeof(Vector4), addOperator.outputSlots[0].property.type);
            Assert.AreEqual(typeof(Vector4), mulOperator.outputSlots[0].property.type);
            Assert.AreEqual(typeof(float), substractOperator.outputSlots[0].property.type);
            Assert.AreEqual(typeof(float), divideOperator.outputSlots[0].property.type);

            Assert.AreEqual(14, addOperator.inputSlots.Count);
            Assert.AreEqual(14, mulOperator.inputSlots.Count);

            Assert.AreEqual(10, substractOperator.inputSlots.Count);
            Assert.AreEqual(10, divideOperator.inputSlots.Count);

            Assert.IsTrue(addOperator.inputSlots.All(o => o.HasLink()));
            Assert.IsTrue(mulOperator.inputSlots.All(o => o.HasLink()));
            Assert.IsTrue(substractOperator.inputSlots.All(o => o.HasLink()));
            Assert.IsTrue(divideOperator.inputSlots.All(o => o.HasLink()));

            Assert.IsTrue(addOperator.inputSlots.Take(2).All(o => o.property.type == typeof(Vector3)));
            Assert.IsTrue(mulOperator.inputSlots.Take(2).All(o => o.property.type == typeof(Vector3)));
            Assert.IsTrue(addOperator.inputSlots.Skip(2).All(o => o.property.type == typeof(Vector4)));
            Assert.IsTrue(mulOperator.inputSlots.Skip(2).All(o => o.property.type == typeof(Vector4)));

        }

        //Cover case 1352832, extension of Sanitize_Shape_To_TShape
        [UnityTest]
        public IEnumerator Sanitize_Shape_To_TShape_And_Check_VFXParameter_State()
        {
            var kSourceAsset = "Assets/AllTests/Editor/Tests/VFXSanitizeTShape.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            yield return null;

            Assert.AreEqual(8, graph.children.OfType<VFXParameter>().Count());
            Assert.AreEqual(11, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Count());
            Assert.AreEqual(0, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Where(o => o.position == Vector2.zero).Count());
            Assert.AreEqual(0, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Where(o => !o.linkedSlots.Any()).Count());
            yield return null;

            var window = VFXViewWindow.GetWindow<VFXViewWindow>();
            var resource = graph.GetResource();
            window.LoadAsset(resource.asset, null);
            yield return null;

            Assert.AreEqual(8, graph.children.OfType<VFXParameter>().Count());
            Assert.AreEqual(11, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Count());
            Assert.AreEqual(0, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Where(o => o.position == Vector2.zero).Count(), "Fail after window.LoadAsset");
            foreach (var param in graph.children.OfType<VFXParameter>())
            {
                var nodes = param.nodes.Where(o => !o.linkedSlots.Any());
                if (nodes.Any())
                {
                    Assert.Fail(param.exposedName + " as an orphan node");
                }
            }
            Assert.AreEqual(0, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Where(o => !o.linkedSlots.Any()).Count()); //Orphan link
        }

        [OneTimeSetUpAttribute]
        public void OneTimeSetUpAttribute()
        {
            string[] guids = AssetDatabase.FindAssets(kTestAssetName, new string[] { kTestAssetDir });

            // If the asset does not exist, create it
            if (guids.Length == 0)
            {
                VisualEffectAsset asset = CreateAssetAtPath(kTestAssetPath);
                InitAsset(asset);
            }
        }

        /*
        [Test]
        public void SerializeModel()
        {
            VisualEffectAsset assetSrc = new VisualEffectAsset();
            VisualEffectAsset assetDst = new VisualEffectAsset();

            InitAsset(assetSrc);
            EditorUtility.CopySerialized(assetSrc, assetDst);
            CheckAsset(assetDst);

            Object.DestroyImmediate(assetSrc);
            Object.DestroyImmediate(assetDst);
        }*/

        [Test]
        public void LoadAssetFromPath()
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(kTestAssetPath);
            CheckAsset(asset);
        }

        private void InitAsset(VisualEffectAsset asset)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();
            graph.RemoveAllChildren();

            var init0 = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var update0 = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var output0 = ScriptableObject.CreateInstance<VFXPointOutput>();

            graph.AddChild(init0);
            graph.AddChild(update0);
            graph.AddChild(output0);

            init0.LinkTo(update0);
            update0.LinkTo(output0);

            var init1 = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var output1 = ScriptableObject.CreateInstance<VFXPointOutput>();

            init1.LinkTo(output1);

            graph.AddChild(init1);
            graph.AddChild(output1);

            // Add some block
            var block0 = ScriptableObject.CreateInstance<InitBlockTest>();
            var block1 = ScriptableObject.CreateInstance<UpdateBlockTest>();
            var block2 = ScriptableObject.CreateInstance<OutputBlockTest>();

            // Add some operator
            VFXOperator add = ScriptableObject.CreateInstance<Operator.Add>();

            init0.AddChild(block0);
            update0.AddChild(block1);
            output0.AddChild(block2);

            graph.AddChild(add);
        }

        private void CheckAsset(VisualEffectAsset asset)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();

            Assert.AreEqual(6, graph.GetNbChildren());

            Assert.AreEqual(1, graph[0].GetNbChildren());
            Assert.AreEqual(1, graph[1].GetNbChildren());
            Assert.AreEqual(1, graph[2].GetNbChildren());
            Assert.AreEqual(0, graph[3].GetNbChildren());
            Assert.AreEqual(0, graph[4].GetNbChildren());
            Assert.AreEqual(0, graph[5].GetNbChildren());

            Assert.IsNotNull((graph[0])[0]);
            Assert.IsNotNull((graph[1])[0]);
            Assert.IsNotNull((graph[2])[0]);

            Assert.AreEqual(VFXContextType.Init,   ((VFXContext)(graph[0])).contextType);
            Assert.AreEqual(VFXContextType.Update, ((VFXContext)(graph[1])).contextType);
            Assert.AreEqual(VFXContextType.Output, ((VFXContext)(graph[2])).contextType);
            Assert.AreEqual(VFXContextType.Init,   ((VFXContext)(graph[3])).contextType);
            Assert.AreEqual(VFXContextType.Output, ((VFXContext)(graph[4])).contextType);

            Assert.IsNotNull(graph[5] as Operator.Add);
        }

        private void CheckIsolatedOperatorAdd(Operator.Add add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(2, add.inputSlots.Count);
            Assert.AreEqual(typeof(float), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.inputSlots[1].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAdd);
        }

        private void CheckIsolatedOperatorAbs(Operator.Absolute add)
        {
            Assert.AreEqual(1, add.outputSlots.Count);
            Assert.AreEqual(1, add.inputSlots.Count);
            Assert.AreEqual(typeof(float), add.inputSlots[0].property.type);
            Assert.AreEqual(typeof(float), add.outputSlots[0].property.type);
            Assert.IsNotNull(add.outputSlots[0].GetExpression());
            Assert.IsNotNull(add.outputSlots[0].GetExpression() as VFXExpressionAbs);
        }

        private void CheckConnectedAbs(Operator.Absolute abs)
        {
            Assert.IsTrue(abs.inputSlots[0].HasLink());
            Assert.AreEqual(1, abs.inputSlots[0].LinkedSlots.Count());
            Assert.IsTrue(abs.inputSlots[0].GetExpression() is VFXExpressionAdd);
        }

        private void InnerSaveAndReloadTest(string suffixname, Action<VisualEffectAsset> write, Action<VisualEffectAsset> read)
        {
            var kTempAssetPathA = string.Format("{0}/Temp_{1}_A.vfx", kTestAssetDir, suffixname);
            var kTempAssetPathB = string.Format("{0}/Temp_{1}_B.vfx", kTestAssetDir, suffixname);
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);

            int hashCodeAsset = 0; //check reference are different between load & reload
            {
                var asset = CreateAssetAtPath(kTempAssetPathA);

                hashCodeAsset = asset.GetHashCode();

                write(asset);
                asset.GetResource().UpdateSubAssets();

                AssetDatabase.SaveAssets();

                asset = null;
                EditorUtility.UnloadUnusedAssetsImmediate();
                //AssetDatabase.CopyAsset(kTempAssetPathA, kTempAssetPathB); // TODO Deactivated because a regression makes it fail when load the copy
                File.Copy(kTempAssetPathA, kTempAssetPathB);

                if (asset != null)
                    AssetDatabase.RemoveObjectFromAsset(asset);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.UnloadUnusedAssetsImmediate();
            {
                VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(kTempAssetPathB);
                Assert.AreNotEqual(hashCodeAsset, asset.GetHashCode());

                read(asset);
            }
            AssetDatabase.DeleteAsset(kTempAssetPathA);
            AssetDatabase.DeleteAsset(kTempAssetPathB);
        }

        private void WriteBasicOperators(VisualEffectAsset asset, bool spawnAbs, bool linkAbs)
        {
            var add = ScriptableObject.CreateInstance<Operator.Add>();
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();
            graph.AddChild(add);

            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                var abs = ScriptableObject.CreateInstance<Operator.Absolute>();
                abs.position = new Vector2(64.0f, 64.0f);
                graph.AddChild(abs);
                CheckIsolatedOperatorAbs(abs);
                if (linkAbs)
                {
                    abs.inputSlots[0].Link(add.outputSlots[0]);
                    CheckConnectedAbs(abs);
                }
            }
        }

        private void ReadBasicOperators(VisualEffectAsset asset, bool spawnAbs, bool linkAbs)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();
            Assert.AreEqual(spawnAbs ? 2 : 1, graph.GetNbChildren());
            Assert.IsNotNull((Operator.Add)graph[0]);
            var add = (Operator.Add)graph[0];
            CheckIsolatedOperatorAdd(add);

            if (spawnAbs)
            {
                Assert.IsNotNull((Operator.Absolute)graph[1]);
                var abs = (Operator.Absolute)graph[1];
                CheckIsolatedOperatorAbs(abs);
                Assert.AreEqual(abs.position.x, 64.0f);
                Assert.AreEqual(abs.position.y, 64.0f);
                if (linkAbs)
                {
                    CheckConnectedAbs(abs);
                }
            }
        }

        private void BasicOperatorTest(string suffix, bool spawnAbs, bool linkAbs)
        {
            InnerSaveAndReloadTest(suffix,
                (a) => WriteBasicOperators(a, spawnAbs, linkAbs),
                (a) => ReadBasicOperators(a, spawnAbs, linkAbs));
        }

        [Test]
        public void SerializeOneOperator()
        {
            BasicOperatorTest("One", false, false);
        }

        [Test]
        public void SerializeTwoOperators()
        {
            BasicOperatorTest("Two", true, false);
        }

        [Test]
        public void SerializeTwoOperatorsLink()
        {
            BasicOperatorTest("TwoLinked", true, true);
        }

        [Test]
        public void SerializeOperatorMaskWithState()
        {
            var expectedValue = "xyxy";
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var mask = ScriptableObject.CreateInstance<Operator.Swizzle>();
                mask.SetSettingValue("mask", expectedValue);
                asset.GetResource().GetOrCreateGraph().AddChild(mask);
                Assert.AreEqual(expectedValue, mask.mask);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                Assert.AreEqual(1, graph.GetNbChildren());
                Assert.IsInstanceOf(typeof(Operator.Swizzle), graph[0]);
                var mask = graph[0] as Operator.Swizzle;

                Assert.AreEqual(expectedValue, mask.mask);
            };

            InnerSaveAndReloadTest("Mask", write, read);
        }

        [Test]
        public void SerializeParameter()
        {
            var name = "unity";
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                parameter.SetSettingValue("m_Exposed", true);
                parameter.SetSettingValue("m_ExposedName", name);
                asset.GetResource().GetOrCreateGraph().AddChild(parameter);
                Assert.AreEqual(VFXValueType.Float2, parameter.outputSlots[0].GetExpression().valueType);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var parameter = asset.GetResource().GetOrCreateGraph()[0] as VFXParameter;
                Assert.AreNotEqual(null, parameter);
                Assert.AreEqual(true, parameter.exposed);
                Assert.AreEqual(parameter.exposedName, name);
                Assert.AreEqual(VFXValueType.Float2, parameter.outputSlots[0].GetExpression().valueType);
            };

            InnerSaveAndReloadTest("Parameter", write, read);
        }

        [Test]
        public void SerializeOperatorAndParameter()
        {
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var add = ScriptableObject.CreateInstance<Operator.Add>();
                var parameter = VFXLibrary.GetParameters().First(o => o.name == "Vector2").CreateInstance();
                add.SetOperandType(0, typeof(Vector2));
                graph.AddChild(add);
                graph.AddChild(parameter);
                add.inputSlots[0].Link(parameter.outputSlots[0]);

                Assert.AreEqual(VFXValueType.Float2, add.outputSlots[0].GetExpression().valueType);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var add = graph[0] as Operator.Add;
                var parameter = graph[1] as VFXParameter;
                Assert.AreNotEqual(null, parameter);
                Assert.AreEqual(VFXValueType.Float2, add.outputSlots[0].GetExpression().valueType);
            };

            InnerSaveAndReloadTest("ParameterAndOperator", write, read);
        }

        [Test]
        public void SerializeBuiltInParameter()
        {
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var builtIn = VFXLibrary.GetOperators().First(o => o.name.StartsWith("Total Time (VFX)")).CreateInstance();
                asset.GetResource().GetOrCreateGraph().AddChild(builtIn);
                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var builtIn = asset.GetResource().GetOrCreateGraph()[0] as VFXDynamicBuiltInParameter;
                Assert.AreNotEqual(null, builtIn);
                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
            };
            InnerSaveAndReloadTest("BuiltInParameter", write, read);
        }

        [Test]
        public void SerializeOperatorAndBuiltInParameter()
        {
            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var add = ScriptableObject.CreateInstance<Operator.Add>();
                var builtIn = VFXLibrary.GetOperators().First(o => o.name.StartsWith("Total Time (VFX)")).CreateInstance();
                graph.AddChild(builtIn);
                graph.AddChild(add);
                add.inputSlots[0].Link(builtIn.outputSlots[0]);

                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
                Assert.IsTrue(add.inputSlots[0].HasLink());
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var graph = asset.GetResource().GetOrCreateGraph();
                var builtIn = graph[0] as VFXDynamicBuiltInParameter;
                var add = graph[1] as Operator.Add;

                Assert.AreNotEqual(null, builtIn);
                Assert.AreNotEqual(null, add);
                Assert.AreEqual(VFXExpressionOperation.TotalTime, builtIn.outputSlots[0].GetExpression().operation);
                Assert.IsTrue(add.inputSlots[0].HasLink());
            };
            InnerSaveAndReloadTest("BuiltInParameter", write, read);
        }

        [Test]
        public void SerializeAttributeParameter()
        {
            var testAttribute = "lifetime";
            Action<VFXAttributeParameter, VFXAttributeLocation> test = delegate(VFXAttributeParameter parameter, VFXAttributeLocation location)
            {
                Assert.AreEqual(VFXExpressionOperation.None, parameter.outputSlots[0].GetExpression().operation);
                Assert.AreEqual(VFXValueType.Float, parameter.outputSlots[0].GetExpression().valueType);
                Assert.IsInstanceOf(typeof(VFXAttributeExpression), parameter.outputSlots[0].GetExpression());
                Assert.AreEqual(location, (parameter.outputSlots[0].GetExpression() as VFXAttributeExpression).attributeLocation);
                Assert.AreEqual(testAttribute, (parameter.outputSlots[0].GetExpression() as VFXAttributeExpression).attributeName);
            };

            Action<VisualEffectAsset> write = delegate(VisualEffectAsset asset)
            {
                var sizeCurrent = VFXLibrary.GetOperators().First(o => o.name.Contains(testAttribute) && o.modelType == typeof(VFXAttributeParameter)).CreateInstance();
                var sizeSource = VFXLibrary.GetOperators().First(o => o.name.Contains(testAttribute) && o.modelType == typeof(VFXAttributeParameter)).CreateInstance();
                (sizeSource as VFXAttributeParameter).SetSettingValue("location", VFXAttributeLocation.Source);
                asset.GetResource().GetOrCreateGraph().AddChild(sizeCurrent);
                asset.GetResource().GetOrCreateGraph().AddChild(sizeSource);
                test(sizeCurrent as VFXAttributeParameter, VFXAttributeLocation.Current);
                test(sizeSource as VFXAttributeParameter, VFXAttributeLocation.Source);
            };

            Action<VisualEffectAsset> read = delegate(VisualEffectAsset asset)
            {
                var sizeCurrent = asset.GetResource().GetOrCreateGraph()[0] as VFXAttributeParameter;
                var sizeSource = asset.GetResource().GetOrCreateGraph()[1] as VFXAttributeParameter;
                Assert.AreNotEqual(null, sizeCurrent);
                Assert.AreNotEqual(null, sizeSource);
                test(sizeCurrent, VFXAttributeLocation.Current);
                test(sizeSource, VFXAttributeLocation.Source);
            };
            InnerSaveAndReloadTest("AttributeParameter", write, read);
        }

        //Cover unexpected behavior : 1307562
        [Test]
        public void Verify_Orphan_Dependencies_Are_Correctly_Cleared()
        {
            string path = null;
            {
                var graph = VFXTestCommon.MakeTemporaryGraph();
                path = AssetDatabase.GetAssetPath(graph);

                var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
                var slotCount = blockConstantRate.GetInputSlot(0);

                var basicInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                var quadOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
                quadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Additive);

                var setPosition = ScriptableObject.CreateInstance<Block.SetAttribute>();
                setPosition.SetSettingValue("attribute", "position");
                setPosition.inputSlots[0].value = VFX.Position.defaultValue;
                basicInitialize.AddChild(setPosition);

                slotCount.value = 1.0f;

                spawnerContext.AddChild(blockConstantRate);
                graph.AddChild(spawnerContext);
                graph.AddChild(basicInitialize);
                graph.AddChild(quadOutput);

                basicInitialize.LinkFrom(spawnerContext);
                quadOutput.LinkFrom(basicInitialize);
            }

            var recordedSize = new List<long>();
            for (uint i = 0; i < 16; ++i)
            {
                AssetDatabase.ImportAsset(path);
                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                var graph = asset.GetResource().GetOrCreateGraph();
                graph.GetResource().WriteAsset();

                recordedSize.Add(new FileInfo(path).Length);

                var quadOutput = graph.children.OfType<VFXPlanarPrimitiveOutput>().FirstOrDefault();

                quadOutput.UnlinkAll();
                graph.RemoveChild(quadOutput);

                var newQuadOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
                newQuadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Additive);

                graph.AddChild(newQuadOutput);
                var basicInitialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
                newQuadOutput.LinkFrom(basicInitialize);
            }

            Assert.AreEqual(1, recordedSize.GroupBy(o => o).Count());
            Assert.AreNotEqual(0u, recordedSize[0]);
        }

        [Test]
        public void Check_Directory_Not_Imported()
        {
            var folderEndingWithVFX = "Assets/FolderTest.vfx";
            Directory.CreateDirectory(folderEndingWithVFX);
// See this PR https://github.com/Unity-Technologies/Graphics/pull/6890
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Assert.False(VisualEffectAssetModificationProcessor.HasVFXExtension(folderEndingWithVFX));
#else
            Assert.True(VisualEffectAssetModificationProcessor.HasVFXExtension(folderEndingWithVFX));
#endif

            Directory.Delete(folderEndingWithVFX);
        }

        [Test]
        public void Check_Null_Or_Empty_Path_Not_Imported()
        {
            Assert.False(VisualEffectAssetModificationProcessor.HasVFXExtension(null));
            Assert.False(VisualEffectAssetModificationProcessor.HasVFXExtension(string.Empty));
        }

        [Test(Description = "Cover issue 1403202")]
        public void Check_GPU_Event_In_Subgraph_With_Link()
        {
            string kSourceAssetMain = "Assets/AllTests/Editor/Tests/VFXGPUEvent_Main.vfx_";
            var main = VFXTestCommon.CopyTemporaryGraph(kSourceAssetMain);

            string kSourceAssetSubGraph = "Assets/AllTests/Editor/Tests/VFXGPUEvent_Subgraph.vfx_";
            var subGraph = VFXTestCommon.CopyTemporaryGraph(kSourceAssetSubGraph);

            var subgraphContext = main.children.OfType<VFXSubgraphContext>().FirstOrDefault();
            Assert.IsNotNull(subgraphContext);

            var subGraphAsset = subGraph.GetResource().asset;
            Assert.IsNotNull(subGraphAsset);

            subgraphContext.SetSettingValue("m_Subgraph", subGraphAsset);
            Assert.AreEqual(subGraphAsset, subgraphContext.GetSettingValue("m_Subgraph"));

            var path = AssetDatabase.GetAssetPath(main);
            AssetDatabase.ImportAsset(path);
        }


        //Cover regression test : 1315191
        [UnityTest]
        public IEnumerator Save_Then_Modify_Something_Check_The_Content_Isnt_Reverted()
        {
            string path = null;
            uint baseValue = 100;
            var graph = VFXTestCommon.MakeTemporaryGraph();
            {
                path = AssetDatabase.GetAssetPath(graph);

                var unsigned = ScriptableObject.CreateInstance<VFXInlineOperator>();
                unsigned.SetSettingValue("m_Type", (SerializableType)typeof(uint));
                unsigned.inputSlots[0].value = baseValue;
                graph.AddChild(unsigned);

                AssetDatabase.ImportAsset(path);
            }
            yield return null;

            for (uint i = 0; i < 3; ++i)
            {
                var inlineOperator = graph.children.OfType<VFXInlineOperator>().FirstOrDefault();
                Assert.IsNotNull(inlineOperator);
                Assert.AreEqual(baseValue + i, (uint)inlineOperator.inputSlots[0].value, "Failing at iteration : " + i);
                graph.GetResource().WriteAsset();

                inlineOperator.inputSlots[0].value = baseValue + i + 1; //Update for next iteration
                Assert.AreEqual(baseValue + i + 1, (uint)inlineOperator.inputSlots[0].value);
                AssetDatabase.ImportAsset(path);
                yield return null;
            }
        }

        private static readonly string s_Modify_SG_Property_VFX = "Assets/AllTests/Editor/Tests/Modify_SG_Property.vfx";
        private static readonly string s_Modify_SG_Property_SG_A = "Assets/AllTests/Editor/Tests/Modify_SG_Property_A.shadergraph";
        private static readonly string s_Modify_SG_Property_SG_B = "Assets/AllTests/Editor/Tests/Modify_SG_Property_B.shadergraph";
        private string m_Modify_SG_Property_VFX;
        private string m_Modify_SG_Property_SG_A;
        private string m_Modify_SG_Property_SG_B;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Modify_SG_Property_VFX = File.ReadAllText(s_Modify_SG_Property_VFX);
            m_Modify_SG_Property_SG_A = File.ReadAllText(s_Modify_SG_Property_SG_A);
            m_Modify_SG_Property_SG_B = File.ReadAllText(s_Modify_SG_Property_SG_B);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        //Cover regression 1361601
        [UnityTest]
        public IEnumerator Modify_ShaderGraph_Property_Check_VFX_Compilation_Doesnt_Fail()
        {
            Assert.IsTrue(m_Modify_SG_Property_SG_A.Contains("Name_A"));
            Assert.IsFalse(m_Modify_SG_Property_SG_A.Contains("Name_B"));
            Assert.IsTrue(m_Modify_SG_Property_SG_B.Contains("Name_B"));
            Assert.IsFalse(m_Modify_SG_Property_SG_B.Contains("Name_A"));
            Assert.IsTrue(m_Modify_SG_Property_VFX.Contains("Name_A"));
            Assert.IsFalse(m_Modify_SG_Property_VFX.Contains("Name_B"));

            AssetDatabase.ImportAsset(s_Modify_SG_Property_VFX);

            //Actually, rename the exposed property "Name_A" into "Name_B"
            File.WriteAllText(s_Modify_SG_Property_SG_A, m_Modify_SG_Property_SG_B);
            yield return null;

            //These import aren't suppose to trigger an exception
            AssetDatabase.ImportAsset(s_Modify_SG_Property_SG_A);
            AssetDatabase.ImportAsset(s_Modify_SG_Property_VFX);

            yield return null;

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(s_Modify_SG_Property_VFX);
            var graph = asset.GetResource().GetOrCreateGraph();
            graph.GetResource().WriteAsset();

            var newVFXContent = File.ReadAllText(s_Modify_SG_Property_VFX);
            Assert.IsTrue(newVFXContent.Contains("Name_B"));
        }

        [UnityTest, Description("Cover regression UUM-13863")]
        public IEnumerator Crash_On_StoreObject_While_Modifying_SG()
        {
            var reproContent = "Assets/AllTests/Editor/Tests/VFXSerialization_Repro_13863.zip";
            var tempDest = VFXTestCommon.tempBasePath + "/Repro_13863";

            System.IO.Compression.ZipFile.ExtractToDirectory(reproContent, tempDest);
            AssetDatabase.Refresh();
            yield return null;

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(tempDest + "/Repro_13863.vfx");
            Assert.IsNotNull(asset);

            VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0);
            var window = VFXViewWindow.GetWindow(asset);
            window.LoadAsset(asset, null);
            for (int i = 0; i < 4; ++i)
                yield return null;

            var baseFilePath = tempDest + "/Repro_13863_A.shadergraph";
            var backupSGContent = File.ReadAllBytes(baseFilePath);
            var newSGContent = File.ReadAllBytes(tempDest + "/Repro_13863_B.shadergraph");
            Assert.IsNotEmpty(backupSGContent);
            Assert.IsNotEmpty(newSGContent);

            //Modify SG once
            File.WriteAllBytes(baseFilePath, newSGContent);
            AssetDatabase.Refresh();
            for (int i = 0; i < 4; ++i)
                yield return null;

            //Restore
            File.WriteAllBytes(baseFilePath, backupSGContent);
            AssetDatabase.Refresh();

            for (int i = 0; i < 4; ++i)
                yield return null; //Crash is occurring here


            window.Close();
            for (int i = 0; i < 4; ++i)
                yield return null;
        }

        [UnityTest, Description("Cover case UUM-553")]
        public IEnumerator Unexpected_Import_Issue_With_Diffusion_Profile()
        {
            var packagePath = "Assets/AllTests/Editor/Tests/Import_Diffusion_Profile_Repro_553.unitypackage";
            AssetDatabase.ImportPackage(packagePath, false);
            //Shouldn't log Repro_553_EmptySG_SSR_DiffProfile.shadergraph has been scheduled for reimport during the Refresh loop and Loading of it has been attempted.
            for (int i = 0; i < 4; ++i)
                yield return null;
        }

        static List<string> ExtractPropertyList(VFXMaterialSerializedSettings settings)
        {
            var field = typeof(VFXMaterialSerializedSettings).GetField("m_PropertyMap", BindingFlags.Instance | BindingFlags.NonPublic);
            var entry = field.GetValue(settings) as Dictionary<string, float>;
            return entry.Keys.ToList();
        }

        [UnityTest, Description("UUM-41220")]
        public IEnumerator Switch_ShaderGraph_From_Old_To_New_Check_MaterialSettings()
        {
            var tempDest = VFXTestCommon.tempBasePath + "/Repro_UUM_41220/";
            Directory.CreateDirectory(tempDest);


            var baseVFXPath = "Assets/AllTests/Editor/Tests/VFXShaderGraphMaterialSettings.vfx";
            var sgOldPath = "Assets/AllTests/Editor/Tests/VFXShaderGraphMaterialSettings_OldSG.shadergraph";
            var sgNewPath = "Assets/AllTests/Editor/Tests/VFXShaderGraphMaterialSettings_NewSG.shadergraph";

            var originalVfxContent = File.ReadAllBytes(baseVFXPath);
            var sgOldContent = File.ReadAllBytes(sgOldPath);
            var sgNewContent = File.ReadAllBytes(sgNewPath);

            Assert.IsNotEmpty(originalVfxContent);
            Assert.IsNotEmpty(sgOldContent);
            Assert.IsNotEmpty(sgNewContent);

            var vfxPath = tempDest + "Repro_" + System.Guid.NewGuid() + ".vfx";
            var sgPath = tempDest + "Repro_" + System.Guid.NewGuid() + ".shaderGraph";
            File.WriteAllBytes(vfxPath, originalVfxContent);
            File.WriteAllBytes(sgPath, sgOldContent);
            AssetDatabase.Refresh();
            yield return null;

            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var sg = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(sgPath);

            Assert.IsNotNull(vfx);
            Assert.IsNotNull(sg);

            var vfxResource = (VisualEffectResource)vfx.GetOrCreateResource();
            var vfxGraph = vfxResource.GetOrCreateGraph();
            var output = vfxGraph.children.OfType<VFXShaderGraphParticleOutput>().Single();
            output.SetSettingValue("shaderGraph", sg);
            AssetDatabase.ImportAsset(vfxPath);
            yield return null;

            var materialSettings = output.GetSetting("materialSettings");
            var properties = ExtractPropertyList((VFXMaterialSerializedSettings)materialSettings.value);
            Assert.IsEmpty(properties);

            File.WriteAllBytes(sgPath, sgNewContent);
            AssetDatabase.Refresh();
            yield return null;

            materialSettings = output.GetSetting("materialSettings");
            properties = ExtractPropertyList((VFXMaterialSerializedSettings)materialSettings.value);
            Assert.IsNotEmpty(properties);
        }
    }


    [TestFixture]
    public class VFXSerializationTestsWithCustomLogger
    {
        private ILogHandler m_BackupLogHandler;

        [OneTimeSetUp]
        public void SetUp()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            m_BackupLogHandler = Debug.unityLogger.logHandler;
        }

        //Equivalent of LogAssert but always works during import
        //LogAssert.Expect(LogType.Error, new Regex("You must use an unlit vfx master node with an unlit output"));
        //LogAssert.Expect(LogType.Error, new Regex("System.InvalidOperationException"));
        //It also provides the ability of breaking on log while running test
        class ShaderGraph_Lit_On_Unlit_Logger : ILogHandler
        {
            public ILogHandler m_ForwardHandler;

            public bool m_HasLoggedMustUnlit;
            public bool m_HasLoggedCantCompile;
            public bool m_HasLoggedFail;

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (logType == LogType.Error)
                {
                    var result = string.Format(format, args);
                    if (result.StartsWith("You must use an unlit vfx master node with an unlit output"))
                    {
                        m_HasLoggedMustUnlit = true;
                        return;
                    }

                    if (result.Contains("Code generation failure from context"))
                    {
                        m_HasLoggedFail = true;
                        return;
                    }
                }

                m_ForwardHandler.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                m_ForwardHandler.LogException(exception, context);
            }
        }

        [UnityTest, Description("Cover regression UUM-5728")]
        public IEnumerator ShaderGraph_Lit_On_Unlit()
        {
            var reproContent = "Assets/AllTests/Editor/Tests/VFXSerialization_Repro_5728.zip";
            var tempDest = VFXTestCommon.tempBasePath + "/Repro_5728";

            System.IO.Compression.ZipFile.ExtractToDirectory(reproContent, tempDest);

            var customLogger = new ShaderGraph_Lit_On_Unlit_Logger() { m_ForwardHandler = m_BackupLogHandler };;

            Debug.unityLogger.logHandler = customLogger;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            yield return null;

            SceneManagement.EditorSceneManager.OpenScene(tempDest + "/Repro_5728.unity");

            for (int i = 0; i < 4; ++i)
                yield return null;

            SceneManagement.EditorSceneManager.OpenScene("Assets/empty.unity");

            for (int i = 0; i < 4; ++i)
                yield return null;

            Assert.IsTrue(customLogger.m_HasLoggedMustUnlit);
            Assert.IsTrue(customLogger.m_HasLoggedFail);
        }

        [UnityTest, Description("Cover regression UUM-563")]
        public IEnumerator ShaderGraph_Not_Reverted_On_Save()
        {
            // Create empty graph
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXViewWindow.GetWindow(graph.GetResource(), true);
            var viewController = VFXViewController.GetController(graph.GetResource(), true);
            window.graphView.controller = viewController;
            yield return null;

            // Add a static mesh output
            var staticMeshOutputContextDesc = VFXLibrary.GetContexts().Single(x => x.model is VFXStaticMeshOutput);
            var staticMeshOutputContext = (VFXStaticMeshOutput)window.graphView.controller.AddVFXContext(Vector2.zero, staticMeshOutputContextDesc);
            var shaderGraph = AssetDatabase.LoadAssetAtPath<Shader>("Assets/AllTests/Editor/Tests/Modify_SG_Property_A.shadergraph");
            staticMeshOutputContext.SetSettingValue("shader", shaderGraph);
            window.graphView.OnSave();
            yield return null;

            // Check that the shader is correctly assigned
            var s = staticMeshOutputContext.GetSetting("shader").value;
            Assert.NotNull(staticMeshOutputContext.GetSetting("shader").value);

            // Change shader to None
            staticMeshOutputContext.SetSettingValue("shader", null);
            window.graphView.OnSave();
            yield return null;

            // Check that the shader is still set to None
            Assert.IsNull(staticMeshOutputContext.GetSetting("shader").value, "The shader was expected to be null but it didn't. Probably the previous value has been restored when saving");
        }


        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
            Debug.unityLogger.logHandler = m_BackupLogHandler;
        }
    }
}
#endif
