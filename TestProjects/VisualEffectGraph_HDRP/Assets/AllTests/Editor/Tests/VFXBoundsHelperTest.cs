using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX.Test
{
    public class VFXBoundsHelperTest
    {

#pragma warning disable 0414
        private static VFXCoordinateSpace[] availableSpaces = { VFXCoordinateSpace.Local, VFXCoordinateSpace.World };
#pragma warning restore 0414

        private readonly Vector3 m_Translation = new Vector3(1.0f, 2.0f, 3.0f);
        private readonly Quaternion m_Rotation = Quaternion.Euler(20.0f, 30.0f, 40.0f);
        private readonly Vector3 m_Scale = new Vector3(3.0f, 2.0f, 1.0f);
        private readonly Vector3 m_ParticleSize = 0.1f * Vector3.one;

        [UnityTest]
        public IEnumerator TestBoundsHelperResults([ValueSource(nameof(availableSpaces))] object systemSpace)
        {
            string kSourceAsset = "Assets/AllTests/Editor/Tests/VFXBoundsHelperTest.vfx";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            VFXCoordinateSpace space = (VFXCoordinateSpace)systemSpace;
            graph.children.OfType<VFXBasicInitialize>().First().space = space;

            var gameObj = new GameObject("GameObjectToCheck");
            gameObj.transform.position = m_Translation;
            gameObj.transform.rotation = m_Rotation;
            gameObj.transform.localScale = m_Scale;

            var vfxComponent = gameObj.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = graph.visualEffectResource.asset;

            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            VFXViewController controller = VFXViewController.GetController(vfxComponent.visualEffectAsset.GetResource(), true);
            view.controller = controller;
            view.attachedComponent = vfxComponent;

            VFXBoundsRecorder boundsRecorder = new VFXBoundsRecorder(vfxComponent, view);

            boundsRecorder.ToggleRecording();
            vfxComponent.Simulate(1.0f/60.0f);
            const int maxFrameTimeout = 100;
            for (int i = 0; i < maxFrameTimeout; i++)
            {
                boundsRecorder.UpdateBounds();
                if (boundsRecorder.bounds.Count > 0)
                    break;
                yield return null; //skip a frame.
            }
            boundsRecorder.ToggleRecording();
            Bounds bounds = boundsRecorder.bounds.FirstOrDefault().Value;
            boundsRecorder.CleanUp();

            Vector3 expectedCenter = Vector3.zero;
            Vector3 expectedExtent = new Vector3(2.0f,2.0f,2.0f);
            expectedExtent += 0.5f * m_ParticleSize * (space == VFXCoordinateSpace.Local
                ? Mathf.Sqrt(3.0f)
                : Mathf.Sqrt(1.0f / Mathf.Pow(m_Scale.x,2) + 1.0f / Mathf.Pow(m_Scale.y,2) + 1.0f /  Mathf.Pow(m_Scale.z,2)));

            Assert.AreEqual(expectedCenter.x, bounds.center.x, .002);
            Assert.AreEqual(expectedCenter.y, bounds.center.y, .002);
            Assert.AreEqual(expectedCenter.z, bounds.center.z, .002);
            Assert.AreEqual(expectedExtent.x, bounds.extents.x, .005);
            Assert.AreEqual(expectedExtent.y, bounds.extents.y, .005);
            Assert.AreEqual(expectedExtent.z, bounds.extents.z, .005);

            view.attachedComponent = null;
            window.Close();

            yield return null;
        }
    }

}
