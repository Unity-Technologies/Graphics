using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public partial class RTHandleSystem
    {
        public class RTHandle
        {
            internal RTHandleSystem             m_Owner;
            internal RenderTexture[]            m_RTs = new RenderTexture[2];
            internal RenderTargetIdentifier[]   m_NameIDs = new RenderTargetIdentifier[2];
            internal bool                       m_EnableMSAA = false;
            internal bool                       m_EnableRandomWrite = false;
            internal string                     m_Name;

            internal Vector2 scaleFactor        = Vector2.one;
            internal ScaleFunc scaleFunc;

            public bool                         useScaling { get; internal set; }
            public Vector2Int                   referenceSize {get; internal set; }

            public RenderTexture rt
            {
                get
                {
                    if (!useScaling)
                    {
                        return m_EnableMSAA ? m_RTs[(int)RTCategory.MSAA] : m_RTs[(int)RTCategory.Regular];
                    }
                    else
                    {
                        var category = (m_EnableMSAA && m_Owner.m_ScaledRTCurrentCategory == RTCategory.MSAA) ? RTCategory.MSAA : RTCategory.Regular;
                        CreateIfNeeded(category);
                        return m_RTs[(int)category];
                    }
                }
            }

            public RenderTargetIdentifier nameID
            {
                get
                {
                    if (!useScaling)
                    {
                        return m_EnableMSAA ? m_NameIDs[(int)RTCategory.MSAA] : m_RTs[(int)RTCategory.Regular];
                    }
                    else
                    {
                        var category = (m_EnableMSAA && m_Owner.m_ScaledRTCurrentCategory == RTCategory.MSAA) ? RTCategory.MSAA : RTCategory.Regular;
                        CreateIfNeeded(category);
                        return m_NameIDs[(int)category];
                    }
                }
            }

            // Keep constructor private
            internal RTHandle(RTHandleSystem owner)
            {
                m_Owner = owner;
            }

            public static implicit operator RenderTexture(RTHandle handle)
            {
                return handle.rt;
            }

            public static implicit operator RenderTargetIdentifier(RTHandle handle)
            {
                return handle.nameID;
            }

            internal void SetRenderTexture(RenderTexture rt, RTCategory category)
            {
                m_RTs[(int)category] = rt;
                m_NameIDs[(int)category] = new RenderTargetIdentifier(rt);
            }

            void CreateIfNeeded(RTCategory category)
            {
                // If a RT was first created for MSAA then the regular one might be null, in this case we create it.
                // That's why we never test the MSAA version: It should always be there if RT was declared correctly.
                if (category == RTCategory.Regular && m_RTs[(int)RTCategory.Regular] == null)
                {
                    var refRT = m_RTs[(int)RTCategory.MSAA];
                    Debug.Assert(refRT != null);
                    referenceSize = new Vector2Int(m_Owner.maxWidthRegular, m_Owner.maxHeightRegular);
                    var scaledSize = GetScaledSize(referenceSize);

                    var newRT = new RenderTexture(scaledSize.x, scaledSize.y, refRT.depth, refRT.format, refRT.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        volumeDepth = refRT.volumeDepth,
                        filterMode = refRT.filterMode,
                        wrapMode = refRT.wrapMode,
                        dimension = refRT.dimension,
                        enableRandomWrite = m_EnableRandomWrite, // We cannot take the info from the msaa rt since we force it to 1
                        useMipMap = refRT.useMipMap,
                        autoGenerateMips = refRT.autoGenerateMips,
                        anisoLevel = refRT.anisoLevel,
                        mipMapBias = refRT.mipMapBias,
                        antiAliasing = 1, // No MSAA for the regular version of the texture.
                        bindTextureMS = false, // Somehow, this can be true even if antiAliasing == 1. Leads to Unity-internal binding errors.
                        useDynamicScale = refRT.useDynamicScale,
                        vrUsage = refRT.vrUsage,
                        memorylessMode = refRT.memorylessMode,
                        name = CoreUtils.GetRenderTargetAutoName(refRT.width, refRT.height, refRT.volumeDepth, refRT.format, m_Name, mips: refRT.useMipMap)
                    };
                    newRT.Create();

                    m_RTs[(int)RTCategory.Regular] = newRT;
                    m_NameIDs[(int)RTCategory.Regular] = new RenderTargetIdentifier(newRT);
                }
            }

            public void Release()
            {
                m_Owner.m_AutoSizedRTs.Remove(this);
                for (int i = 0; i < (int)RTCategory.Count; ++i)
                {
                    CoreUtils.Destroy(m_RTs[i]);
                    m_NameIDs[i] = BuiltinRenderTextureType.None;
                    m_RTs[i] = null;
                }
            }

            public Vector2Int GetScaledSize(Vector2Int refSize)
            {
                if (scaleFunc != null)
                {
                    return scaleFunc(refSize);
                }
                else
                {
                    return new Vector2Int(
                        x: Mathf.RoundToInt(scaleFactor.x * refSize.x),
                        y: Mathf.RoundToInt(scaleFactor.y * refSize.y)
                        );
                }
            }
        }
    }
}
