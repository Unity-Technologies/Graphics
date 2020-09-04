using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for the default cloud layer material
    /// </summary>
    class DefaultCloudLayerGUI : ShaderGUI
    {
		override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
		{
			materialEditor.SetDefaultGUIWidths();
			
            for (var i = 0; i < props.Length; i++)
            {
                if ((props[i].flags & MaterialProperty.PropFlags.HideInInspector) != 0)
                    continue;

                float h = materialEditor.GetPropertyHeight(props[i], props[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                materialEditor.ShaderProperty(r, props[i], props[i].displayName);
            }
		}
    }
} // namespace UnityEditor
