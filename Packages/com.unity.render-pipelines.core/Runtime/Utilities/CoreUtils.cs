using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    using UnityObject = UnityEngine.Object;

    /// <summary>
    /// Set of utility functions for the Core Scriptable Render Pipeline Library
    /// </summary>
    public static class CoreUtils
    {
        /// <summary>
        /// List of look at matrices for cubemap faces.
        /// Ref: https://msdn.microsoft.com/en-us/library/windows/desktop/bb204881(v=vs.85).aspx
        /// </summary>
        static public readonly Vector3[] lookAtList =
        {
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, -1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, -1.0f),
        };

        /// <summary>
        /// List of up vectors for cubemap faces.
        /// Ref: https://msdn.microsoft.com/en-us/library/windows/desktop/bb204881(v=vs.85).aspx
        /// </summary>
        static public readonly Vector3[] upVectorList =
        {
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, -1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
        };

        /// <summary>
        /// Class to store the menu sections
        /// </summary>
        public static class Sections
        {
            /// <summary>Menu section 1</summary>
            public const int section1 = 10000;
            /// <summary>Menu section 2</summary>
            public const int section2 = 20000;
            /// <summary>Menu section 3</summary>
            public const int section3 = 30000;
            /// <summary>Menu section 4</summary>
            public const int section4 = 40000;
            /// <summary>Menu section 5</summary>
            public const int section5 = 50000;
            /// <summary>Menu section 6</summary>
            public const int section6 = 60000;
            /// <summary>Menu section 7</summary>
            public const int section7 = 70000;
            /// <summary>Menu section 8</summary>
            public const int section8 = 80000;
        }

        /// <summary>
        /// Class to store the menu priorities on each top level menu
        /// </summary>
        public static class Priorities
        {
            /// <summary>Assets > Create > Shader priority</summary>
            public const int assetsCreateShaderMenuPriority = 83;
            /// <summary>Assets > Create > Rendering priority</summary>
            public const int assetsCreateRenderingMenuPriority = 308;
            /// <summary>Edit Menu base priority</summary>
            public const int editMenuPriority = 320;
            /// <summary>Game Object Menu priority</summary>
            public const int gameObjectMenuPriority = 10;
            /// <summary>Lens Flare Priority</summary>
            public const int srpLensFlareMenuPriority = 303;
        }

        const string obsoletePriorityMessage = "Use CoreUtils.Priorities instead";

        /// <summary>Edit Menu priority 1</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int editMenuPriority1 = 320;
        /// <summary>Edit Menu priority 2</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int editMenuPriority2 = 331;
        /// <summary>Edit Menu priority 3</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int editMenuPriority3 = 342;
        /// <summary>Edit Menu priority 4</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int editMenuPriority4 = 353;
        /// <summary>Asset Create Menu priority 1</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int assetCreateMenuPriority1 = 230;
        /// <summary>Asset Create Menu priority 2</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int assetCreateMenuPriority2 = 241;
        /// <summary>Asset Create Menu priority 3</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int assetCreateMenuPriority3 = 300;
        /// <summary>Game Object Menu priority</summary>
        [Obsolete(obsoletePriorityMessage, false)]
        public const int gameObjectMenuPriority = 10;

        static Cubemap m_BlackCubeTexture;
        /// <summary>
        /// Black cubemap texture.
        /// </summary>
        public static Cubemap blackCubeTexture
        {
            get
            {
                if (m_BlackCubeTexture == null)
                {
                    m_BlackCubeTexture = new Cubemap(1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                    for (int i = 0; i < 6; ++i)
                        m_BlackCubeTexture.SetPixel((CubemapFace)i, 0, 0, Color.black);
                    m_BlackCubeTexture.Apply();
                }

                return m_BlackCubeTexture;
            }
        }

        static Cubemap m_MagentaCubeTexture;
        /// <summary>
        /// Magenta cubemap texture.
        /// </summary>
        public static Cubemap magentaCubeTexture
        {
            get
            {
                if (m_MagentaCubeTexture == null)
                {
                    m_MagentaCubeTexture = new Cubemap(1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                    for (int i = 0; i < 6; ++i)
                        m_MagentaCubeTexture.SetPixel((CubemapFace)i, 0, 0, Color.magenta);
                    m_MagentaCubeTexture.Apply();
                }

                return m_MagentaCubeTexture;
            }
        }

        static CubemapArray m_MagentaCubeTextureArray;
        /// <summary>
        /// Black cubemap array texture.
        /// </summary>
        public static CubemapArray magentaCubeTextureArray
        {
            get
            {
                if (m_MagentaCubeTextureArray == null)
                {
                    m_MagentaCubeTextureArray = new CubemapArray(1, 1, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
                    for (int i = 0; i < 6; ++i)
                    {
                        Color[] colors = { Color.magenta };
                        m_MagentaCubeTextureArray.SetPixels(colors, (CubemapFace)i, 0);
                    }
                    m_MagentaCubeTextureArray.Apply();
                }

                return m_MagentaCubeTextureArray;
            }
        }

        static Cubemap m_WhiteCubeTexture;
        /// <summary>
        /// White cubemap texture.
        /// </summary>
        public static Cubemap whiteCubeTexture
        {
            get
            {
                if (m_WhiteCubeTexture == null)
                {
                    m_WhiteCubeTexture = new Cubemap(1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                    for (int i = 0; i < 6; ++i)
                        m_WhiteCubeTexture.SetPixel((CubemapFace)i, 0, 0, Color.white);
                    m_WhiteCubeTexture.Apply();
                }

                return m_WhiteCubeTexture;
            }
        }

        static RenderTexture m_EmptyUAV;
        /// <summary>
        /// Empty 1x1 texture usable as a dummy UAV.
        /// </summary>
        public static RenderTexture emptyUAV
        {
            get
            {
                if (m_EmptyUAV == null)
                {
                    m_EmptyUAV = new RenderTexture(1, 1, 0);
                    m_EmptyUAV.enableRandomWrite = true;
                    m_EmptyUAV.Create();
                }

                return m_EmptyUAV;
            }
        }

        static Texture3D m_BlackVolumeTexture;
        /// <summary>
        /// Black 3D texture.
        /// </summary>
        public static Texture3D blackVolumeTexture
        {
            get
            {
                if (m_BlackVolumeTexture == null)
                {
                    Color[] colors = { Color.black };
                    m_BlackVolumeTexture = new Texture3D(1, 1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                    m_BlackVolumeTexture.SetPixels(colors, 0);
                    m_BlackVolumeTexture.Apply();
                }

                return m_BlackVolumeTexture;
            }
        }

        /// <summary>
        /// Clear the currently bound render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="clearFlag">Specify how the render texture should be cleared.</param>
        /// <param name="clearColor">Specify with which color the render texture should be cleared.</param>
        public static void ClearRenderTarget(CommandBuffer cmd, ClearFlag clearFlag, Color clearColor)
        {
            if (clearFlag != ClearFlag.None)
                cmd.ClearRenderTarget((RTClearFlags)clearFlag, clearColor, 1.0f, 0x00);
        }

        // We use -1 as a default value because when doing SPI for XR, it will bind the full texture array by default (and has no effect on 2D textures)
        // Unfortunately, for cubemaps, passing -1 does not work for faces other than the first one, so we fall back to 0 in this case.
        private static int FixupDepthSlice(int depthSlice, RTHandle buffer)
        {
            // buffer.rt can be null in case the RTHandle is constructed from a RenderTextureIdentifier.
            if (depthSlice == -1 && buffer.rt?.dimension == TextureDimension.Cube)
                depthSlice = 0;

            return depthSlice;
        }

        private static int FixupDepthSlice(int depthSlice, CubemapFace cubemapFace)
        {
            if (depthSlice == -1 && cubemapFace != CubemapFace.Unknown)
                depthSlice = 0;

            return depthSlice;
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">RenderTargetIdentifier of the render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace, depthSlice);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">RenderTargetIdentifier of the render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            SetRenderTarget(cmd, buffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color render texture.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color render texture.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color render texture.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace, depthSlice);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, ClearFlag.None, Color.clear);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, clearFlag, Color.clear);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer, 0, CubemapFace.Unknown, -1);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        // Explicit load and store actions
        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(buffer, loadAction, storeAction);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, ClearFlag clearFlag)
        {
            SetRenderTarget(cmd, buffer, loadAction, storeAction, clearFlag, Color.clear);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthBuffer">Depth buffer RenderTargetIdentifier.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthBuffer, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffer, colorLoadAction, colorStoreAction, depthBuffer, depthLoadAction, depthStoreAction);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">RenderTargetIdentifier of the render texture.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(buffer, colorLoadAction, colorStoreAction, depthLoadAction, depthStoreAction);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthBuffer">Depth buffer RenderTargetIdentifier.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthBuffer, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlag)
        {
            SetRenderTarget(cmd, colorBuffer, colorLoadAction, colorStoreAction, depthBuffer, depthLoadAction, depthStoreAction, clearFlag, Color.clear);
        }

        private static void SetViewportAndClear(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag, Color clearColor)
        {
            // Clearing a partial viewport currently does not go through the hardware clear.
            // Instead it goes through a quad rendered with a specific shader.
            // When enabling wireframe mode in the scene view, unfortunately it overrides this shader thus breaking every clears.
            // That's why in the editor we don't set the viewport before clearing (it's set to full screen by the previous SetRenderTarget) but AFTER so that we benefit from un-bugged hardware clear.
            // We consider that the small loss in performance is acceptable in the editor.
            // A refactor of wireframe is needed before we can fix this properly (with not doing anything!)
#if !UNITY_EDITOR
            SetViewport(cmd, buffer);
#endif
            CoreUtils.ClearRenderTarget(cmd, clearFlag, clearColor);
#if UNITY_EDITOR
            SetViewport(cmd, buffer);
#endif
        }

        // This set of RenderTarget management methods is supposed to be used when rendering RTHandle render texture.
        // This will automatically set the viewport based on the RTHandle System reference size and the RTHandle scaling info.

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="buffer">Color buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, buffer);
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, buffer, clearFlag, clearColor);
        }

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="buffer">Color buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
            => SetRenderTarget(cmd, buffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="colorBuffer">Color buffer RTHandle</param>
        /// <param name="depthBuffer">Depth buffer RTHandle</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle colorBuffer, RTHandle depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="colorBuffer">Color buffer RTHandle</param>
        /// <param name="depthBuffer">Depth buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle colorBuffer, RTHandle depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="colorBuffer">Color buffer RTHandle</param>
        /// <param name="depthBuffer">Depth buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle colorBuffer, RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            CoreUtils.SetRenderTarget(cmd, colorBuffer.rt, depthBuffer.rt, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, colorBuffer, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">Depth Buffer RTHandle.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer.rt, ClearFlag.None, Color.clear);
            SetViewport(cmd, depthBuffer);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">Depth Buffer RTHandle.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer.rt); // Don't clear here, viewport needs to be set before we do.
            SetViewportAndClear(cmd, depthBuffer, clearFlag, Color.clear);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">Depth Buffer RTHandle.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer, 0, CubemapFace.Unknown, -1);
            SetViewportAndClear(cmd, depthBuffer, clearFlag, clearColor);
        }

        // Scaling viewport is done for auto-scaling render targets.
        // In the context of SRP, every auto-scaled RT is scaled against the maximum RTHandles reference size (that can only grow).
        // When we render using a camera whose viewport is smaller than the RTHandles reference size (and thus smaller than the RT actual size), we need to set it explicitly (otherwise, native code will set the viewport at the size of the RT)
        // For auto-scaled RTs (like for example a half-resolution RT), we need to scale this viewport accordingly.
        // For non scaled RTs we just do nothing, the native code will set the viewport at the size of the RT anyway.

        /// <summary>
        /// Setup the viewport to the size of the provided RTHandle.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="target">RTHandle from which to compute the proper viewport.</param>
        public static void SetViewport(CommandBuffer cmd, RTHandle target)
        {
            if (target.useScaling)
            {
                Vector2Int scaledViewportSize = target.GetScaledSize(target.rtHandleProperties.currentViewportSize);
                cmd.SetViewport(new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y));
            }
        }

        /// <summary>
        /// Generate a name based on render texture parameters.
        /// </summary>
        /// <param name="width">With of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="depth">Depth of the texture.</param>
        /// <param name="format">Format of the render texture.</param>
        /// <param name="name">Base name of the texture.</param>
        /// <param name="mips">True if the texture has mip maps.</param>
        /// <param name="enableMSAA">True if the texture is multisampled.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <returns>Generated names bassed on the provided parameters.</returns>
        public static string GetRenderTargetAutoName(int width, int height, int depth, RenderTextureFormat format, string name, bool mips = false, bool enableMSAA = false, MSAASamples msaaSamples = MSAASamples.None)
            => GetRenderTargetAutoName(width, height, depth, format.ToString(), TextureDimension.None, name, mips, enableMSAA, msaaSamples, dynamicRes: false);

        /// <summary>
        /// Generate a name based on render texture parameters.
        /// </summary>
        /// <param name="width">With of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="depth">Depth of the texture.</param>
        /// <param name="format">Graphics format of the render texture.</param>
        /// <param name="name">Base name of the texture.</param>
        /// <param name="mips">True if the texture has mip maps.</param>
        /// <param name="enableMSAA">True if the texture is multisampled.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <returns>Generated names bassed on the provided parameters.</returns>
        public static string GetRenderTargetAutoName(int width, int height, int depth, GraphicsFormat format, string name, bool mips = false, bool enableMSAA = false, MSAASamples msaaSamples = MSAASamples.None)
            => GetRenderTargetAutoName(width, height, depth, format.ToString(), TextureDimension.None, name, mips, enableMSAA, msaaSamples, dynamicRes: false);

        /// <summary>
        /// Generate a name based on render texture parameters.
        /// </summary>
        /// <param name="width">With of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="depth">Depth of the texture.</param>
        /// <param name="format">Graphics format of the render texture.</param>
        /// <param name="dim">Dimension of the texture.</param>
        /// <param name="name">Base name of the texture.</param>
        /// <param name="mips">True if the texture has mip maps.</param>
        /// <param name="enableMSAA">True if the texture is multisampled.</param>
        /// <param name="msaaSamples">Number of MSAA samples.</param>
        /// <param name="dynamicRes">True if the texture uses dynamic resolution.</param>
        /// <returns>Generated names bassed on the provided parameters.</returns>
        public static string GetRenderTargetAutoName(int width, int height, int depth, GraphicsFormat format, TextureDimension dim, string name, bool mips = false, bool enableMSAA = false, MSAASamples msaaSamples = MSAASamples.None, bool dynamicRes = false)
            => GetRenderTargetAutoName(width, height, depth, format.ToString(), dim, name, mips, enableMSAA, msaaSamples, dynamicRes);

        static string GetRenderTargetAutoName(int width, int height, int depth, string format, TextureDimension dim, string name, bool mips, bool enableMSAA, MSAASamples msaaSamples, bool dynamicRes)
        {
            string result = string.Format("{0}_{1}x{2}", name, width, height);

            if (depth > 1)
                result = string.Format("{0}x{1}", result, depth);

            if (mips)
                result = string.Format("{0}_{1}", result, "Mips");

            result = string.Format("{0}_{1}", result, format);

            if (dim != TextureDimension.None)
                result = string.Format("{0}_{1}", result, dim);

            if (enableMSAA)
                result = string.Format("{0}_{1}", result, msaaSamples.ToString());

            if (dynamicRes)
                result = string.Format("{0}_{1}", result, "dynamic");

            return result;
        }

        /// <summary>
        /// Generate a name based on texture parameters.
        /// </summary>
        /// <param name="width">With of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="format">Format of the texture.</param>
        /// <param name="dim">Dimension of the texture.</param>
        /// <param name="name">Base name of the texture.</param>
        /// <param name="mips">True if the texture has mip maps.</param>
        /// <param name="depth">Depth of the texture.</param>
        /// <returns>Generated names based on the provided parameters.</returns>
        public static string GetTextureAutoName(int width, int height, TextureFormat format, TextureDimension dim = TextureDimension.None, string name = "", bool mips = false, int depth = 0)
            => GetTextureAutoName(width, height, format.ToString(), dim, name, mips, depth);

        /// <summary>
        /// Generate a name based on texture parameters.
        /// </summary>
        /// <param name="width">With of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="format">Graphics format of the texture.</param>
        /// <param name="dim">Dimension of the texture.</param>
        /// <param name="name">Base name of the texture.</param>
        /// <param name="mips">True if the texture has mip maps.</param>
        /// <param name="depth">Depth of the texture.</param>
        /// <returns>Generated names based on the provided parameters.</returns>
        public static string GetTextureAutoName(int width, int height, GraphicsFormat format, TextureDimension dim = TextureDimension.None, string name = "", bool mips = false, int depth = 0)
            => GetTextureAutoName(width, height, format.ToString(), dim, name, mips, depth);

        static string GetTextureAutoName(int width, int height, string format, TextureDimension dim = TextureDimension.None, string name = "", bool mips = false, int depth = 0)
        {
            string temp;
            if (depth == 0)
                temp = string.Format("{0}x{1}{2}_{3}", width, height, mips ? "_Mips" : "", format);
            else
                temp = string.Format("{0}x{1}x{2}{3}_{4}", width, height, depth, mips ? "_Mips" : "", format);
            temp = String.Format("{0}_{1}_{2}", name == "" ? "Texture" : name, (dim == TextureDimension.None) ? "" : dim.ToString(), temp);

            return temp;
        }

        /// <summary>
        /// Clear a cubemap render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="renderTexture">Cubemap render texture that needs to be cleared.</param>
        /// <param name="clearColor">Color used for clearing.</param>
        /// <param name="clearMips">Set to true to clear the mip maps of the render texture.</param>
        public static void ClearCubemap(CommandBuffer cmd, RenderTexture renderTexture, Color clearColor, bool clearMips = false)
        {
            int mipCount = 1;
            if (renderTexture.useMipMap && clearMips)
            {
                mipCount = (int)Mathf.Log((float)renderTexture.width, 2.0f) + 1;
            }

            for (int i = 0; i < 6; ++i)
            {
                for (int mip = 0; mip < mipCount; ++mip)
                {
                    SetRenderTarget(cmd, new RenderTargetIdentifier(renderTexture), ClearFlag.Color, clearColor, mip, (CubemapFace)i);
                }
            }
        }

        /// <summary>
        /// Draws a full screen triangle.
        /// </summary>
        /// <param name="commandBuffer">CommandBuffer used for rendering commands.</param>
        /// <param name="material">Material used on the full screen triangle.</param>
        /// <param name="properties">Optional material property block for the provided material.</param>
        /// <param name="shaderPassId">Index of the material pass.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        /// <summary>
        /// Draws a full screen triangle.
        /// </summary>
        /// <param name="commandBuffer">CommandBuffer used for rendering commands.</param>
        /// <param name="material">Material used on the full screen triangle.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color buffer that needs to be set before drawing the full screen triangle.</param>
        /// <param name="properties">Optional material property block for the provided material.</param>
        /// <param name="shaderPassId">Index of the material pass.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffer, 0, CubemapFace.Unknown, -1);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        /// <summary>
        /// Draws a full screen triangle.
        /// </summary>
        /// <param name="commandBuffer">CommandBuffer used for rendering commands.</param>
        /// <param name="material">Material used on the full screen triangle.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color buffer that needs to be set before drawing the full screen triangle.</param>
        /// <param name="depthStencilBuffer">RenderTargetIdentifier of the depth buffer that needs to be set before drawing the full screen triangle.</param>
        /// <param name="properties">Optional material property block for the provided material.</param>
        /// <param name="shaderPassId">Index of the material pass.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffer, depthStencilBuffer, 0, CubemapFace.Unknown, -1);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        /// <summary>
        /// Draws a full screen triangle.
        /// </summary>
        /// <param name="commandBuffer">CommandBuffer used for rendering commands.</param>
        /// <param name="material">Material used on the full screen triangle.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color buffers that needs to be set before drawing the full screen triangle.</param>
        /// <param name="depthStencilBuffer">RenderTargetIdentifier of the depth buffer that needs to be set before drawing the full screen triangle.</param>
        /// <param name="properties">Optional material property block for the provided material.</param>
        /// <param name="shaderPassId">Index of the material pass.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffers, depthStencilBuffer, 0, CubemapFace.Unknown, -1);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        // Important: the first RenderTarget must be created with 0 depth bits!

        /// <summary>
        /// Draws a full screen triangle.
        /// </summary>
        /// <param name="commandBuffer">CommandBuffer used for rendering commands.</param>
        /// <param name="material">Material used on the full screen triangle.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color buffers that needs to be set before drawing the full screen triangle.</param>
        /// <param name="properties">Optional material property block for the provided material.</param>
        /// <param name="shaderPassId">Index of the material pass.</param>
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            // It is currently not possible to have MRT without also setting a depth target.
            // To work around this deficiency of the CommandBuffer.SetRenderTarget() API,
            // we pass the first color target as the depth target. If it has 0 depth bits,
            // no depth target ends up being bound.
            DrawFullScreen(commandBuffer, material, colorBuffers, colorBuffers[0], properties, shaderPassId);
        }

        // Color space utilities
        /// <summary>
        /// Converts the provided sRGB color to the current active color space.
        /// </summary>
        /// <param name="color">Input color.</param>
        /// <returns>Linear color if the active color space is ColorSpace.Linear, the original input otherwise.</returns>
        public static Color ConvertSRGBToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        /// <summary>
        /// Converts the provided linear color to the current active color space.
        /// </summary>
        /// <param name="color">Input color.</param>
        /// <returns>sRGB color if the active color space is ColorSpace.Gamma, the original input otherwise.</returns>
        public static Color ConvertLinearToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color : color.gamma;
        }

        /// <summary>
        /// Creates a Material with the provided shader path.
        /// hideFlags will be set to HideFlags.HideAndDontSave.
        /// </summary>
        /// <param name="shaderPath">Path of the shader used for the material.</param>
        /// <returns>A new Material instance using the shader found at the provided path.</returns>
        public static Material CreateEngineMaterial(string shaderPath)
        {
            Shader shader = Shader.Find(shaderPath);
            if (shader == null)
            {
                Debug.LogError("Cannot create required material because shader " + shaderPath + " could not be found");
                return null;
            }

            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        /// <summary>
        /// Creates a Material with the provided shader.
        /// hideFlags will be set to HideFlags.HideAndDontSave.
        /// </summary>
        /// <param name="shader">Shader used for the material.</param>
        /// <returns>A new Material instance using the provided shader.</returns>
        public static Material CreateEngineMaterial(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Cannot create required material because shader is null");
                return null;
            }

            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        /// <summary>
        /// Bitfield flag test.
        /// </summary>
        /// <typeparam name="T">Type of the enum flag.</typeparam>
        /// <param name="mask">Bitfield to test the flag against.</param>
        /// <param name="flag">Flag to be tested against the provided mask.</param>
        /// <returns>True if the flag is present in the mask.</returns>
        public static bool HasFlag<T>(T mask, T flag) where T : IConvertible
        {
            return (mask.ToUInt32(null) & flag.ToUInt32(null)) != 0;
        }

        /// <summary>
        /// Swaps two values.
        /// </summary>
        /// <typeparam name="T">Type of the values</typeparam>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        public static void Swap<T>(ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        /// <summary>
        /// Set a global keyword using a CommandBuffer
        /// </summary>
        /// <param name="cmd">CommandBuffer on which to set the global keyword.</param>
        /// <param name="keyword">Keyword to be set.</param>
        /// <param name="state">Value of the keyword to be set.</param>
        public static void SetKeyword(CommandBuffer cmd, string keyword, bool state)
        {
            if (state)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        // Caution: such a call should not be use interlaced with command buffer command, as it is immediate
        /// <summary>
        /// Set a keyword immediatly on a Material.
        /// </summary>
        /// <param name="material">Material on which to set the keyword.</param>
        /// <param name="keyword">Keyword to set on the material.</param>
        /// <param name="state">Value of the keyword to set on the material.</param>
        public static void SetKeyword(Material material, string keyword, bool state)
        {
            if (state)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        /// <summary>
        /// Set a keyword to a compute shader
        /// </summary>
        /// <param name="cs">Compute Shader on which to set the keyword.</param>
        /// <param name="keyword">Keyword to be set.</param>
        /// <param name="state">Value of the keyword to be set.</param>
        public static void SetKeyword(ComputeShader cs, string keyword, bool state)
        {
            if (state)
                cs.EnableKeyword(keyword);
            else
                cs.DisableKeyword(keyword);
        }

        /// <summary>
        /// Destroys a UnityObject safely.
        /// </summary>
        /// <param name="obj">Object to be destroyed.</param>
        public static void Destroy(UnityObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
            }
        }

        static IEnumerable<Type> m_AssemblyTypes;

        /// <summary>
        /// Returns all assembly types.
        /// </summary>
        /// <returns>The list of all assembly types of the current domain.</returns>
        public static IEnumerable<Type> GetAllAssemblyTypes()
        {
            if (m_AssemblyTypes == null)
            {
                m_AssemblyTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t =>
                    {
                        // Ugly hack to handle mis-versioned dlls
                        var innerTypes = new Type[0];
                        try
                        {
                            innerTypes = t.GetTypes();
                        }
                        catch { }
                        return innerTypes;
                    });
            }

            return m_AssemblyTypes;
        }

        /// <summary>
        /// Returns a list of types that inherit from the provided type.
        /// </summary>
        /// <typeparam name="T">Parent Type</typeparam>
        /// <returns>A list of types that inherit from the provided type.</returns>
        public static IEnumerable<Type> GetAllTypesDerivedFrom<T>()
        {
#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
            return UnityEditor.TypeCache.GetTypesDerivedFrom<T>();
#else
            return GetAllAssemblyTypes().Where(t => t.IsSubclassOf(typeof(T)));
#endif
        }

        /// <summary>
        /// Safely release a Compute Buffer.
        /// </summary>
        /// <param name="buffer">Compute Buffer that needs to be released.</param>
        public static void SafeRelease(ComputeBuffer buffer)
        {
            if (buffer != null)
                buffer.Release();
        }

        /// <summary>
        /// Creates a cube mesh.
        /// </summary>
        /// <param name="min">Minimum corner coordinates in local space.</param>
        /// <param name="max">Maximum corner coordinates in local space.</param>
        /// <returns>A new instance of a cube Mesh.</returns>
        public static Mesh CreateCubeMesh(Vector3 min, Vector3 max)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[8];

            vertices[0] = new Vector3(min.x, min.y, min.z);
            vertices[1] = new Vector3(max.x, min.y, min.z);
            vertices[2] = new Vector3(max.x, max.y, min.z);
            vertices[3] = new Vector3(min.x, max.y, min.z);
            vertices[4] = new Vector3(min.x, min.y, max.z);
            vertices[5] = new Vector3(max.x, min.y, max.z);
            vertices[6] = new Vector3(max.x, max.y, max.z);
            vertices[7] = new Vector3(min.x, max.y, max.z);

            mesh.vertices = vertices;

            int[] triangles = new int[36];

            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
            triangles[6] = 1; triangles[7] = 6; triangles[8] = 5;
            triangles[9] = 1; triangles[10] = 2; triangles[11] = 6;
            triangles[12] = 5; triangles[13] = 7; triangles[14] = 4;
            triangles[15] = 5; triangles[16] = 6; triangles[17] = 7;
            triangles[18] = 4; triangles[19] = 3; triangles[20] = 0;
            triangles[21] = 4; triangles[22] = 7; triangles[23] = 3;
            triangles[24] = 3; triangles[25] = 6; triangles[26] = 2;
            triangles[27] = 3; triangles[28] = 7; triangles[29] = 6;
            triangles[30] = 4; triangles[31] = 1; triangles[32] = 5;
            triangles[33] = 4; triangles[34] = 0; triangles[35] = 1;

            mesh.triangles = triangles;
            return mesh;
        }

        /// <summary>
        /// Returns true if "Post Processes" are enabled for the view associated with the given camera.
        /// </summary>
        /// <param name="camera">Input camera.</param>
        /// <returns>True if "Post Processes" are enabled for the view associated with the given camera.</returns>
        public static bool ArePostProcessesEnabled(Camera camera)
        {
            bool enabled = true;

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                enabled = false;

                // Determine whether the "Post Processes" checkbox is checked for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++)
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;

                    // Post-processing is disabled in scene view if either showImageEffects is disabled or we are
                    // rendering in wireframe mode.
                    if (sv.camera == camera &&
                        (sv.sceneViewState.imageEffectsEnabled && sv.cameraMode.drawMode != UnityEditor.DrawCameraMode.Wireframe))
                    {
                        enabled = true;
                        break;
                    }
                }
            }
