using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Remapper")]
    public class RemapMasterNode : AbstractMasterNode
        , IOnAssetEnabled
    {
        [SerializeField]
        private string m_SerialziedRemapGraph = string.Empty;

        [Serializable]
        private class RemapGraphHelper
        {
            public MaterialRemapAsset subGraph;
        }

        public override bool allowedInRemapGraph
        {
            get { return false; }
        }
        
        public override string GetFullShader(GenerationMode mode, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            var shaderTemplateLocation = ShaderGenerator.GetTemplatePath("shader.template");

            if (remapAsset == null || !File.Exists(shaderTemplateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }
            
            var shaderPropertiesVisitor = new PropertyGenerator();

            // Step 1: Set this node as the remap target
            // Pass in the shader properties visitor here as
            // high level properties are shared
            // this is only used for the header
            var subShaders = remapAsset.masterRemapGraph.GetSubShadersFor(this, mode, shaderPropertiesVisitor);

            var templateText = File.ReadAllText(shaderTemplateLocation);
            var resultShader = templateText.Replace("${ShaderName}", GetType() + guid.ToString());
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            if (subShaders != null)
                resultShader = resultShader.Replace("${SubShader}", subShaders.Aggregate(string.Empty, (i, j) => i + Environment.NewLine + j));
            else
                resultShader = resultShader.Replace("${SubShader}", string.Empty);

            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();
            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        public override string GetSubShader(GenerationMode mode, PropertyGenerator shaderPropertiesVisitor)
        {
            throw new NotImplementedException();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            if (remapAsset == null)
                return;

            remapAsset.masterRemapGraph.CollectPreviewMaterialProperties(properties);
        }

#if UNITY_EDITOR
        public MaterialRemapAsset remapAsset
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerialziedRemapGraph))
                    return null;

                var helper = new RemapGraphHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerialziedRemapGraph, helper);
                return helper.subGraph;
            }
            set
            {
                if (remapAsset == value)
                    return;
                    
                var helper = new RemapGraphHelper();
                helper.subGraph = value;
                m_SerialziedRemapGraph = EditorJsonUtility.ToJson(helper, true);
                OnEnable();

                if (onModified != null)
                    onModified(this, ModificationScope.Graph);
            }
        }
#else
        public MaterialSubGraphAsset subGraphAsset {get; set; }
#endif
        
        public override PreviewMode previewMode
        {
            get
            {
                if (remapAsset == null)
                    return PreviewMode.Preview2D;

                return PreviewMode.Preview3D;
            }
        }

        public RemapMasterNode()
        {
            name = "Remapper";
        }

        public void OnEnable()
        {
            var validNames = new List<int>();
            if (remapAsset == null)
            {
                RemoveSlotsNameNotMatching(validNames);
                return;
            }

            var inputNode = remapAsset.masterRemapGraph.inputNode;
            foreach (var slot in inputNode.GetOutputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.id, slot.displayName, slot.shaderOutputName, SlotType.Input, slot.valueType, slot.defaultValue));
                validNames.Add(slot.id);
            }
            RemoveSlotsNameNotMatching(validNames);
        }
    }
}
