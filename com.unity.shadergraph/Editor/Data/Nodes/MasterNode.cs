using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MasterNode<T> : AbstractMaterialNode, IMasterNode, IHasSettings
        where T : JsonObject, ISubShader
    {
        [SerializeField]
        JsonList<T> m_SubShaders = new JsonList<T>();

        [SerializeField]
        int m_MasterNodeVersion;

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

        public Type supportedSubshaderType
        {
            get { return typeof(T); }
        }

        public IEnumerable<T> subShaders => m_SubShaders;

        public void AddSubShader(T subshader)
        {
            if (m_SubShaders.Any(x => ReferenceEquals(x, subshader)))
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
            foreach (var subShader in subShaders)
            {
                if (subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                    return subShader;
            }
            return null;
        }

        public string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
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
                owner.AddValidationError(this, ShaderKeyword.kVariantLimitWarning, Rendering.ShaderCompilerMessageSeverity.Error);

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

                foreach (var subShader in subShaders)
                {
                    if (mode != GenerationMode.Preview || subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                        finalShader.AppendLines(subShader.GetSubshader(this, mode, sourceAssetDependencyPaths));
                }

                finalShader.AppendLine(@"FallBack ""Hidden/InternalErrorShader""");
            }
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.ToString();
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            foreach (var subShader in subShaders)
            {
                if (subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                    return true;
            }
            return false;
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

        public virtual void ProcessPreviewMaterial(Material material) {}

        internal override void OnDeserialized(string json)
        {
            if (m_MasterNodeVersion == 0)
            {
                m_MasterNodeVersion = 1;
                var masterNodeV0 = JsonUtility.FromJson<MasterNodeV0>(json);
                SerializationHelper.Upgrade(masterNodeV0.subShaders, m_SubShaders);
            }
        }
    }
}
