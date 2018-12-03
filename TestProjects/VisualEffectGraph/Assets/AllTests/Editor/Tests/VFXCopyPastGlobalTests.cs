#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
#if false
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using UnityEditor;
using UnityEngine.TestTools;
using System.Linq;
using UnityEditor.VFX.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VFX;
using UnityEditor.VFX.Block;


namespace UnityEditor.VFX.Test
{
    public class VFXCopyPastGlobalTests
    {
        string tempFilePath = "Assets/TmpTests/vfxTest.vfx";
        VisualEffectAsset m_Asset;
        VFXViewController m_ViewController;
        VFXView m_View;

        VFXGraph MakeTemporaryGraph()
        {
            m_Asset = VisualEffectResource.CreateNewAsset(tempFilePath);
            VisualEffectResource resource = m_Asset.GetResource(); // force resource creation
            VFXGraph graph = ScriptableObject.CreateInstance<VFXGraph>();
            graph.visualEffectResource = resource;

            var window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
            window = EditorWindow.GetWindow<VFXViewWindow>();
            m_ViewController = VFXViewController.GetController(m_Asset.GetResource(), true);
            m_View = window.graphView;
            m_View.controller = m_ViewController;

            return graph;
        }

        [TearDown]
        public void CleanUp()
        {
            AssetDatabase.DeleteAsset(tempFilePath);
            var window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
        }

        public struct CutBefore
        {
            internal VFXTaskType taskType;
            public override string ToString()
            {
                return taskType.ToString();
            }
        }

        private static CutBefore[] cutBeforeSource = new CutBefore[]
        {
        new CutBefore() { taskType = VFXTaskType.Spawner },
        new CutBefore() { taskType = VFXTaskType.Initialize },
        new CutBefore() { taskType = VFXTaskType.Update },
        new CutBefore() { taskType = VFXTaskType.Output },
        };
        [UnityTest]
        public IEnumerator CopyPast_Context_And_Relink([ValueSource("cutBeforeSource")] CutBefore cutBeforeEncapsultor)
        {
            VFXTaskType curBeforeSource = cutBeforeEncapsultor.taskType;
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            var graph = MakeTemporaryGraph();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var basicInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var basicUpdate = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var quadOutput = ScriptableObject.CreateInstance<VFXQuadOutput>();

            var arrayOfContext = new VFXContext[] { spawnerContext, basicInitialize, basicUpdate, quadOutput };
            quadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Additive);

            var setPosition = ScriptableObject.CreateInstance<SetAttribute>(); //only needed to allocate a minimal attributeBuffer
            setPosition.SetSettingValue("attribute", "position");
            basicInitialize.AddChild(setPosition);

            var capacity = 2u;
            //(basicInitialize.GetData() as VFXDataParticle).capacity = capacity; //voluntary overflow in case of capacity is not correctly copied

            basicInitialize.SetSettingValue("capacity", 2u); // pass through regular way to change capacity to have initialize and data up to date
            var spawnerBurst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            spawnerBurst.inputSlots[0].value = 4.0f;

            spawnerContext.AddChild(spawnerBurst);
            graph.AddChild(spawnerContext);
            graph.AddChild(basicInitialize);
            graph.AddChild(basicUpdate);
            graph.AddChild(quadOutput);
            basicInitialize.LinkFrom(spawnerContext);
            basicUpdate.LinkFrom(basicInitialize);
            quadOutput.LinkFrom(basicUpdate);

            m_ViewController.NotifyUpdate();
            Assert.AreEqual(4, m_View.GetAllContexts().Count());

            //Copy partially in two passes
            var indexOffset = cutBeforeSource.Select((o, i) => new { o = o, i = i }).Where(o => o.o.taskType == curBeforeSource).Select(o => o.i).First();
            var contextToCopy_A = arrayOfContext.Skip(indexOffset).Select(o => m_View.GetAllContexts().Where(u => u.controller.model == o).First() as UnityEditor.Experimental.GraphView.GraphElement);
            foreach (var graphElement in contextToCopy_A)
                m_View.AddToSelection(graphElement);
            m_View.DuplicateSelectionCallback();
            m_View.ClearSelection();
            m_ViewController.NotifyUpdate();

            if (indexOffset > 0)
            {
                var contextToCopy_B = arrayOfContext.Take(indexOffset).Select(o => m_View.GetAllContexts().Where(u => u.controller.model == o).First() as UnityEditor.Experimental.GraphView.GraphElement);
                foreach (var graphElement in contextToCopy_B)
                    m_View.AddToSelection(graphElement);
                m_View.DuplicateSelectionCallback();
                m_View.ClearSelection();
            }

            m_ViewController.NotifyUpdate();
            Assert.AreEqual(8, m_View.GetAllContexts().Count());

            //Restore missing link
            var allContext = m_View.GetAllContexts().Select(o => o.controller.model).ToArray();
            if (indexOffset > 0)
            {
                var from = allContext.Last();
                var to = allContext[4];
                to.LinkFrom(from);
            }

            VFXBasicInitialize[] initializes = graph.children.OfType<VFXBasicInitialize>().ToArray();

            Assert.AreEqual(2, initializes.Length);
            Assert.AreEqual(2, (initializes[0].GetData() as VFXDataParticle).capacity);
            Assert.AreEqual(2, (initializes[1].GetData() as VFXDataParticle).capacity);
            Assert.AreEqual(2, initializes[0].GetType().GetField("capacity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(initializes[0]));
            Assert.AreEqual(2, initializes[1].GetType().GetField("capacity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(initializes[1]));

            graph.RecompileIfNeeded();

            var gameObj = new GameObject("CreateAssetAndComponentToCopyPastPerfomedWell");
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            var cameraObj = new GameObject("CreateAssetAndComponentToCopyPastPerfomedWell_Camera");
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

            Assert.AreEqual(capacity * 2, vfxComponent.aliveParticleCount); //Excepted to have two viable equivalent particles system
            UnityEngine.Object.DestroyImmediate(vfxComponent);
            UnityEngine.Object.DestroyImmediate(cameraObj);
        }
    }
}
#endif
#endif
