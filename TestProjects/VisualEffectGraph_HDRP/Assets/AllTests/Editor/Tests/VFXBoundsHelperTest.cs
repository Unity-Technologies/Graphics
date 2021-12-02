using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX.Test
{
    public class VFXBoundsHelperTest
    {

#pragma warning disable 0414
        private static VFXCoordinateSpace[] available_Space = { VFXCoordinateSpace.Local, VFXCoordinateSpace.World };
#pragma warning restore 0414

        Vector3 expectedCenter =  new Vector3(0,1,0);
        Vector3 expectedExtent =  new Vector3(4.14f,4.14f,4.14f);
        [UnityTest]
        public IEnumerator TestBoundsHelperResults([ValueSource(nameof(available_Space))] object systemSpace, [ValueSource(nameof(available_Space))] object boundSpace)
        {
            string kSourceAsset = "Assets/AllTests/Editor/Tests/VFXBoundsHelperTest.vfx";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);

            graph.children.OfType<VFXBasicInitialize>().First().space = (VFXCoordinateSpace)systemSpace;

            var gameObj = new GameObject("GameObjectToCheck");
            gameObj.transform.position = new Vector3(0,1,0);
            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            VFXViewController controller = VFXViewController.GetController(vfxComponent.visualEffectAsset.GetResource(), true);
            view.controller = controller;
            view.attachedComponent = vfxComponent;

            VFXBoundsRecorder boundsRecorder = new VFXBoundsRecorder(vfxComponent, view);

            boundsRecorder.ToggleRecording();
            vfxComponent.Simulate(1.0f/60.0f, 10u);
            boundsRecorder.UpdateBounds();
            boundsRecorder.ToggleRecording();
            Bounds bounds = boundsRecorder.bounds.FirstOrDefault().Value;
            boundsRecorder.CleanUp();

            Assert.AreEqual(expectedCenter.y, bounds.center.y, .01);
            Debug.Log("Bounds : " + bounds  );
            view.attachedComponent = null;
            window.Close();

            yield return null;
        }
    }

}
