using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    class ShadowCaster2DShapeTool : PathEditorTool<ScriptablePath>
    {
        const string k_ShapePath = "m_ShapePath";
 
        protected override IShape GetShape(Object target)
        {
            return (target as ShadowCaster2D).shapePath.ToPolygon(false);
        }

        protected override void SetShape(ScriptablePath shapeEditor, SerializedObject serializedObject)
        {
            serializedObject.Update();

            var pointsProperty = serializedObject.FindProperty(k_ShapePath);
            pointsProperty.arraySize = shapeEditor.pointCount;

            for (var i = 0; i < shapeEditor.pointCount; ++i)
                pointsProperty.GetArrayElementAtIndex(i).vector3Value = shapeEditor.GetPoint(i).position;

            // This is untracked right now...
            serializedObject.ApplyModifiedProperties();

            ShadowCaster2D shadowCaster = target as ShadowCaster2D;
            if (shadowCaster != null)
            {
                int hash = LightUtility.GetShapePathHash(shadowCaster.shapePath);
                shadowCaster.shapePathHash = hash;
            }
        }
    }
}