#endif

            return enabled;
        }

        /// <summary>
        /// Returns true if "Animated Materials" are enabled for the view associated with the given camera.
        /// </summary>
        /// <param name="camera">Input camera.</param>
        /// <returns>True if "Animated Materials" are enabled for the view associated with the given camera.</returns>
        public static bool AreAnimatedMaterialsEnabled(Camera camera)
        {
            bool animateMaterials = true;

#if UNITY_EDITOR
            animateMaterials = Application.isPlaying; // For Game and VR views; Reflection views pass the parent camera

            if (camera.cameraType == CameraType.SceneView)
            {
                animateMaterials = false;

                // Determine whether the "Animated Materials" checkbox is checked for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++) // Using a foreach on an ArrayList generates garbage ...
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
#if UNITY_2020_2_OR_NEWER
                    if (sv.camera == camera && sv.sceneViewState.alwaysRefreshEnabled)
#else
                    if (sv.camera == camera && sv.sceneViewState.materialUpdateEnabled)
#endif
                    {
                        animateMaterials = true;
                        break;
                    }
                }
            }
            else if (camera.cameraType == CameraType.Preview)
            {
                // Enable for previews so the shader graph main preview works with time parameters.
                animateMaterials = true;
            }
            else if (camera.cameraType == CameraType.Reflection)
            {
                // Reflection cameras should be handled outside this function.
                // Debug.Assert(false, "Unexpected View type.");
            }

            // IMHO, a better solution would be:
            // A window invokes a camera render. The camera knows which window called it, so it can query its properies
            // (such as animated materials). This camera provides the space-time position. It should also be able
            // to access the rendering settings somehow. Using this information, it is then able to construct the
            // primary view with information about camera-relative rendering, LOD, time, rendering passes/features
            // enabled, etc. We then render this view. It can have multiple sub-views (shadows, reflections).
            // They inherit all the properties of the primary view, but also have the ability to override them
            // (e.g. primary cam pos and time are retained, matrices are modified, SSS and tessellation are disabled).
            // These views can then have multiple sub-views (probably not practical for games),
            // which simply amounts to a recursive call, and then the story repeats itself.
            //
            // TLDR: we need to know the caller and its status/properties to make decisions.
