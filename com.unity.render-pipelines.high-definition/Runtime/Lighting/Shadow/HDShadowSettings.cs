using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class HDShadowSettings : VolumeComponent
    {
        float[] m_CascadeShadowSplits = new float[3];
        float[] m_CascadeShadowBorders = new float[4];

        public float[] cascadeShadowSplits
        {
            get
            {
                m_CascadeShadowSplits[0] = cascadeShadowSplit0;
                m_CascadeShadowSplits[1] = cascadeShadowSplit1;
                m_CascadeShadowSplits[2] = cascadeShadowSplit2;
                return m_CascadeShadowSplits;
            }
        }

        public float[] cascadeShadowBorders
        {
            get
            {
                m_CascadeShadowBorders[0] = cascadeShadowBorder0;
                m_CascadeShadowBorders[1] = cascadeShadowBorder1;
                m_CascadeShadowBorders[2] = cascadeShadowBorder2;
                m_CascadeShadowBorders[3] = cascadeShadowBorder3;

                // For now we don't use shadow cascade borders but we still want to have the last split fading out.
                if (!LightLoop.s_UseCascadeBorders)
                {
                    m_CascadeShadowBorders[cascadeShadowSplitCount - 1] = 0.2f;
                }
                return m_CascadeShadowBorders;
            }
        }

        [Tooltip("Sets the maximum distance HDRP renders shadows for all Light types.")]
        public NoInterpMinFloatParameter        maxShadowDistance = new NoInterpMinFloatParameter(500.0f, 0.0f);

        [Tooltip("Controls the number of cascades HDRP uses for cascaded shadow maps.")]
        public NoInterpClampedIntParameter      cascadeShadowSplitCount = new NoInterpClampedIntParameter(4, 1, 4);
        [Tooltip("Sets the position of the first cascade split as a percentage of Max Distance.")]
        public NoInterpClampedFloatParameter    cascadeShadowSplit0 = new NoInterpClampedFloatParameter(0.05f, 0.0f, 1.0f);
        [Tooltip("Sets the position of the second cascade split as a percentage of Max Distance.")]
        public NoInterpClampedFloatParameter    cascadeShadowSplit1 = new NoInterpClampedFloatParameter(0.15f, 0.0f, 1.0f);
        [Tooltip("Sets the position of the third cascade split as a percentage of Max Distance.")]
        public NoInterpClampedFloatParameter    cascadeShadowSplit2 = new NoInterpClampedFloatParameter(0.3f, 0.0f, 1.0f);
        [Tooltip("Sets the border size between the first and second cascade split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder0 = new NoInterpMinFloatParameter(0.0f, 0.0f);
        [Tooltip("Sets the border size between the second and third cascade split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder1 = new NoInterpMinFloatParameter(0.0f, 0.0f);
        [Tooltip("Sets the border size between the third and last cascade split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder2 = new NoInterpMinFloatParameter(0.0f, 0.0f);
        [Tooltip("Sets the border size at the end of the last cascade split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder3 = new NoInterpMinFloatParameter(0.0f, 0.0f);
    }
}
