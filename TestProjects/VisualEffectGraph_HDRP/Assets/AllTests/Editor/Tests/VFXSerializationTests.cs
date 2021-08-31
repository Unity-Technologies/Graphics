#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.VFX.Block.Test;
using UnityEngine.VFX;
using UnityEditor.VFX;


using Object = UnityEngine.Object;
using System.IO;
using UnityEngine.TestTools;
using System.Collections;
using UnityEditor.VFX.UI;

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
        [UnityTest]
        public IEnumerable Verify_Orphan_Dependencies_Are_Correctly_Cleared()
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
            yield return null;
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

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
    }
}
#endif
