#if UNITY_EDITOR && (!UNITY_EDITOR_OSX || MAC_FORCE_TESTS)
using System.Linq;
using System.Collections;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;

using UnityEditor.VFX.Block;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSpaceBoundTests
    {
        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

#pragma warning disable 0414
        private static VFXSpace[] available_Space = { VFXSpace.Local, VFXSpace.World };
#pragma warning restore 0414

        [UnityTest]
        public IEnumerator CreateAssetAndComponent_Space_Bounds([ValueSource("available_Space")] object systemSpace, [ValueSource("available_Space")] object boundSpace)
        {
            var objectPosition = new Vector3(0.123f, 0.0f, 0.0f);
            var boundPosition = new Vector3(0.0f, 0.0987f, 0.0f);
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();

            var basicInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var basicUpdate = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var quadOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
            quadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Additive);

            var setLifetime = ScriptableObject.CreateInstance<SetAttribute>(); //only needed to allocate a minimal attributeBuffer
            setLifetime.SetSettingValue("attribute", "lifetime");
            setLifetime.inputSlots[0].value = 1.0f;
            basicInitialize.AddChild(setLifetime);

            spawnerContext.AddChild(blockConstantRate);
            graph.AddChild(spawnerContext);
            graph.AddChild(basicInitialize);
            graph.AddChild(basicUpdate);
            graph.AddChild(quadOutput);
            basicInitialize.LinkFrom(spawnerContext);
            basicUpdate.LinkFrom(basicInitialize);
            quadOutput.LinkFrom(basicUpdate);

            basicInitialize.space = (VFXSpace)systemSpace;
            basicInitialize.inputSlots[0].space = (VFXSpace)boundSpace;
            basicInitialize.inputSlots[0][0].value = boundPosition;
            basicInitialize.inputSlots[0][1].value = Vector3.one * 5.0f;

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var gameObj = new GameObject("CreateAssetAndComponentToCheckBound");
            gameObj.transform.position = objectPosition;
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateAssetAndComponentToCheckBound_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(vfxComponent.transform);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var renderer = vfxComponent.GetComponent<VFXRenderer>();
            var parentFromCenter = VFXTestCommon.CollectParentExpression(basicInitialize.inputSlots[0][0].GetExpression()).ToArray();
            if ((VFXSpace)systemSpace == VFXSpace.Local && (VFXSpace)boundSpace == VFXSpace.Local)
            {
                Assert.IsFalse(parentFromCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
                Assert.AreEqual(boundPosition.x + objectPosition.x, renderer.bounds.center.x, 0.0001);
                Assert.AreEqual(boundPosition.y + objectPosition.y, renderer.bounds.center.y, 0.0001);
                Assert.AreEqual(boundPosition.z + objectPosition.z, renderer.bounds.center.z, 0.0001);
            }
            else if ((VFXSpace)systemSpace == VFXSpace.World && (VFXSpace)boundSpace == VFXSpace.Local)
            {
                Assert.IsFalse(parentFromCenter.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
                Assert.AreEqual(boundPosition.x + objectPosition.x, renderer.bounds.center.x, 0.0001);
                Assert.AreEqual(boundPosition.y + objectPosition.y, renderer.bounds.center.y, 0.0001);
                Assert.AreEqual(boundPosition.z + objectPosition.z, renderer.bounds.center.z, 0.0001);
            }
            else if ((VFXSpace)systemSpace == VFXSpace.World && (VFXSpace)boundSpace == VFXSpace.World)
            {
                Assert.IsTrue(parentFromCenter.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);
                //object position has no influence in that case
                Assert.AreEqual(boundPosition.x, renderer.bounds.center.x, 0.0001);
                Assert.AreEqual(boundPosition.y, renderer.bounds.center.y, 0.0001);
                Assert.AreEqual(boundPosition.z, renderer.bounds.center.z, 0.0001);
            }
            else if ((VFXSpace)systemSpace == VFXSpace.Local && (VFXSpace)boundSpace == VFXSpace.World)
            {
                Assert.IsTrue(parentFromCenter.Count(o => o.operation == VFXExpressionOperation.WorldToLocal) == 1);
                //object position has no influence in that case
                Assert.AreEqual(boundPosition.x, renderer.bounds.center.x, 0.0001);
                Assert.AreEqual(boundPosition.y, renderer.bounds.center.y, 0.0001);
                Assert.AreEqual(boundPosition.z, renderer.bounds.center.z, 0.0001);
            }
            else
            {
                //Unknown case, should not happen
                Assert.IsFalse(true);
            }
        }
    }
}
#endif
