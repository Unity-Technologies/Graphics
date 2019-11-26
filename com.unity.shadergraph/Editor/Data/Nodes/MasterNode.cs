using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    abstract class MasterNode : AbstractMaterialNode, IMasterNode, IHasSettings
    {
        public override bool hasPreview
        {
            get { return false; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public abstract string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null);
        public abstract bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        public abstract int GetPreviewPassIndex();

        public VisualElement CreateSettingsElement()
        {
            var container = new VisualElement();
            var commonSettingsElement = CreateCommonSettingsElement();
            if (commonSettingsElement != null)
                container.Add(commonSettingsElement);

            return container;
        }

        protected virtual VisualElement CreateCommonSettingsElement()
        {
            return null;
        }

        public virtual object saveContext => null;

        public virtual void ProcessPreviewMaterial(Material Material) {}
    }
    
    [Serializable]
    abstract class MasterNode<T> : MasterNode
        where T : class, ISubShader
    {
        [NonSerialized]
        List<T> m_SubShaders = new List<T>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSubShaders = new List<SerializationHelper.JSONSerializedElement>();

        public IEnumerable<T> subShaders => m_SubShaders;

        public void AddSubShader(T subshader)
        {
            if (m_SubShaders.Contains(subshader))
                return;

            m_SubShaders.Add(subshader);
            Dirty(ModificationScope.Graph);
        }

        public void RemoveSubShader(T subshader)
        {
            m_SubShaders.RemoveAll(x => x == subshader);
            Dirty(ModificationScope.Graph);
        }

        public ISubShader GetActiveSubShader()
        {
            foreach (var subShader in m_SubShaders)
            {
                if (subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                    return subShader;
            }
            return null;
        }

        public sealed override string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            var activeNodeList = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            var shaderProperties = new PropertyCollector();
            var shaderKeywords = new KeywordCollector();
            if (owner != null)
            {
                owner.CollectShaderProperties(shaderProperties, mode);
                owner.CollectShaderKeywords(shaderKeywords, mode);
            }

            if(owner.GetKeywordPermutationCount() > ShaderGraphPreferences.variantLimit)
            {
                owner.AddValidationError(tempId, ShaderKeyword.kVariantLimitWarning, Rendering.ShaderCompilerMessageSeverity.Error);
                
                configuredTextures = shaderProperties.GetConfiguredTexutres();
                return ShaderGraphImporter.k_ErrorShader;
            }

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, mode);

            var finalShader = new ShaderStringBuilder();
            finalShader.AppendLine(@"Shader ""{0}""", outputName);
            using (finalShader.BlockScope())
            {
                SubShaderGenerator.GeneratePropertiesBlock(finalShader, shaderProperties, shaderKeywords, mode);

                foreach (var subShader in m_SubShaders)
                {
                    if (mode != GenerationMode.Preview || subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                        finalShader.AppendLines(subShader.GetSubshader(this, mode, sourceAssetDependencyPaths));
                }

                finalShader.AppendLine(@"FallBack ""Hidden/Shader Graph/FallbackError""");
            }
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.ToString();
        }

        public sealed override bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            foreach (var subShader in m_SubShaders)
            {
                if (subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                    return true;
            }
            return false;
        }

        public sealed override int GetPreviewPassIndex()
        {
            return GetActiveSubShader()?.GetPreviewPassIndex() ?? 0;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableSubShaders = SerializationHelper.Serialize<T>(m_SubShaders);
        }

        public override void OnAfterDeserialize()
        {
            m_SubShaders = SerializationHelper.Deserialize<T>(m_SerializableSubShaders, GraphUtil.GetLegacyTypeRemapping());
            m_SubShaders.RemoveAll(x => x == null);
            m_SerializableSubShaders = null;
            base.OnAfterDeserialize();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypesOrNothing())
                {
                    var isValid = !type.IsAbstract && !type.IsGenericType && type.IsClass && typeof(T).IsAssignableFrom(type);
                    if (isValid && !subShaders.Any(s => s.GetType() == type))
                    {
                        try
                        {
                            var subShader = (T)Activator.CreateInstance(type);
                            AddSubShader(subShader);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }
    }
}
