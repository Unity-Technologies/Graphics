using System;
using System.Collections.Generic;
using System.Reflection;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class NodePreviewPresenter : GraphElementPresenter
    {
        protected NodePreviewPresenter()
        {}

        private MaterialGraphPreviewGenerator m_PreviewGenerator;

        [NonSerialized]
        private int m_LastShaderVersion = -1;

        [NonSerialized]
        private Material m_PreviewMaterial;

        [NonSerialized]
        private Shader m_PreviewShader;

        private AbstractMaterialNode m_Node;

        [NonSerialized]
        private ModificationScope m_modificationScope;

        // Null means no modification is currently in progress.
        public ModificationScope modificationScope
        {
            get
            {
                return m_modificationScope;
            }
            set
            {
                // External changes can only set the modification scope higher, to prevent missing out on a previously set shader regeneration.
                if (m_modificationScope >= value)
                    return;
                m_modificationScope = value;
            }
        }

        [NonSerialized]
        private Texture m_texture;

        private PreviewMode m_GeneratedShaderMode = PreviewMode.Preview2D;

        public Material previewMaterial
        {
            get
            {
                if (m_PreviewMaterial == null)
                {
                    m_PreviewMaterial = new Material(Shader.Find("Unlit/Color")) {hideFlags = HideFlags.HideInHierarchy};
                    m_PreviewMaterial.hideFlags = HideFlags.HideInHierarchy;
                }
                return m_PreviewMaterial;
            }
        }

        private MaterialGraphPreviewGenerator previewGenerator
        {
            get
            {
                if (m_PreviewGenerator == null)
                {
                    m_PreviewGenerator = new MaterialGraphPreviewGenerator();
                }
                return m_PreviewGenerator;
            }
        }

        public void Initialize(AbstractMaterialNode node)
        {
            m_Node = node;
            m_modificationScope = ModificationScope.Graph;
        }

        public Texture Render(Vector2 dimension)
        {
            if (m_Node == null)
                return null;

            if (m_Node.hasPreview == false)
                return null;

            if (m_modificationScope != ModificationScope.Nothing)
            {
                bool status = false;
                if (m_modificationScope >= ModificationScope.Graph)
                {
                    previewGenerator.Reset();

                    // TODO: Handle shader regeneration error
                    status = UpdatePreviewShader();
                }
                m_texture = RenderPreview(dimension);
                m_modificationScope = ModificationScope.Nothing;
            }
            else if (m_texture == null)
            {
                m_texture = RenderPreview(dimension);
            }

            return m_texture;
        }

        protected virtual string GetPreviewShaderString()
        {
            // TODO: this is a workaround right now.
            if (m_Node is IMasterNode)
            {
                var localNode = (IMasterNode)m_Node;
                if (localNode == null)
                    return string.Empty;

                List<PropertyGenerator.TextureInfo> defaultTextures;
                var resultShader =  ((IMasterNode) m_Node).GetFullShader(GenerationMode.Preview, out defaultTextures);
                m_GeneratedShaderMode = PreviewMode.Preview3D;
                return resultShader;
            }
            return ShaderGenerator.GeneratePreviewShader(m_Node, out m_GeneratedShaderMode);
        }

        private bool UpdatePreviewShader()
        {
            if (m_Node == null || m_Node.hasError)
                return false;

            var resultShader = GetPreviewShaderString();
            Debug.Log("RecreateShaderAndMaterial : " + m_Node.GetVariableNameForNode() + Environment.NewLine + resultShader);

            if (string.IsNullOrEmpty(resultShader))
                return false;

            // workaround for some internal shader compiler weirdness
            // if we are in error we sometimes to not properly clean
            // out the error flags and will stay in error, even
            // if we are now valid
            if (m_PreviewShader && MaterialGraphAsset.ShaderHasError(m_PreviewShader))
            {
                Object.DestroyImmediate(m_PreviewShader, true);
                m_PreviewShader = null;
            }

            if (m_PreviewShader == null)
            {
                m_PreviewShader = ShaderUtil.CreateShaderAsset(resultShader);
                m_PreviewShader.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                ShaderUtil.UpdateShaderAsset(m_PreviewShader, resultShader);
            }

            return !MaterialGraphAsset.ShaderHasError(m_PreviewShader);
        }

        /// <summary>
        ///     RenderPreview gets called in OnPreviewGUI. Nodes can override
        ///     RenderPreview and do their own rendering to the render texture
        /// </summary>
        private Texture RenderPreview(Vector2 targetSize)
        {
            previewMaterial.shader = m_PreviewShader;
            UpdateMaterialProperties(m_Node, previewMaterial);
            return previewGenerator.DoRenderPreview(previewMaterial, m_GeneratedShaderMode, new Rect(0, 0, targetSize.x, targetSize.y));
        }

        private static void SetPreviewMaterialProperty(PreviewProperty previewProperty, Material mat)
        {
            switch (previewProperty.m_PropType)
            {
                case PropertyType.Texture2D:
                    mat.SetTexture(previewProperty.m_Name, previewProperty.m_Texture);
                    break;
                case PropertyType.Color:
                    mat.SetColor(previewProperty.m_Name, previewProperty.m_Color);
                    break;
                case PropertyType.Vector2:
                    mat.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    break;
                case PropertyType.Vector3:
                    mat.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    break;
                case PropertyType.Vector4:
                    mat.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    break;
                case PropertyType.Float:
                    mat.SetFloat(previewProperty.m_Name, previewProperty.m_Float);
                    break;
            }
        }

        public static void UpdateMaterialProperties(AbstractMaterialNode target, Material material)
        {
            var childNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(childNodes, target);

            var pList = ListPool<PreviewProperty>.Get();
            for (var index = 0; index < childNodes.Count; index++)
            {
                var node = childNodes[index] as AbstractMaterialNode;
                if (node == null)
                    continue;

                node.CollectPreviewMaterialProperties(pList);
            }

            foreach (var prop in pList)
                SetPreviewMaterialProperty(prop, material);

            ListPool<INode>.Release(childNodes);
            ListPool<PreviewProperty>.Release(pList);
        }
    }
}
