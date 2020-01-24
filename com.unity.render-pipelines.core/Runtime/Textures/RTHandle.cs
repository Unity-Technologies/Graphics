using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A RTHandle is a RenderTexture that scales automatically with the camera size.
    /// This allows proper reutilization of RenderTexture memory when different cameras with various sizes are used during rendering.
    /// <seealso cref="RTHandleSystem"/>
    /// </summary>
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

        /// <summary>
        /// Scale factor applied to the RTHandle reference size.
        /// </summary>
        public Vector2 scaleFactor { get; internal set; }
        internal ScaleFunc scaleFunc;

        /// <summary>
        /// Returns true if the RTHandle uses automatic scaling.
        /// </summary>
        public bool                         useScaling { get; internal set; }
        /// <summary>
        /// Reference size of the RTHandle System associated with the RTHandle
        /// </summary>
        public Vector2Int                   referenceSize {get; internal set; }
        /// <summary>
        /// Current properties of the RTHandle System
        /// </summary>
        public RTHandleProperties           rtHandleProperties { get { return m_Owner.rtHandleProperties; } }
        /// <summary>
        /// RenderTexture associated with the RTHandle
        /// </summary>
        public RenderTexture rt { get { return m_RT; } }
        /// <summary>
        /// RenderTargetIdentifier associated with the RTHandle
        /// </summary>
        public RenderTargetIdentifier nameID { get { return m_NameID; } }
        /// <summary>
        /// Name of the RTHandle
        /// </summary>
        public string name { get { return m_Name; } }

        // Keep constructor private
        internal RTHandle(RTHandleSystem owner)
        {
            m_Owner = owner;
        }

        /// <summary>
        /// Implicit conversion operator to RenderTexture
        /// </summary>
        /// <param name="handle">Input RTHandle</param>
        /// <returns>RenderTexture representation of the RTHandle.</returns>
        public static implicit operator RenderTexture(RTHandle handle)
        {
            Debug.Assert(handle.rt != null, "RTHandle was created using a regular Texture and is used as a RenderTexture");
            return handle.rt;
        }

        /// <summary>
        /// Implicit conversion operator to Texture
        /// </summary>
        /// <param name="handle">Input RTHandle</param>
        /// <returns>Texture representation of the RTHandle.</returns>
        public static implicit operator Texture(RTHandle handle)
        {
            Debug.Assert(handle.m_ExternalTexture != null || handle.rt != null);
            return (handle.rt != null) ? handle.rt : handle.m_ExternalTexture;
        }

        /// <summary>
        /// Implicit conversion operator to RenderTargetIdentifier
        /// </summary>
        /// <param name="handle">Input RTHandle</param>
        /// <returns>RenderTargetIdentifier representation of the RTHandle.</returns>
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

        /// <summary>
        /// Release the RTHandle
        /// </summary>
        public void Release()
        {
            m_Owner.Remove(this);
            CoreUtils.Destroy(m_RT);
            m_NameID = BuiltinRenderTextureType.None;
            m_RT = null;
            m_ExternalTexture = null;
        }

        /// <summary>
        /// Return the input size, scaled by the RTHandle scale factor.
        /// </summary>
        /// <param name="refSize">Input size</param>
        /// <returns>Input size scaled by the RTHandle scale factor.</returns>
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