#endif

            return animateMaterials;
        }

        /// <summary>
        /// Returns true if "Scene Lighting" is enabled for the view associated with the given camera.
        /// </summary>
        /// <param name="camera">Input camera.</param>
        /// <returns>True if "Scene Lighting" is enabled for the view associated with the given camera.</returns>
        public static bool IsSceneLightingDisabled(Camera camera)
        {
            bool disabled = false;
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                // Determine whether the "No Scene Lighting" checkbox is checked for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++)
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
                    if (sv.camera == camera && !sv.sceneLighting)
                    {
                        disabled = true;
                        break;
                    }
                }
            }
#endif
            return disabled;
        }

        /// <summary>
        /// Returns true if the "Light Overlap" scene view draw mode is enabled.
        /// </summary>
        /// <param name="camera">Input camera.</param>
        /// <returns>True if "Light Overlap" is enabled in the scene view associated with the input camera.</returns>
        public static bool IsLightOverlapDebugEnabled(Camera camera)
        {
            bool enabled = false;
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                // Determine whether the "LightOverlap" mode is enabled for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++)
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
                    if (sv.camera == camera && sv.cameraMode.drawMode == UnityEditor.DrawCameraMode.LightOverlap)
                    {
                        enabled = true;
                        break;
                    }
                }
            }
