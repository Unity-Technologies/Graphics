using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// This struct contains some static helpers that can be used when converting RTid to RThandle
    /// The common use case is to convert rtId to rtHandle and use the handle with other handle compatible core APIs
    /// </summary>
    public struct RTHandleStaticHelpers
    {
        /// <summary>
        /// Static RTHandle wrapper around RenderTargetIdentifier to avoid gc.alloc
        /// Set this wrapper through `RTHandleStaticHelpers.SetRTHandleStaticWrapper`
        /// </summary>
        public static RTHandle s_RTHandleWrapper;

        /// <summary>
        /// Set static RTHandle wrapper given a RTid. The static RTHandle wrapper is treated as external handle in RTHandleSystem
        /// Get the static wrapper through `RTHandleStaticHelpers.s_RTHandleWrapper`. 
        /// </summary>
        /// <param name="rtId">Input render target identifier to be converted.</param>
        public static void SetRTHandleStaticWrapper(RenderTargetIdentifier rtId)
        {
            if (s_RTHandleWrapper == null)
                s_RTHandleWrapper = RTHandles.Alloc(rtId);
            else
                s_RTHandleWrapper.SetTexture(rtId);
        }

        /// <summary>
        /// Set user managed RTHandle wrapper given a RTid. The wrapper is treated as external handle in RTHandleSystem 
        /// </summary>
        /// <param name="rtWrapper">User managed RTHandle wrapper.</param>
        /// <param name="rtId">Input render target identifier to be set.</param>
        public static void SetRTHandleUserManagedWrapper(ref RTHandle rtWrapper, RenderTargetIdentifier rtId)
        {
            // User managed wrapper is null, just return here.
            if (rtWrapper == null)
                return;
            
            // Check user managed RTHandle wrapper is actually a warpper around RTid
            if (rtWrapper.m_RT != null)
                throw new ArgumentException($"Input wrapper must be a wrapper around RenderTargetIdentifier. Passed in warpper contains valid RenderTexture {rtWrapper.m_RT.name} and cannot be used as warpper.");
            if (rtWrapper.m_ExternalTexture != null)
                throw new ArgumentException($"Input wrapper must be a wrapper around RenderTargetIdentifier. Passed in warpper contains valid Texture {rtWrapper.m_ExternalTexture.name} and cannot be used as warpper.");

            rtWrapper.SetTexture(rtId);
        }
    }

    /// <summary>
    /// A RTHandle is a RenderTexture that scales automatically with the camera size.
    /// This allows proper reutilization of RenderTexture memory when different cameras with various sizes are used during rendering.
    /// <seealso cref="RTHandleSystem"/>
    /// </summary>
    public class RTHandle
    {
        internal RTHandleSystem m_Owner;
        internal RenderTexture m_RT;
        internal Texture m_ExternalTexture;
        internal RenderTargetIdentifier m_NameID;
        internal bool m_EnableMSAA = false;
        internal bool m_EnableRandomWrite = false;
        internal bool m_EnableHWDynamicScale = false;
        internal string m_Name;

        internal bool m_UseCustomHandleScales = false;
        internal RTHandleProperties m_CustomHandleProperties;

        /// <summary>
        /// By default, rtHandleProperties gets the global state of scalers against the global reference mode.
        /// This method lets the current RTHandle use a local custom RTHandleProperties. This function is being used
        /// by scalers such as TAAU and DLSS, which require to have a different resolution for color (independent of the RTHandleSystem).
        /// </summary>
        /// <param name="properties">Properties to set.</param>
        public void SetCustomHandleProperties(in RTHandleProperties properties)
        {
            m_UseCustomHandleScales = true;
            m_CustomHandleProperties = properties;
        }

        /// <summary>
        /// Method that clears any custom handle property being set.
        /// </summary>
        public void ClearCustomHandleProperties()
        {
            m_UseCustomHandleScales = false;
        }

        /// <summary>
        /// Scale factor applied to the RTHandle reference size.
        /// </summary>
        public Vector2 scaleFactor { get; internal set; }
        internal ScaleFunc scaleFunc;

        /// <summary>
        /// Returns true if the RTHandle uses automatic scaling.
        /// </summary>
        public bool useScaling { get; internal set; }
        /// <summary>
        /// Reference size of the RTHandle System associated with the RTHandle
        /// </summary>
        public Vector2Int referenceSize { get; internal set; }
        /// <summary>
        /// Current properties of the RTHandle System. If a custom property has been set through SetCustomHandleProperties method, it will be used that one instead.
        /// </summary>
        public RTHandleProperties rtHandleProperties { get { return m_UseCustomHandleScales ? m_CustomHandleProperties : m_Owner.rtHandleProperties; } }
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

        /// <summary>
        /// Returns true is MSAA is enabled, false otherwise.
        /// </summary>
        public bool isMSAAEnabled { get { return m_EnableMSAA; } }

        // Keep constructor private
        internal RTHandle(RTHandleSystem owner)
        {
            m_Owner = owner;
        }

        /// <summary>
        /// Implicit conversion operator to RenderTargetIdentifier
        /// </summary>
        /// <param name="handle">Input RTHandle</param>
        /// <returns>RenderTargetIdentifier representation of the RTHandle.</returns>
        public static implicit operator RenderTargetIdentifier(RTHandle handle)
        {
            return handle != null ? handle.nameID : default(RenderTargetIdentifier);
        }

        /// <summary>
        /// Implicit conversion operator to Texture
        /// </summary>
        /// <param name="handle">Input RTHandle</param>
        /// <returns>Texture representation of the RTHandle.</returns>
        public static implicit operator Texture(RTHandle handle)
        {
            // If RTHandle is null then conversion should give a null Texture
            if (handle == null)
                return null;

            Debug.Assert(handle.m_ExternalTexture != null || handle.rt != null);
            return (handle.rt != null) ? handle.rt : handle.m_ExternalTexture;
        }

        /// <summary>
        /// Implicit conversion operator to RenderTexture
        /// </summary>
        /// <param name="handle">Input RTHandle</param>
        /// <returns>RenderTexture representation of the RTHandle.</returns>
        public static implicit operator RenderTexture(RTHandle handle)
        {
            // If RTHandle is null then conversion should give a null RenderTexture
            if (handle == null)
                return null;

            Debug.Assert(handle.rt != null, "RTHandle was created using a regular Texture and is used as a RenderTexture");
            return handle.rt;
        }

        internal void SetRenderTexture(RenderTexture rt)
        {
            m_RT = rt;
            m_ExternalTexture = null;
            m_NameID = new RenderTargetIdentifier(rt);
        }

        internal void SetTexture(Texture tex)
        {
            m_RT = null;
            m_ExternalTexture = tex;
            m_NameID = new RenderTargetIdentifier(tex);
        }

        internal void SetTexture(RenderTargetIdentifier tex)
        {
            m_RT = null;
            m_ExternalTexture = null;
            m_NameID = tex;
        }

        /// <summary>
        /// Get the Instance ID of the RTHandle.
        /// </summary>
        /// <returns>The RTHandle Instance ID.</returns>
        public int GetInstanceID()
        {
            if (m_RT != null)
                return m_RT.GetInstanceID();
            else if (m_ExternalTexture != null)
                return m_ExternalTexture.GetInstanceID();
            else
                return m_NameID.GetHashCode(); // No instance ID so we return the hash code.
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
            if (!useScaling)
                return refSize;

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

        /// <summary>
        /// Return the scaled size of the RTHandle.
        /// </summary>
        /// <returns>The scaled size of the RTHandle.</returns>
        public Vector2Int GetScaledSize()
        {
            if (!useScaling)
                return referenceSize;

            if (scaleFunc != null)
            {
                return scaleFunc(referenceSize);
            }
            else
            {
                return new Vector2Int(
                    x: Mathf.RoundToInt(scaleFactor.x * referenceSize.x),
                    y: Mathf.RoundToInt(scaleFactor.y * referenceSize.y)
                );
            }
        }

#if UNITY_2020_2_OR_NEWER
        /// <summary>
        /// Switch the render target to fast memory on platform that have it.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="residencyFraction">How much of the render target is to be switched into fast memory (between 0 and 1).</param>
        /// <param name="flags">Flag to determine what parts of the render target is spilled if not fully resident in fast memory.</param>
        /// <param name="copyContents">Whether the content of render target are copied or not when switching to fast memory.</param>

        public void SwitchToFastMemory(CommandBuffer cmd,
            float residencyFraction = 1.0f,
            FastMemoryFlags flags = FastMemoryFlags.SpillTop,
            bool copyContents = false
        )
        {
            residencyFraction = Mathf.Clamp01(residencyFraction);
            cmd.SwitchIntoFastMemory(m_RT, flags, residencyFraction, copyContents);
        }

        /// <summary>
        /// Switch the render target to fast memory on platform that have it and copies the content.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="residencyFraction">How much of the render target is to be switched into fast memory (between 0 and 1).</param>
        /// <param name="flags">Flag to determine what parts of the render target is spilled if not fully resident in fast memory.</param>
        public void CopyToFastMemory(CommandBuffer cmd,
            float residencyFraction = 1.0f,
            FastMemoryFlags flags = FastMemoryFlags.SpillTop
        )
        {
            SwitchToFastMemory(cmd, residencyFraction, flags, copyContents: true);
        }

        /// <summary>
        /// Switch out the render target from fast memory back to main memory on platforms that have fast memory.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="copyContents">Whether the content of render target are copied or not when switching out fast memory.</param>
        public void SwitchOutFastMemory(CommandBuffer cmd, bool copyContents = true)
        {
            cmd.SwitchOutOfFastMemory(m_RT, copyContents);
        }

#endif
    }
}
