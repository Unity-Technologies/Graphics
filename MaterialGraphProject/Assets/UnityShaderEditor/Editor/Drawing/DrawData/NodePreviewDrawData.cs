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
    public class NodePreviewDrawData : GraphElementData
    {
        protected NodePreviewDrawData()
        {}

        private MaterialGraphPreviewGenerator m_PreviewGenerator;

        [NonSerialized]
        private int m_LastShaderVersion = -1;

        [NonSerialized]
        private Material m_PreviewMaterial;

        [NonSerialized]
        private Shader m_PreviewShader;

        private AbstractMaterialNode m_Node;

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
        }

        public Texture Render(Vector2 dimension)
        {
            if (m_Node == null)
                return null;

            if (m_Node.hasPreview == false)
                return null;

            if (m_LastShaderVersion != m_Node.version)
            {
                if (UpdatePreviewShader())
                    m_LastShaderVersion = m_Node.version;
            }

            return RenderPreview(dimension);
        }

        protected virtual string GetPreviewShaderString()
        {
            // TODO: this is a workaround right now.
            if (m_Node is PixelShaderNode)
            {
                var localNode = (PixelShaderNode)m_Node;
                if (localNode == null)
                    return string.Empty;

                var shaderName = "Hidden/PreviewShader/" + localNode.GetVariableNameForNode();
                List<PropertyGenerator.TextureInfo> defaultTextures;
                //TODO: Need to get the real options somehow
                var resultShader = ShaderGenerator.GenerateSurfaceShader(localNode, new MaterialOptions(), shaderName, true, out defaultTextures);
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
            if (m_PreviewShader && ShaderHasError(m_PreviewShader))
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

            return !ShaderHasError(m_PreviewShader);
        }

        public static bool ShaderHasError(Shader shader)
        {
            var hasErrorsCall = typeof(ShaderUtil).GetMethod("GetShaderErrorCount", BindingFlags.Static | BindingFlags.NonPublic);
            var result = hasErrorsCall.Invoke(null, new object[] {shader});
            return (int)result != 0;
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
