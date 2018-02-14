using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Decal
    {
        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 10000)]
        public struct DecalSurfaceData
        {
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector4 baseColor;
            [SurfaceDataAttributes("Normal", true)]
            public Vector4 normalWS;
            [SurfaceDataAttributes("Mask", true)]
            public Vector4 mask;
			[SurfaceDataAttributes("HTileMask")]
			public uint HTileMask; 
        };

        [GenerateHLSL(PackingRules.Exact)]
        public enum DBufferMaterial
        {
            // Note: This count doesn't include the velocity buffer. On shader and csharp side the velocity buffer will be added by the framework
            Count = 3
        };

        [GenerateHLSL(PackingRules.Exact)]
        public enum DBufferHTileBit
        {            
            Diffuse = 1,
            Normal = 2,
            Mask = 4
        };

        //-----------------------------------------------------------------------------
        // DBuffer management
        //-----------------------------------------------------------------------------

		// should this be combined into common class shared with Lit.cs???
       static public int GetMaterialDBufferCount() { return (int)DBufferMaterial.Count; }

	   static RenderTextureFormat[] m_RTFormat = { RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32 };
	   static bool[] m_sRGBFlags= { true, false, false };

       static public void GetMaterialDBufferDescription(out RenderTextureFormat[] RTFormat, out bool[] sRGBFlags)
       {
            RTFormat = m_RTFormat;
            sRGBFlags = m_sRGBFlags;
       }
    }

    // normalToWorld.m03 - total blend factor
    // normalToWorld.m13 - diffuse texture index in atlas
    // normalToWorld.m23 - normal texture index in atlas
    // normalToWorld.m33 - mask texture index in atlas
    [GenerateHLSL]
    public struct DecalData
    {
        public Matrix4x4 worldToDecal;
        public Matrix4x4 normalToWorld;
    };
}
