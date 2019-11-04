using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    public class RTHandle
    {
        internal RTHandleSystem             m_Owner;
        internal RenderTexture              m_RT;
        internal Texture                    m_ExternalTexture;
        internal RenderTargetIdentifier     m_NameID;
        internal bool                       m_EnableMSAA = false;
        internal bool                       m_EnableRandomWrite = false;
        internal bool                       m_EnableHWDynamicScale = false;
        internal string                     m_Name;

        public Vector2 scaleFactor { get; internal set; }
        internal ScaleFunc scaleFunc;

        public bool                         useScaling { get; internal set; }
        public Vector2Int                   referenceSize {get; internal set; }

        public RTHandleProperties           rtHandleProperties { get { return m_Owner.rtHandleProperties; } }

        public RenderTexture rt { get { return m_RT; } }

        public RenderTargetIdentifier nameID { get { return m_NameID; } }

        public string name { get { return m_Name; } }

        // Keep constructor private
        internal RTHandle(RTHandleSystem owner)
        {
            m_Owner = owner;
        }

        public static implicit operator RenderTexture(RTHandle handle)
        {
            Debug.Assert(handle.rt != null, "RTHandle was created using a regular Texture and is used as a RenderTexture");
            return handle.rt;
        }

        public static implicit operator Texture(RTHandle handle)
        {
            Debug.Assert(handle.m_ExternalTexture != null || handle.rt != null);
            return (handle.rt != null) ? handle.rt : handle.m_ExternalTexture;
        }

        public static implicit operator RenderTargetIdentifier(RTHandle handle)
        {
            return handle.nameID;
        }

        internal void SetRenderTexture(RenderTexture rt)
        {
            m_RT=  rt;
            m_ExternalTexture = null;
            m_NameID = new RenderTargetIdentifier(rt);
        }

        internal void SetTexture(Texture tex)
        {
            m_RT = null;
            m_ExternalTexture = tex;
            m_NameID = new RenderTargetIdentifier(tex);
        }

        public void Release()
        {
            m_Owner.Remove(this);
            CoreUtils.Destroy(m_RT);
            m_NameID = BuiltinRenderTextureType.None;
            m_RT = null;
            m_ExternalTexture = null;
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
