using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(AdditionalShadowCaster2D))]
    [CanEditMultipleObjects]
    internal class AdditionalShadowCaster2DEditor : ShadowCaster2DEditor
    {
        [EditorTool("Edit Shadow Caster Shape", typeof(AdditionalShadowCaster2D))]
        class AdditionalShadowCasterShapeTool : ShadowCaster2DShapeTool { };

        public void OnEnable()
        {
            ShadowCaster2DOnEnable();
        }

        public void OnSceneGUI()
        {
            ShadowCaster2DSceneGUI();
        }

        public override void OnInspectorGUI()
        {
            ShadowCaster2DInspectorGUI<AdditionalShadowCasterShapeTool>();
        }
    }
}
