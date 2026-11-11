using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Serialization
{
    [TestFixture]
    public class VFXSerializationMigrationTests
    {
        [Test]
        public void Sanitize_GetSpawnCount()
        {
            string kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/SpawnCount/VFXSerializationTests_GetSpawnCount.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            graph.PrepareGraph(); //This isn't called automatically anymore by the import /!\

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
            var kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/ShapeMigration/VFXSanitizeTShape.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            graph.PrepareGraph();

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

                var sphereBlocks = initialize.children.OfType<Block.PositionShape>().Where(o => (PositionShapeBase.Type)o.GetSettingValue("shape") == PositionShapeBase.Type.Sphere).ToArray();
                Assert.AreEqual(4, sphereBlocks.Length);
                foreach (var block in sphereBlocks)
                {
                    //N.B: PositionSphereDeprecatedV2 had only one dimension sequencer, it has been fixed with PositionShape
                    Assert.AreEqual(4, block.inputSlots.Count);

                    if (block != sphereBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][0][1].HasLink()); //radius

                        if (block == sphereBlocks.First())
                            Assert.IsTrue(block.inputSlots[0][1].HasLink()); //arc
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

                    Assert.AreEqual(0.0f, block.inputSlots[1].value); //height sequencer
                    Assert.IsTrue(block.inputSlots[1].HasLink());
                    Assert.AreEqual(0.7f, block.inputSlots[2].value); //arc sequencer
                    Assert.AreEqual(6.0f, block.inputSlots[3].value); //thickness
                }
            }

            //Position Circle
            {
                var initialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault(o => o.label == "position_circle");
                Assert.IsNotNull(initialize);

                var circleBlock = initialize.children.OfType<Block.PositionShape>().Where(o => (PositionShapeBase.Type)o.GetSettingValue("shape") == PositionShapeBase.Type.Circle).ToArray();
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

                    Assert.AreEqual(0.7f, block.inputSlots[1].value);
                    Assert.AreEqual(6.0f, block.inputSlots[2].value);
                }
            }

            //Position Cone
            {
                var initialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault(o => o.label == "position_cone");
                Assert.IsNotNull(initialize);

                var coneBlocks = initialize.children.OfType<Block.PositionShape>().Where(o => (PositionShapeBase.Type)o.GetSettingValue("shape") == PositionShapeBase.Type.Cone).ToArray();
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

                    Assert.AreEqual(0.9f, block.inputSlots[1].value);
                    Assert.AreEqual(1.0f, block.inputSlots[2].value);
                    Assert.AreEqual(8.0f, block.inputSlots[3].value);
                }
            }

            //Position Torus
            {
                var initialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault(o => o.label == "position_torus");
                Assert.IsNotNull(initialize);

                var torusBlocks = initialize.children.OfType<Block.PositionShape>().Where(o => (PositionShapeBase.Type)o.GetSettingValue("shape") == PositionShapeBase.Type.Torus).ToArray();
                Assert.AreEqual(3, torusBlocks.Length);
                foreach (var block in torusBlocks)
                {
                    Assert.AreEqual(4, block.inputSlots.Count);

                    if (block != torusBlocks.Last())
                    {
                        Assert.IsTrue(block.inputSlots[0][0][0][0].HasLink()); //center
                        Assert.IsTrue(block.inputSlots[0][0][1].HasLink()); //majorRadius
                        Assert.IsTrue(block.inputSlots[0][0][2].HasLink()); //minorRadius

                        if (block == torusBlocks.First())
                            Assert.IsTrue(block.inputSlots[0][1].HasLink()); //arc
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

                    Assert.IsTrue(block.inputSlots[1].HasLink()); //height sequencer
                    Assert.AreEqual(0.8f, block.inputSlots[2].value); //arc sequencer
                    Assert.AreEqual(7.0f, block.inputSlots[3].value); //thickness
                }
            }

            //Kill Sphere
            {
                var initialize = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.label == "kill_sphere");
                Assert.IsNotNull(initialize);

                var sphereBlocks = initialize.children.OfType<Block.CollisionBase>().Where(o => (CollisionBase.Behavior)o.GetSetting("behavior").value == CollisionBase.Behavior.Kill).ToArray();
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

                var sphereBlocks = initialize.children.OfType<Block.CollisionShape>().Where(o => (Block.CollisionShapeBase.Type)o.GetSetting("shape").value == Block.CollisionShapeBase.Type.Sphere) .ToArray();
                Assert.AreEqual(3, sphereBlocks.Length);
                foreach (var block in sphereBlocks)
                {
                    Assert.AreEqual(7, block.inputSlots.Count);

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
                    Assert.AreEqual(0.0f, block.inputSlots[3].value); // Overridden bounce speed threshold
                    Assert.AreEqual(0.4f, block.inputSlots[4].value);
                    Assert.AreEqual(0.5f, block.inputSlots[5].value);
                    Assert.AreEqual(0.6f, block.inputSlots[6].value);
                }
            }

            //Collide Cylinder (migration to cone)
            {
                var initialize = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.label == "collision_cylinder");
                Assert.IsNotNull(initialize);

                var coneBlocks = initialize.children.OfType<Block.CollisionShape>().Where(o => (Block.CollisionShapeBase.Type)o.GetSetting("shape").value == Block.CollisionShapeBase.Type.Cone).ToArray();
                Assert.AreEqual(3, coneBlocks.Length);
                foreach (var block in coneBlocks)
                {
                    Assert.AreEqual(7, block.inputSlots.Count);

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
                    Assert.AreEqual(0.0f, block.inputSlots[3].value); // Overridden bounce speed threshold
                    Assert.AreEqual(0.4f, block.inputSlots[4].value);
                    Assert.AreEqual(0.5f, block.inputSlots[5].value);
                    Assert.AreEqual(0.6f, block.inputSlots[6].value);
                }
            }
        }

        [Test]
        public void Sanitize_Position_Block_Shape()
        {
            var kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/ShapeMigration/VFXSanitizePositionShapeV2.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            graph.PrepareGraph();

            Assert.AreEqual(2, graph.children.OfType<VFXBasicUpdate>().Count());

            var updateRandom = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.label == "Random");
            var updateCustom = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.label == "Custom");

            Assert.IsNotNull(updateRandom);
            Assert.IsNotNull(updateCustom);

            Assert.AreEqual(6, updateRandom.children.Count());
            Assert.AreEqual(6, updateCustom.children.Count());

            var expectedOrder = new [] { PositionShapeBase.Type.OrientedBox, PositionShapeBase.Type.Sphere, PositionShapeBase.Type.Cone, PositionShapeBase.Type.Torus, PositionShapeBase.Type.Line, PositionShapeBase.Type.Circle };
            for (int i = 0; i < 6; ++i)
            {
                var expectedShapeType = expectedOrder[i];
                var randomBlock = updateRandom[i] as PositionShape;
                var customBlock = updateCustom[i] as PositionShape;

                Assert.IsNotNull(randomBlock);
                Assert.IsNotNull(customBlock);

                Assert.AreEqual(PositionBase.SpawnMode.Random, randomBlock.spawnMode);
                if (expectedShapeType != PositionShapeBase.Type.OrientedBox)
                    Assert.AreEqual(PositionBase.SpawnMode.Custom, customBlock.spawnMode);
                Assert.AreEqual(expectedShapeType, customBlock.GetSettingValue("shape"));

                if (expectedShapeType == PositionShapeBase.Type.Sphere ||
                    expectedShapeType == PositionShapeBase.Type.Torus)
                {
                    var heightSequencer = customBlock.inputSlots.FirstOrDefault(o => o.name == "heightSequencer");
                    Assert.IsNotNull(heightSequencer);
                    Assert.IsTrue(heightSequencer.HasLink());

                    var owner = heightSequencer.LinkedSlots.First().owner;
                    Assert.IsTrue(owner is Operator.Random);

                    var random = owner as Operator.Random;
                    Assert.IsFalse(random.constant);
                }
            }
        }

        [Test]
        public void Sanitize_MeshSampling_To_OutOfExperimental_MeshSampling()
        {
            string kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/MeshSampling/VFXSanitizeMeshSampling.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            graph.PrepareGraph();

            Assert.AreEqual(2, graph.children.OfType<VFXBasicUpdate>().Count());
            Assert.AreEqual(4, graph.children.OfType<Operator.SampleMesh>().Count());

            var basicUpdateInLocal = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.space == VFXSpace.Local);
            var basicUpdateInWorld = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault(o => o.space == VFXSpace.World);

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

            Assert.AreEqual(VFXSpace.Local, positionMeshLocal.inputSlots.Last().space);
            Assert.AreEqual(VFXSpace.Local, positionSkinnedLocal.inputSlots.Last().space);
            Assert.AreEqual(VFXSpace.World, positionMeshWorld.inputSlots.Last().space);
            Assert.AreEqual(VFXSpace.World, positionSkinnedWorld.inputSlots.Last().space);

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
            var kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/ShapeMigration/VFXSanitizeTShape.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            yield return null;

            Assert.AreEqual(8, graph.children.OfType<VFXParameter>().Count());
            Assert.AreEqual(11, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Count());
            Assert.AreEqual(0, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Where(o => o.position == Vector2.zero).Count());
            Assert.AreEqual(0, graph.children.OfType<VFXParameter>().SelectMany(o => o.nodes).Where(o => !o.linkedSlots.Any()).Count());
            yield return null;

            VFXTestCommon.GetWindow((VFXGraph)null, true)
                .LoadAsset(graph.GetResource().asset, null);
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

        [Test]
        public void Sanitize_SpaceNone_IntMaxValue_To_Minus_One()
        {
            var kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/SpaceMigration/VFXSpaceNoneMigration.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);

            Assert.AreEqual(8, graph.children.OfType<VFXInlineOperator>().Count());
            Assert.AreEqual(2, graph.children.OfType<Operator.Add>().Count());
            Assert.AreEqual(1, graph.children.OfType<VFXBasicUpdate>().Count());
            Assert.AreEqual(1, graph.children.OfType<VFXBasicInitialize>().Count());

            var positions = graph.children.OfType<VFXInlineOperator>().Where(o => o.inputSlots[0] is VFXSlotPosition).ToArray();
            Assert.AreEqual(3, positions.Length);
            Assert.AreEqual(1, positions.Count(o => o.inputSlots[0].space == VFXSpace.Local));
            Assert.AreEqual(1, positions.Count(o => o.inputSlots[0].space == VFXSpace.World));
            Assert.AreEqual(1, positions.Count(o => o.inputSlots[0].space == VFXSpace.None));

            var adds = graph.children.OfType<Operator.Add>().ToArray();
            var addLocalAndNone = adds.FirstOrDefault(o => o.inputSlots[0].space == VFXSpace.Local && o.inputSlots[1].space == VFXSpace.None);
            var addNoneAndNone = adds.FirstOrDefault(o => o != addLocalAndNone);
            Assert.IsNotNull(addLocalAndNone);
            Assert.IsNotNull(addNoneAndNone);

            Assert.AreEqual(VFXSpace.Local, addLocalAndNone.inputSlots[0].space);
            Assert.AreEqual(VFXSpace.None, addLocalAndNone.inputSlots[1].space);
            Assert.AreEqual(VFXSpace.Local, addLocalAndNone.outputSlots[0].space);

            Assert.AreEqual(VFXSpace.None, addNoneAndNone.inputSlots[0].space);
            Assert.AreEqual(VFXSpace.None, addNoneAndNone.inputSlots[1].space);
            Assert.AreEqual(VFXSpace.None, addNoneAndNone.outputSlots[0].space);

            var basicUpdate = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault();
            Assert.IsNotNull(basicUpdate);
            Assert.AreEqual(3, basicUpdate.children.Count());

            Assert.AreEqual(VFXSpace.Local, basicUpdate.children.ElementAt(0).inputSlots[0].space);
            Assert.AreEqual(VFXSpace.World, basicUpdate.children.ElementAt(1).inputSlots[0].space);
            Assert.AreEqual(VFXSpace.None, basicUpdate.children.ElementAt(2).inputSlots[0].space);

            var basicInitialize = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            Assert.IsNotNull(basicInitialize);
            Assert.AreEqual(3, basicInitialize.children.Count());
            Assert.IsTrue(basicInitialize.children.SelectMany(o => o.inputSlots).All(o => o.HasLink()));

            Assert.AreEqual(VFXSpace.World, basicInitialize.children.ElementAt(0).inputSlots[0].space);
            Assert.AreEqual(VFXSpace.Local, basicInitialize.children.ElementAt(1).inputSlots[0].space);
            Assert.AreEqual(VFXSpace.None, basicInitialize.children.ElementAt(2).inputSlots[0].space);

            var directions = graph.children.OfType<VFXInlineOperator>().Where(o => o.inputSlots[0] is VFXSlotDirection).ToArray();
            Assert.AreEqual(3, directions.Length);
            Assert.IsTrue(directions.All(o => o.outputSlots[0].HasLink()));
            Assert.AreEqual(1, directions.Count(o => o.inputSlots[0].space == VFXSpace.Local));
            Assert.AreEqual(1, directions.Count(o => o.inputSlots[0].space == VFXSpace.World));
            Assert.AreEqual(1, directions.Count(o => o.inputSlots[0].space == VFXSpace.None));
        }

        [Test, Description("Cover regression UUM-83598")]
        public void Sanitize_Custom_Attribute_With_Random()
        {
            var kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/CustomAttributes/Repro_UUM_83598.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            graph.PrepareGraph();
            Assert.IsNotNull(graph);

            var initialize = graph.GetGraph().children.OfType<VFXBasicInitialize>().Single();
            Assert.IsNotNull(initialize);

            Assert.AreEqual(2u, initialize.children.Count());
            Assert.AreEqual(2u, initialize.children.OfType<SetAttribute>().Count());
            var sizeExpected = (string)initialize.children.First().GetSetting("attribute").value;
            var customExpected = (string)initialize.children.Last().GetSetting("attribute").value;
            Assert.AreEqual(VFXAttribute.Size.name, sizeExpected);
            Assert.AreEqual("CustomAttribute", customExpected);

            var customAttribute = graph.customAttributes.SingleOrDefault(o => o.attributeName == customExpected);
            Assert.IsNotNull(customAttribute);

            foreach (var block in initialize.children)
            {
                var random = (RandomMode)block.GetSetting("Random").value;
                Assert.AreEqual(RandomMode.Uniform, random);
                Assert.AreEqual(2u, block.inputSlots.Count);
                Assert.AreEqual("A", block.inputSlots[0].name);
                Assert.AreEqual("B", block.inputSlots[1].name);
                Assert.AreEqual(typeof(float), block.inputSlots[0].property.type);
                Assert.AreEqual(typeof(float), block.inputSlots[1].property.type);
                Assert.AreEqual(0.0f, block.inputSlots[0].value);
                Assert.AreEqual(1.0f, block.inputSlots[1].value);
            }
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
    }

}
