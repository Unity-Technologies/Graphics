using System.Text;
using Unity.Collections;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using StringBuilder = System.Text.StringBuilder;

class VrsTestPass : CustomPass
{
    private VrsLut m_VrsLut;

    private Texture2D m_ColorMaskTexture;
    private NativeArray<uint> m_Pixels;
    private Vector2Int m_FullSize;
    private Vector2Int m_PrevSize;

    private Texture2D m_VrsClearMask;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        m_VrsLut = VrsLut.CreateDefault();

        // Vrs CPU texture
        m_FullSize = RTHandles.CalculateDimensions(Vector2.one);
        m_ColorMaskTexture = new Texture2D(m_FullSize.x, m_FullSize.y, GraphicsFormat.R8G8B8A8_UNorm,
            TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
        m_Pixels = new NativeArray<uint>(m_FullSize.x * m_FullSize.y, Allocator.Persistent);
        UpdateMaskTexture(m_FullSize);

        // Clear texture
        m_VrsClearMask = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm,
            TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
        m_VrsClearMask.SetPixel(0,0, m_VrsLut[ShadingRateFragmentSize.FragmentSize1x1]);
        m_VrsClearMask.Apply(false, true);

        // VrsInfo
        if (true)
        {
            bool vrsSupported = SystemInfo.supportsVariableRateShading;
            string vrsFormat = GraphicsFormatUtility.GetFormatString(ShadingRateInfo.graphicsFormat);
            Debug.Log($"VRSInfo - Supported:{vrsSupported} VrsDraw:{ShadingRateInfo.supportsPerDrawCall} " +
                      $"VrsImage:{ShadingRateInfo.supportsPerImageTile} " +
                      $"TileSize:{ShadingRateInfo.imageTileSize.x}x{ShadingRateInfo.imageTileSize.y} Format:{vrsFormat}");
            if (vrsSupported)
            {
                StringBuilder str = new StringBuilder(1024);
                foreach (var fragSize in ShadingRateInfo.availableFragmentSizes)
                {
                    str.Append($"{fragSize.ToString()}, ");
                }
                Debug.Log($"FragSizes:\n{str.ToString()}");
            }
        }
    }

    void UpdateMaskTexture(Vector2Int patternSize)
    {
        uint Pack(Color32 c)
        {
            return (uint)(c.a << 24 | c.b << 16 | c.g << 8 | c.r);
        }

        uint c1 = Pack(m_VrsLut[ShadingRateFragmentSize.FragmentSize1x1].linear);
        uint c2 = Pack(m_VrsLut[ShadingRateFragmentSize.FragmentSize2x2].linear);
        uint c4 = Pack(m_VrsLut[ShadingRateFragmentSize.FragmentSize4x4].linear);

        for(uint y = 0; y < m_FullSize.y; y++)
        {
            for(uint x = 0; x < m_FullSize.x; x++)
            {
                uint color = c1;
                if( y < patternSize.y / 2)
                    color = c4;
                else if (x <= patternSize.x / 2)
                {
                    color = c2;
                }
                m_Pixels[(int)(y * m_FullSize.x + x)] = color;
            }
        }

        m_ColorMaskTexture.SetPixelData(m_Pixels, 0, 0); // NativeArray to avoid allocations.
        m_ColorMaskTexture.Apply(false);
        m_PrevSize = patternSize;
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!Vrs.IsColorMaskTextureConversionSupported())
        {
            Debug.LogWarning("VrsTestPass color to shading rate is not supported!");
            return;
        }

        if (m_ColorMaskTexture != null)
        {
            // The resolution might have changed and we (re)use only a part of the max-sized buffer.
            // Update VRS texture pattern to only affect the used portion of the camera buffer as the VRS texture is used fully.
            var curSize = new Vector2Int(ctx.hdCamera.actualWidth, ctx.hdCamera.actualHeight);
            if(curSize != m_PrevSize)
                UpdateMaskTexture(curSize);

            // Convert custom CPU texture to VRS texture.
            RTHandle sriTexture = ctx.shadingRateBuffer;
            if (sriTexture != null)
            {
                // Clear the Vrs texture just for clarity.
                Vrs.ColorMaskTextureToShadingRateImageDispatch(ctx.cmd, sriTexture, (Texture)m_VrsClearMask);

                // Convert the procedural CPU (test) texture to VRS.
                Vrs.ColorMaskTextureToShadingRateImageDispatch(ctx.cmd, sriTexture, (Texture)m_ColorMaskTexture);
            }
        }
    }

    protected override void Cleanup()
    {
        m_Pixels.Dispose();
        m_VrsLut = null;
        CoreUtils.Destroy(m_ColorMaskTexture);

    }
}