#endif
            return enabled;
        }

#if UNITY_EDITOR
        static Func<List<UnityEditor.MaterialEditor>> materialEditors;

        static CoreUtils()
        {
            //quicker than standard reflection as it is compiled
            System.Reflection.FieldInfo field = typeof(UnityEditor.MaterialEditor).GetField("s_MaterialEditors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var fieldExpression = System.Linq.Expressions.Expression.Field(null, field);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<List<UnityEditor.MaterialEditor>>>(fieldExpression);
            materialEditors = lambda.Compile();
            LoadSceneViewMethods();
        }

#endif

        /// <summary>
        /// Returns true if "Fog" is enabled for the view associated with the given camera.
        /// </summary>
        /// <param name="camera">Input camera.</param>
        /// <returns>True if "Fog" is enabled for the view associated with the given camera.</returns>
        public static bool IsSceneViewFogEnabled(Camera camera)
        {
            bool fogEnable = true;

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                fogEnable = false;

                // Determine whether the "Animated Materials" checkbox is checked for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++)
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
                    if (sv.camera == camera && sv.sceneViewState.fogEnabled)
                    {
                        fogEnable = true;
                        break;
                    }
                }
            }
#endif

            return fogEnable;
        }

        /// <summary>
        /// Returns true if any Scene view is using the Scene filtering.
        /// </summary>
        /// <returns>True if any Scene view is using the Scene filtering.</returns>
        public static bool IsSceneFilteringEnabled()
        {
#if UNITY_EDITOR && UNITY_2021_2_OR_NEWER
            for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++)
            {
                var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
                if (sv.isUsingSceneFiltering) return true;
            }
