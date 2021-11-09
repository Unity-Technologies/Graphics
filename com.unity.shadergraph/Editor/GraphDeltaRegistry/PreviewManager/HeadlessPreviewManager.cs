using UnityEngine;
using System;
using System.Collections.Generic;
using Editor.GraphDeltaRegistry.Utils;
using JetBrains.Annotations;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    /// <summary>
    /// This class encapsulates all functionality related to generating preview render and shader data from a node graph
    /// </summary>
    public class HeadlessPreviewManager
    {
        // Could we replace this by a bool?
        // We would need to handle concretization of preview mode on GTF side of things
        // prior to asking preview manager to issue updates for all downstream affected nodes
        public enum PreviewRenderMode
        {
            Inherit,   // this usually means: 2D, unless a connected input node is 3D, in which case it is 3D
            Preview2D,
            Preview3D
        }

        class PreviewData
        {
            public string Name;
            public Shader shader;
            public Material materialPropertyBlock;
            public string shaderString;
            public Texture texture;

            // Do we need to cache the render texture?
            public RenderTexture renderTexture;

            // Do we need to track how many passes are actively compiled per shader? What is it used for beyond debug log stuff?
            public int passesCompiling;
            // Same for this stuff below...
            public bool isOutOfDate;
            public bool hasError;
        }

        public HeadlessPreviewManager()
        {
            m_ErrorTexture = GenerateFourSquare(Color.magenta, Color.black);
            m_SceneResources = new PreviewSceneResources();
            m_MasterPreviewData = new PreviewData();
        }

         static Texture2D GenerateFourSquare(Color c1, Color c2)
         {
             var tex = new Texture2D(2, 2);
             tex.SetPixel(0, 0, c1);
             tex.SetPixel(0, 1, c2);
             tex.SetPixel(1, 0, c2);
             tex.SetPixel(1, 1, c1);
             tex.filterMode = FilterMode.Point;
             tex.Apply();
             return tex;
         }

        /// <summary>
        /// Map from node names to associated preview data object
        /// </summary>
        Dictionary<string, PreviewData> m_CachedPreviewData;

        /// <summary>
        /// Handle to the graph object we are currently generating preview data for
        /// </summary>
        IGraphHandler m_GraphHandle;

        MaterialPropertyBlock m_PreviewMaterialPropertyBlock = new ();

        Texture2D m_ErrorTexture;

        PreviewSceneResources m_SceneResources;

        PreviewData m_MasterPreviewData;

        /// <summary>
        /// Used to set which graph this preview manager gets its data from.
        /// </summary>
        public void SetActiveGraph(IGraphHandler activeGraphReference)
        {
            m_GraphHandle = activeGraphReference;
        }

        /// <summary>
        /// Used to change the propertyValue of global properties like Time, Blackboard Properties, Render Pipeline Intrinsics etc.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview render state of all nodes downstream of any references to the changed property </remarks>
        public List<string> SetGlobalProperty(string propertyName, object newPropertyValue)
		{
			return null;
		}

        /// <summary>
        /// Used to change the propertyValue of local properties such as node inputs/port propertyValues.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview render state of all nodes downstream of any references to the changed property </remarks>
        public List<string> SetLocalProperty(string nodeName, string portName, object newPropertyValue)
		{
			return null;
		}

        /// <summary>
        /// Used to notify when a node has been deleted and for when connections to a node change.
        /// </summary>
        /// <returns> List of names describing all nodes that were affected by this change </returns>
        /// <remarks> Dirties the preview compile & render state of all nodes downstream of the changed node </remarks>
        public List<string> NotifyNodeFlowChanged(string nodeName)
		{
			return null;
		}

        /// <summary>
        /// Used to get current preview render output of a node, and optionally at a specific preview mode provided as an argument.
        /// </summary>
        /// <returns> Texture that contains the current preview output of a node, if its shaders have been compiled and ready to return </returns>
        // Could return an enum/bool as to the state of the requested texture, and then a nullable out parameter for the actual texture result
        public bool RequestNodePreviewImage(string nodeName, [CanBeNull] out Texture nodeRenderOutput, PreviewRenderMode nodePreviewMode = PreviewRenderMode.Preview2D)
		{
            if (m_CachedPreviewData.ContainsKey(nodeName))
            {
                // Check if shaders and render output is ready, and return if so

            }
            else
            {
                AddNodePreviewData(nodeName);
                nodeRenderOutput = Texture2D.blackTexture;
                return false;
            }
		}

        /// <summary>
        /// Used to get preview materialPropertyBlockerial associated with a node.
        /// </summary>
        /// <returns> Material that describes the current preview shader and render output of a node </returns>
        public Material RequestNodePreviewMaterial(string nodeName)
		{
			return null;
		}

        /// <summary>
        /// Used to get preview shader code associated with a node.
        /// </summary>
        /// <returns> Current preview shader generated by a node </returns>
        public string RequestNodePreviewShaderCode(string nodeName)
		{
			return null;
		}

        /// <summary>
        /// Used to get preview materialPropertyBlockerial associated with the final output of the active graph.
        /// </summary>
        /// <returns> Material that describes the current preview shader and render output of a graph </returns>
        public Material RequestMasterPreviewMaterial()
		{
			return null;
		}

        /// <summary>
        /// Used to get preview shader code associated with the final output of the active graph.
        /// </summary>
        /// <returns> Current preview shader code generated by the active graph </returns>
        public string RequestMasterPreviewShaderCode()
		{
			return null;
		}

        void AddNodePreviewData(string nodeName)
        {
            var renderData = new PreviewData
            {
                Name = nodeName,
                renderTexture =
                    new RenderTexture(200, 200, 16, RenderTextureFormaterialPropertyBlock.ARGB32, RenderTextureReadWrite.Default)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    }
            };

            var nodeReader = m_GraphHandle.GetNodeReader(nodeName);
            var nodeInputPorts = nodeReader.GetInputPorts();
            foreach (var inputPort in nodeInputPorts)
            {

            }


            m_CachedPreviewData.Add(nodeName, renderData);
        }

        enum DefaultTextureType
        {
            White,
            Black,
            NormalMap
        }

        DefaultTextureType Mock_GetDefaultTextureType(IPortReader portReader)
        {
            return DefaultTextureType.White;
        }

        object Mock_GetPortValue(IPortReader portReader)
        {
            return null;
        }

        void SetValueOnMaterialPropertyBlock(MaterialPropertyBlock materialPropertyBlock, string propertyName, IPortReader portReader)
        {
            var propertyValue = portReader.TryGetField(propertyName, );
            var type = propertyValue.GetType();

            if ((type == typeof(Texture2D) /*|| propertyType == PropertyType.Texture2DArray*/ || type == typeof(Texture3D)))
            {
                if (propertyValue == null)
                {
                    // there's no way to set the texture back to NULL
                    // and no way to delete the property either
                    // so instead we set the propertyValue to what we know the default will be
                    // (all textures in ShaderGraph default to white)

                    DefaultTextureType defaultTextureType = Mock_GetDefaultTextureType(m_PortReader);

                    switch (defaultTextureType)
                    {
                        case DefaultTextureType.White:
                            materialPropertyBlock.SetTexture(Name, Texture2D.whiteTexture);
                            break;
                        case DefaultTextureType.Black:
                            materialPropertyBlock.SetTexture(Name, Texture2D.blackTexture);
                            break;
                        case DefaultTextureType.NormalMap:
                            materialPropertyBlock.SetTexture(Name, Texture2D.normalTexture);
                            break;
                    }
                }
                else
                {
                    var textureValue = propertyValue as Texture;
                    materialPropertyBlock.SetTexture(Name, textureValue);
                }
            }
            else if (type == typeof(Cubemap))
            {
                if (propertyValue == null)
                {
                    // there's no Cubemap.whiteTexture, but this seems to work
                    materialPropertyBlock.SetTexture(Name, Texture2D.whiteTexture);
                }
                else
                {
                    var cubemapValue = propertyValue as Cubemap;
                    materialPropertyBlock.SetTexture(Name, cubemapValue);
                }
            }
            else if (type == typeof(Color))
            {
                var colorValue = propertyValue is Color colorVal ? colorVal : default;
                materialPropertyBlock.SetColor(Name, colorValue);
            }
            else if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4))
            {
                var vector4Value = propertyValue is Vector4 vector4Val ? vector4Val : default;
                materialPropertyBlock.SetVector(Name, vector4Value);
            }
            else if (type == typeof(float))
            {
                var floatValue = propertyValue is float floatVal ? floatVal : default;
                materialPropertyBlock.SetFloat(Name, floatValue);
            }
            else if (type == typeof(Boolean))
            {
                var boolValue = propertyValue is Boolean boolVal ? boolVal : default;
                materialPropertyBlock.SetFloat(Name, boolValue ? 1 : 0);
            }
            // TODO: How to handle Matrix2/Matrix3 types?
            // Will probably be registry defined, how will we compare against them?
            else if (type == typeof(Matrix4x4)/*propertyType == PropertyType.Matrix2 || propertyType == PropertyType.Matrix3 || */)
            {
                var materialPropertyBlockrixValue = propertyValue is Matrix4x4 materialPropertyBlockrixVal ? materialPropertyBlockrixVal : default;
                materialPropertyBlock.SetMatrix(Name, materialPropertyBlockrixValue);
            }
            else if (type == typeof(Gradient))
            {
                var gradientValue = propertyValue as Gradient;
                materialPropertyBlock.SetFloat(string.FormaterialPropertyBlock("{0}_Type", Name), (int)gradientValue.mode);
                materialPropertyBlock.SetFloat(string.FormaterialPropertyBlock("{0}_ColorsLength", Name), gradientValue.colorKeys.Length);
                materialPropertyBlock.SetFloat(string.FormaterialPropertyBlock("{0}_AlphasLength", Name), gradientValue.alphaKeys.Length);
                for (int i = 0; i < 8; i++)
                    materialPropertyBlock.SetVector(string.FormaterialPropertyBlock("{0}_ColorKey{1}", Name, i), i < gradientValue.colorKeys.Length ? GradientUtil.ColorKeyToVector(gradientValue.colorKeys[i]) : Vector4.zero);
                for (int i = 0; i < 8; i++)
                    materialPropertyBlock.SetVector(string.FormaterialPropertyBlock("{0}_AlphaKey{1}", Name, i), i < gradientValue.alphaKeys.Length ? GradientUtil.AlphaKeyToVector(gradientValue.alphaKeys[i]) : Vector2.zero);
            }
            // TODO: Virtual textures handling
            /*else if (type == typeof(VirtualTexture))
            {
                // virtual texture assignments are not supported via the materialPropertyBlockerial property block, we must assign them to the materialPropertyBlockerials
            }*/
        }
    }
}
