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
            var localNode = node as PixelShaderNode;
            if (localNode == null)
                return base.Render(area);

            var lightFunctions = PixelShaderNode.GetLightFunctions();
			var lightFunction = localNode.lightFunction.GetType();

            int lightFuncIndex = 0;
            if (lightFunction != null)
				lightFuncIndex = lightFunctions.Select(x => x.GetType()).ToList().IndexOf(lightFunction);

            EditorGUI.BeginChangeCheck();
            lightFuncIndex = EditorGUI.Popup(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight), lightFuncIndex, lightFunctions.Select(x => x.lightFunctionName).ToArray(), EditorStyles.popup);
            localNode.lightFunction = lightFunctions[lightFuncIndex];
            var toReturn = GUIModificationType.None;
            if (EditorGUI.EndChangeCheck())
            {
               localNode.UpdateNodeAfterDeserialization();
               toReturn = GUIModificationType.ModelChanged;
            }
            area.y += EditorGUIUtility.singleLineHeight;
            area.height -= EditorGUIUtility.singleLineHeight;
            toReturn |= base.Render(area);
            return toReturn;
        } 

        protected override string GetPreviewShaderString()
        {
            var localNode = node as PixelShaderNode;
            if (localNode == null)
                return string.Empty;

            var shaderName = "Hidden/PreviewShader/" + localNode.GetVariableNameForNode();
            List<PropertyGenerator.TextureInfo> defaultTextures;
            //TODO: Need to get the real options somehow
            var resultShader = ShaderGenerator.GenerateSurfaceShader(localNode, new MaterialOptions(), shaderName, true, out defaultTextures);
            generatedShaderMode = PreviewMode.Preview3D;
            return resultShader;
        }
    }
}
