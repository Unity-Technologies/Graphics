using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class NodePreviewPresenter : ScriptableObject
    {
        protected NodePreviewPresenter()
        {}

        MaterialGraphPreviewGenerator m_PreviewGenerator;

        [NonSerialized]
        int m_LastShaderVersion = -1;

        [NonSerialized]
        Material m_PreviewMaterial;

        [NonSerialized]
        Shader m_PreviewShader;

        AbstractMaterialNode m_Node;

        [SerializeField]
        ModificationScope m_ModificationScope;

        // Null means no modification is currently in progress.
        public ModificationScope modificationScope
        {
            get
            {
                return m_ModificationScope;
            }
            set
            {
                // External changes can only set the modification scope higher, to prevent missing out on a previously set shader regeneration.
                if (m_ModificationScope >= value)
                    return;
                m_ModificationScope = value;
            }
        }

        [NonSerialized]
        Texture m_Texture;

        public Texture texture
        {
            get { return m_Texture; }
        }

        PreviewMode m_GeneratedShaderMode = PreviewMode.Preview2D;

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

        MaterialGraphPreviewGenerator previewGenerator
        {
            get
            {
                if (m_PreviewGenerator == null)
                {
                    m_PreviewGenerator = new MaterialGraphPreviewGenerator(m_Node is AbstractMasterNode);
                }
                return m_PreviewGenerator;
            }
        }

        public void Initialize(AbstractMaterialNode node)
        {
            m_Node = node;
            m_ModificationScope = ModificationScope.Graph;
        }

        public void UpdateTexture()
        {
            if (m_Node == null)
            {
                m_Texture = null;
                return;
            }

            if (m_Node.hasPreview == false)
            {
                m_Texture = null;
                return;
            }

            var scope = m_ModificationScope;
            m_ModificationScope = ModificationScope.Nothing;

            if (scope != ModificationScope.Nothing)
            {
                if (scope >= ModificationScope.Graph)
                {
                    var stage = NodeUtils.FindEffectiveShaderStage(m_Node, true);
                    if (!(m_Node is AbstractSurfaceMasterNode) && stage == ShaderStage.Vertex)
                    {
//                        Debug.Log(m_Node.name + ":" + stage);
                        m_Texture = null;
                        return;
                    }
                    // TODO: Handle shader regeneration error
                    var status = UpdatePreviewShader();
                }
                m_Texture = RenderPreview(new Vector2(256, 256));
            }
            else if (texture == null)
            {
                m_Texture = RenderPreview(new Vector2(256, 256));
            }
        }

        void OnDisable()
        {
            if (m_PreviewGenerator != null)
                m_PreviewGenerator.Dispose();
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
                var resultShader =  ((IMasterNode)m_Node).GetFullShader(GenerationMode.Preview, m_Node.guid + "_preview", out defaultTextures);

                if (((IMasterNode)m_Node).has3DPreview())
                {
                    m_GeneratedShaderMode = PreviewMode.Preview3D;
                }

                return resultShader;
            }
            return ShaderGenerator.GeneratePreviewShader(m_Node, out m_GeneratedShaderMode);
        }

        bool UpdatePreviewShader()
        {
            if (m_Node == null || m_Node.hasError)
                return false;

            var resultShader = GetPreviewShaderString();
            Debug.Log("RecreateShaderAndMaterial : " + m_Node.GetVariableNameForNode() + Environment.NewLine + resultShader);
            string shaderOuputString = resultShader.Replace("UnityEngine.MaterialGraph", "Generated");
            System.IO.File.WriteAllText(Application.dataPath + "/../GeneratedShader.shader", shaderOuputString);

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
        Texture RenderPreview(Vector2 targetSize)
        {
            previewMaterial.shader = m_PreviewShader;
            UpdateMaterialProperties(m_Node, previewMaterial);
            return previewGenerator.DoRenderPreview(previewMaterial, m_GeneratedShaderMode, new Rect(0, 0, targetSize.x, targetSize.y));
        }

        static void SetPreviewMaterialProperty(PreviewProperty previewProperty, Material mat)
        {
            switch (previewProperty.m_PropType)
            {
                case PropertyType.Texture:
                    mat.SetTexture(previewProperty.m_Name, previewProperty.m_Texture);
                    break;
                case PropertyType.Cubemap:
                    mat.SetTexture(previewProperty.m_Name, previewProperty.m_Cubemap);
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