#endif
            return false;
        }

#if UNITY_EDITOR
        static Func<int> GetSceneViewPrefabStageContext;

        static void LoadSceneViewMethods()
        {
            var stageNavigatorManager = typeof(UnityEditor.SceneManagement.PrefabStage).Assembly.GetType("UnityEditor.SceneManagement.StageNavigationManager");
            var instance = stageNavigatorManager.GetProperty("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
            var renderMode = stageNavigatorManager.GetProperty("contextRenderMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            var renderModeAccessor = System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Property(null, instance), renderMode);
            var internalRenderModeLambda = System.Linq.Expressions.Expression.Lambda<Func<int>>(System.Linq.Expressions.Expression.Convert(renderModeAccessor, typeof(int)));
            GetSceneViewPrefabStageContext = internalRenderModeLambda.Compile();
        }
#endif

        /// <summary>
        /// Returns true if the currently opened prefab stage context is set to Hidden.
        /// </summary>
        /// <returns>True if the currently opened prefab stage context is set to Hidden.</returns>
        public static bool IsSceneViewPrefabStageContextHidden()
        {
#if UNITY_EDITOR
            return GetSceneViewPrefabStageContext() == 2; // 2 is hidden, see ContextRenderMode enum
#else
            return false;
#endif
        }

        /// <summary>
        /// Draw a renderer list.
        /// </summary>
        /// <param name="renderContext">Current Scriptable Render Context.</param>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="rendererList">Renderer List to render.</param>
        [Obsolete("Use the updated RendererList API in the UnityEngine.Rendering.RendererUtils namespace.")]
        public static void DrawRendererList(ScriptableRenderContext renderContext, CommandBuffer cmd, Experimental.Rendering.RendererList rendererList)
        {
            if (!rendererList.isValid)
                throw new ArgumentException("Invalid renderer list provided to DrawRendererList");

            // This is done here because DrawRenderers API lives outside command buffers so we need to make call this before doing any DrawRenders or things will be executed out of order
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (rendererList.stateBlock == null)
                renderContext.DrawRenderers(rendererList.cullingResult, ref rendererList.drawSettings, ref rendererList.filteringSettings);
            else
            {
                var renderStateBlock = rendererList.stateBlock.Value;
                renderContext.DrawRenderers(rendererList.cullingResult, ref rendererList.drawSettings, ref rendererList.filteringSettings, ref renderStateBlock);
            }
        }

        /// <summary>
        /// Draw a renderer list.
        /// </summary>
        /// <param name="renderContext">Current Scriptable Render Context.</param>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="rendererList">Renderer List to render.</param>
        public static void DrawRendererList(ScriptableRenderContext renderContext, CommandBuffer cmd, RendererUtils.RendererList rendererList)
        {
            if (!rendererList.isValid)
                throw new ArgumentException("Invalid renderer list provided to DrawRendererList");

            cmd.DrawRendererList(rendererList);
        }

        /// <summary>
        /// Compute a hash of texture properties.
        /// </summary>
        /// <param name="texture"> Source texture.</param>
        /// <returns>Returns hash of texture properties.</returns>
        public static int GetTextureHash(Texture texture)
        {
            int hash = texture.GetHashCode();

            unchecked
            {
#if UNITY_EDITOR
                hash = 23 * hash + texture.imageContentsHash.GetHashCode();
#endif
                hash = 23 * hash + texture.GetInstanceID().GetHashCode();
                hash = 23 * hash + texture.graphicsFormat.GetHashCode();
                hash = 23 * hash + texture.wrapMode.GetHashCode();
                hash = 23 * hash + texture.width.GetHashCode();
                hash = 23 * hash + texture.height.GetHashCode();
                hash = 23 * hash + texture.filterMode.GetHashCode();
                hash = 23 * hash + texture.anisoLevel.GetHashCode();
                hash = 23 * hash + texture.mipmapCount.GetHashCode();
                hash = 23 * hash + texture.updateCount.GetHashCode();
            }

            return hash;
        }

        // Hacker’s Delight, Second Edition page 66
        /// <summary>
        /// Branchless previous power of two.
        /// </summary>
        /// <param name="size">Starting size or number.</param>
        /// <returns>Previous power of two.</returns>
        public static int PreviousPowerOfTwo(int size)
        {
            if (size <= 0)
                return 0;

            size |= (size >> 1);
            size |= (size >> 2);
            size |= (size >> 4);
            size |= (size >> 8);
            size |= (size >> 16);
            return size - (size >> 1);
        }

        /// <summary>
        /// Get the last declared value from an enum Type
        /// </summary>
        /// <typeparam name="T">Type of the enum</typeparam>
        /// <returns>Last value of the enum</returns>
        public static T GetLastEnumValue<T>() where T : Enum
            => typeof(T).GetEnumValues().Cast<T>().Last();

        internal static string GetCorePath()
            => "Packages/com.unity.render-pipelines.core/";

#if UNITY_EDITOR
        // This is required in Runtime assembly between #if UNITY_EDITOR
        /// <summary>
        /// AssetDataBase.FindAssets("t:<type>") load all asset in project to check the type.
        /// This utility function will try to filter at much possible before loading anything.
        /// This also works with Interface and inherited types.
        /// This will not find embedded sub assets.
        /// This still take times on big project so it must be only used in Editor context only.
        /// </summary>
        /// <typeparam name="T">Type or Interface to search</typeparam>
        /// <param name="extension">Extension of files to search in</param>
        /// <param name="allowSubTypes">Allows to retrieve type inheriting from T.</param>
        /// <returns>List of all asset of type T or implementing interface T.</returns>
        public static IEnumerable<T> LoadAllAssets<T>(string extension = "asset", bool allowSubTypes = true)
            where T : class
        {
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException(nameof(extension), "You must pass a valid extension");

            bool isInterface = typeof(T).IsInterface;
            if (!typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) && !isInterface)
                throw new Exception("T must be an interface or inherite UnityEngine.Object.");

            Func<Type, bool> needsLoad = (allowSubTypes || isInterface)
                ? (type) => typeof(T).IsAssignableFrom(type)
                : (type) => typeof(T) == type;

            string[] guids = UnityEditor.AssetDatabase.FindAssets($"glob:\"*.{extension}\"");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Type type = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
                if (needsLoad(type))
                    yield return UnityEditor.AssetDatabase.LoadAssetAtPath(path, type) as T;
            }
        }

        /// <summary>
        /// Create any missing folder in the file path given.
        /// Path must use '/' separator
        /// </summary>
        /// <param name="filePath">Path to a file or to a folder (ending with '/') to ensure existance of each sub folder in it. </param>
        public static void EnsureFolderTreeInAssetFilePath(string filePath)
        {
            void Recurse(string _folderPath)
            {
                int lastSeparator = _folderPath.LastIndexOf('/');
                if (lastSeparator == -1)
                    return;

                string rootPath = _folderPath.Substring(0, lastSeparator);

                Recurse(rootPath);

                string folder = _folderPath.Substring(lastSeparator + 1);
                if (!UnityEditor.AssetDatabase.IsValidFolder(_folderPath))
                    UnityEditor.AssetDatabase.CreateFolder(rootPath, folder);
            }

            if (!filePath.StartsWith("assets/", System.StringComparison.CurrentCultureIgnoreCase))
                throw new System.ArgumentException($"Path should start with \"Assets/\". Got {filePath}.", filePath);

            Recurse(filePath.Substring(0, filePath.LastIndexOf('/')));
        }

#endif
    }
}
