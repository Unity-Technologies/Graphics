using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct LightLoopShaderVariables
    {
        public const int MAX_ENV2D_LIGHTS = 32;

        public uint m_DirectionalLightCount;
        public uint m_PunctualLightCount;
        public uint m_AreaLightCount;
        public uint m_EnvLightCount;
        public uint m_EnvProxyCount;
        public int m_EnvLightSkyEnabled;         // TODO: make it a bool
        public int m_DirectionalShadowIndex;

        public float m_MicroShadowOpacity;

        public uint m_NumTileFtplX;
        public uint m_NumTileFtplY;

        public int pad1, pad2;

        public fixed float /* 4x4 */ m_mInvScrProjection[16]; // TODO: remove, unused in HDRP

        public float m_fClustScale;
        public float m_fClustBase;
        public float m_fNearPlane;
        public float m_fFarPlane;
        public int m_iLog2NumClusters; // We need to always define these to keep constant buffer layouts compatible

        public uint m_isLogBaseBufferEnabled;

        public uint m_NumTileClusteredX;
        public uint m_NumTileClusteredY;

        public fixed float m_ShadowAtlasSize[4];
        public fixed float m_CascadeShadowAtlasSize[4];
        public uint m_CascadeShadowCount;
        public int pad3, pad4, pad5;

        public fixed float /* 4x4 */ m_Env2DCaptureVP[MAX_ENV2D_LIGHTS * 4 * 4];

        // TODO: move this elsewhere
        public int m_DebugSingleShadowIndex;

        public int m_EnvSliceSize;

        public uint m_DecalCount;

    }
}
