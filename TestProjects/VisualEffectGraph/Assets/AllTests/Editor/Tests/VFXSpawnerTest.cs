using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using System.Linq;
using UnityEditor.VFX.UI;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    public class VFXSpawnerTest
    {
        [UnityTest]
        public IEnumerator CreateAssetAndComponentSpawner()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockBurst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            var slotCount = blockBurst.GetInputSlot(0);
            var slotDelay = blockBurst.GetInputSlot(1);

            var spawnCountValue = 753.0f;
            slotCount.value = new Vector2(spawnCountValue, spawnCountValue);
            slotDelay.value = new Vector2(0.0f, 0.0f);

            spawnerContext.AddChild(blockBurst);
            graph.AddChild(spawnerContext);

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateAssetAndComponentSpawner");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            while (vfxComponent.culled)
            {
                yield return null;
            }
            yield return null; //wait for exactly one more update if visible

            var spawnerState = vfxComponent.GetSpawnerState(0);
            Assert.AreEqual(spawnCountValue, spawnerState.spawnCount);

            UnityEngine.Object.DestroyImmediate(gameObj);
        }

        [UnityTest]
        public IEnumerator CreateEventAttributeAndStart()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockBurst = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();

            spawnerContext.AddChild(blockBurst);
            graph.AddChild(spawnerContext);

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateEventAttributeAndStart");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            var lifeTimeIn = 28.0f;
            var vfxEventAttr = vfxComponent.CreateVFXEventAttribute();
            vfxEventAttr.SetFloat("lifeTime", lifeTimeIn);
            vfxComponent.Start(vfxEventAttr);

            while (vfxComponent.culled)
            {
                yield return null;
            }
            yield return null; //wait for exactly one more update if visible

            var spawnerState = vfxComponent.GetSpawnerState(0);
            var lifeTimeOut = spawnerState.vfxEventAttribute.GetFloat("lifeTime");
            Assert.AreEqual(lifeTimeIn, lifeTimeOut);

            UnityEngine.Object.DestroyImmediate(gameObj);
        }

        [UnityTest]
        public IEnumerator CreateCustomSpawnerAndComponent()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockCustomSpawner = ScriptableObject.CreateInstance<VFXSpawnerCustomWrapper>();
            blockCustomSpawner.Init(typeof(VFXCustomSpawnerTest));

            spawnerContext.AddChild(blockCustomSpawner);
            graph.AddChild(spawnerContext);

            var valueTotalTime = 187.0f;

            blockCustomSpawner.GetInputSlot(0).value = valueTotalTime;

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateAssetAndComponentSpawner");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            while (vfxComponent.culled)
            {
                yield return null;
            }
            yield return null; //wait for exactly one more update if visible

            var spawnerState = vfxComponent.GetSpawnerState(0);
            Assert.GreaterOrEqual(spawnerState.totalTime, valueTotalTime);
            Assert.AreEqual(VFXCustomSpawnerTest.s_LifeTime, spawnerState.vfxEventAttribute.GetFloat("lifeTime"));
            Assert.AreEqual(VFXCustomSpawnerTest.s_SpawnCount, spawnerState.spawnCount);
        }
    }
}
