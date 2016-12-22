using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.NodeInspectors
{
    public class SurfaceMasterNodeInspector : BasicNodeInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var surfaceNode = node as AbstractSurfaceMasterNode;
            if (surfaceNode == null)
                return;

            var options = surfaceNode.options;

            options.srcBlend = (SurfaceMaterialOptions.BlendMode) EditorGUILayout.EnumPopup("Src Blend", options.srcBlend);
            options.dstBlend = (SurfaceMaterialOptions.BlendMode) EditorGUILayout.EnumPopup("Dst Blend", options.dstBlend);
            options.cullMode = (SurfaceMaterialOptions.CullMode) EditorGUILayout.EnumPopup("Cull Mode", options.cullMode);
            options.zTest = (SurfaceMaterialOptions.ZTest) EditorGUILayout.EnumPopup("Z Test", options.zTest);
            options.zWrite = (SurfaceMaterialOptions.ZWrite) EditorGUILayout.EnumPopup("Z Write", options.zWrite);
            options.renderQueue = (SurfaceMaterialOptions.RenderQueue) EditorGUILayout.EnumPopup("Render Queue", options.renderQueue);
            options.renderType = (SurfaceMaterialOptions.RenderType) EditorGUILayout.EnumPopup("Render Type", options.renderType);
        }
    }
}