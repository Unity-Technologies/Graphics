using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    internal class ShadowCaster2DEditor : PathComponentEditor<ScriptablePath>
    {
        public void ShadowCaster2DOnEnable()
        {
            
        }

        public void ShadowCaster2DSceneGUI()
        {
            ShadowCaster2D shadowCaster = target as ShadowCaster2D;

            Transform t = shadowCaster.transform;
            Vector3[] shape = shadowCaster.shapePath;
            Handles.color = Color.white;

            for (int i = 0; i < shape.Length-1; ++i)
            {
                //Handles.DrawLine(t.TransformPoint(shape[i]), t.TransformPoint(shape[i + 1]));
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[i]), t.TransformPoint(shape[i + 1]) });
            }

            if(shape.Length > 1)
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[shape.Length - 1]), t.TransformPoint(shape[0]) });
        }

        public void ShadowCaster2DInspectorGUI<T>() where T : ShadowCaster2DShapeTool
        {
            DoEditButton<T>(PathEditorToolContents.icon, "Edit Shape");
            DoPathInspector<T>();
            DoSnappingInspector<T>();
        }
    }
}
