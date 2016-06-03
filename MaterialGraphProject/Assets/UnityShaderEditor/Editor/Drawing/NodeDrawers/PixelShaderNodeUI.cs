using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{

    [CustomNodeUI(typeof(PixelShaderNode))]
    public class PixelShaderNodeUI : AbstractMaterialNodeUI
    {
        public override float GetNodeWidth()
        {
            return 300;
        }

        public override float GetNodeUiHeight(float width)
        {
            return base.GetNodeUiHeight(width) + EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType Render(Rect area)
        {
            var node = m_Node as PixelShaderNode;
            if (node == null)
                return base.Render(area);

            var lightFunctions = PixelShaderNode.GetLightFunctions();
            var lightFunction = node.GetLightFunction();

            int lightFuncIndex = 0;
            if (lightFunction != null)
                lightFuncIndex = lightFunctions.IndexOf(lightFunction);

            EditorGUI.BeginChangeCheck();
            lightFuncIndex = EditorGUI.Popup(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight), lightFuncIndex, lightFunctions.Select(x => x.GetLightFunctionName()).ToArray(), EditorStyles.popup);
            node.m_LightFunctionClassName = lightFunctions[lightFuncIndex].GetType().ToString();
            var toReturn = GUIModificationType.None;
            if (EditorGUI.EndChangeCheck())
            {
               node.UpdateNodeAfterDeserialization();
               toReturn = GUIModificationType.ModelChanged;
            }
            area.y += EditorGUIUtility.singleLineHeight;
            area.height -= EditorGUIUtility.singleLineHeight;
            toReturn |= base.Render(area);
            return toReturn;
        } 

        protected override string GetPreviewShaderString()
        {
            var shaderName = "Hidden/PreviewShader/" + m_Node.GetVariableNameForNode();
            List<PropertyGenerator.TextureInfo> defaultTextures;
            //TODO: Need to get the real options somehow
            var resultShader = ShaderGenerator.GenerateSurfaceShader(m_Node as PixelShaderNode, new MaterialOptions(), shaderName, true, out defaultTextures);
            m_GeneratedShaderMode = PreviewMode.Preview3D;
            return resultShader;
        }
    }
}
