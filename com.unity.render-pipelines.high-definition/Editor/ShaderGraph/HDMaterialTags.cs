using System;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [Serializable]
    class HDMaterialTags
    {
        public enum RenderType
        {
            HDLitShader,    // For Lit, LayeredLit, LitTesselation, LayeredLitTesselation
            HDUnlitShader,  // Unlit
            Opaque,         // Used by Terrain
        }

        [SerializeField]
        private RenderType m_RenderType = RenderType.Opaque;

        [SerializeField]
        private int m_RenderQueueIndex = 0;

        public int renderQueueIndex { get { return m_RenderQueueIndex; } set { m_RenderQueueIndex = value; } }
        public RenderType renderType { get { return m_RenderType; } set { m_RenderType = value; } }

        public void Init()
        {
            renderQueueIndex = (int)HDRenderQueue.Priority.Opaque;
            renderType = RenderType.Opaque;
        }

        public void GetTags(ShaderStringBuilder builder, string pipeline)
        {
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                builder.AppendLine("\"RenderPipeline\"=\"{0}\"", pipeline);
                builder.AppendLine("\"RenderType\"=\"{0}\"", renderType);

                // We can't use a number inside the Queue tag so we convert it to RenderingQueue+number
                builder.AppendLine("\"Queue\"=\"{0}\"", GetRenderQueueTag(renderQueueIndex));
            }
        }

        // Note: all these tags make no sense for HDRP but are required by shader queue tags (as it doesn't accept int values)
        // So we convert our HDRenderQueue.Priority to a format that unity's shader understand (i.e: "Geometry+X")
        string GetRenderQueueTag(int index)
        {
            // Special case for transparent (as we have transparent range from PreRefractionFirst to AfterPostprocessTransparentLast
            // that start before RenderQueue.Transparent value
            if (HDRenderQueue.k_RenderQueue_AllTransparent.Contains(index)
                || HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent.Contains(index))
            {
                int v = (index - (int)RenderQueue.Transparent);
                return "Transparent" + ((v < 0) ? "" : "+") + v;
            }
            else if (index >= (int)RenderQueue.Overlay)
                return "Overlay+" + (index - (int)RenderQueue.Overlay);
            else if (index >= (int)RenderQueue.AlphaTest)
                return "AlphaTest+" + (index - (int)RenderQueue.AlphaTest);
            else if (index >= (int)RenderQueue.Geometry)
                return "Geometry+" + (index - (int)RenderQueue.Geometry);
            else
            {
                int v = (index - (int)RenderQueue.Background);
                return "Background" + ((v < 0) ? "" : "+") + v;
            }
        }
    }
}
