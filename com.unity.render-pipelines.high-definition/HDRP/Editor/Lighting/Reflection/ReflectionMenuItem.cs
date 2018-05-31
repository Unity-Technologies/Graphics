using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class ReflectionMenuItems
    {
        [MenuItem("GameObject/Rendering/Planar Reflection", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreatePlanarReflectionGameObject(MenuCommand menuCommand)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObjectUtility.SetParentAndAlign(plane, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(plane, "Create " + plane.name);
            Selection.activeObject = plane;

            var planarProbe = plane.AddComponent<PlanarReflectionProbe>();
            planarProbe.influenceVolume.boxBaseSize = new Vector3(10, 0.01f, 10);

            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            var material = hdrp != null ? hdrp.GetDefaultMirrorMaterial() : null;

            if (material)
            {
                plane.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
    }
}
