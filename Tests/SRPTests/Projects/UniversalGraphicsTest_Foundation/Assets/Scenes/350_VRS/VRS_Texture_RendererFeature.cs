using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

public class VRS_Texture_RendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent;

    class VRS_Texture_CustomRenderPass : ScriptableRenderPass
    {
        public VRS_Texture_CustomRenderPass()
        {
            profilingSampler = new ProfilingSampler("VRS_Texture_RendererFeature");
        }

        private class PassData
        {
            public VRSHistory vrsHistory;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            //const string passName = "Create VRS texture";

           if (!Vrs.IsColorMaskTextureConversionSupported()) return;

           UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
           UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

           if (cameraData.historyManager == null)
               return;

           cameraData.historyManager.RequestAccess<VRSHistory>();

           VRSHistory vrsHistory = cameraData.historyManager.GetHistoryForWrite<VRSHistory>();
           if (vrsHistory == null)
               return;

           vrsHistory.Update(ref cameraData.cameraTargetDescriptor);

           TextureHandle sriTextureHandle = renderGraph.ImportShadingRateImageTexture(vrsHistory.GetSRITexture());
           TextureHandle sriColorMask = renderGraph.ImportTexture(vrsHistory.GetSRIColorMask());

           Vrs.ColorMaskTextureToShadingRateImage(renderGraph, sriTextureHandle, sriColorMask, TextureDimension.Tex2D, true);

           vrsHistory.importedSRITextureHandle = sriTextureHandle;
           Debug.Assert(sriTextureHandle.IsValid(), "Create imported sri is not valid.");
        }
    }

    VRS_Texture_CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new VRS_Texture_CustomRenderPass();
        m_ScriptablePass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}

public class VRSHistory : CameraHistoryItem
{
    private int m_Id_SRIHandle;
    private RTHandle m_SRIColorMask;
    private RenderTextureDescriptor m_Descriptor;
    private Hash128 m_DescKey;

    /// <summary>
    /// Called internally on instance creation.
    /// Sets up RTHandle ids.
    /// </summary>
    public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
    {
        base.OnCreate(owner, typeId);
        m_Id_SRIHandle = MakeId(0);
    }

    public RTHandle GetSRITexture()
    {
        return GetCurrentFrameRT(m_Id_SRIHandle);
    }

    public RTHandle GetSRIColorMask()
    {
        return m_SRIColorMask;
    }

    public TextureHandle importedSRITextureHandle
    {
        get;set;
    }

    internal bool IsAllocated()
    {
        return GetSRITexture() != null;
    }

    // True if the desc changed, graphicsFormat etc.
    internal bool IsDirty(ref RenderTextureDescriptor desc)
    {
        return m_DescKey != Hash128.Compute(ref desc);
    }

    RTHandle GenerateVRSColorMask(int width, int height)
    {
        uint Pack(Color32 c)
        {
            return (uint)(c.a << 24 | c.b << 16 | c.g << 8 | c.r);
        }

        var vrsLut = VrsLut.CreateDefault();
        uint c1 = Pack(vrsLut[ShadingRateFragmentSize.FragmentSize1x1].linear);
        uint c2 = Pack(vrsLut[ShadingRateFragmentSize.FragmentSize2x2].linear);
        uint c4 = Pack(vrsLut[ShadingRateFragmentSize.FragmentSize4x4].linear);

        Texture2D CPUColorMask = new Texture2D(width, height, GraphicsFormat.R8G8B8A8_UNorm,
            TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
        var pixels = new uint[width * height];
        for(uint y = 0; y < height; y++)
        {
            for(uint x = 0; x < width; x++)
            {
                uint color = c1;
                if (x <= y)
                {
                    color = c4;
                }
                else if (x <= y + height)
                {
                    color = c2;
                }

                pixels[y * width + x] = color;
            }
        }

        CPUColorMask.SetPixelData(pixels, 0, 0);
        CPUColorMask.Apply(false);

        return RTHandles.Alloc(CPUColorMask);
    }

    private void Alloc(ref RenderTextureDescriptor desc)
    {
        Debug.Log("Allocating VRS history texture");

        AllocHistoryFrameRT(m_Id_SRIHandle, 1, ref desc, FilterMode.Point, "SRITexture");

        m_SRIColorMask = GenerateVRSColorMask(desc.width, desc.height);

        m_Descriptor = desc;
        m_DescKey = Hash128.Compute(ref desc);
    }

    /// <summary>
    /// Release the history texture(s).
    /// </summary>
    public override void Reset()
    {
        Debug.Log("Release VRS history texture");
        ReleaseHistoryFrameRT(m_Id_SRIHandle);
        m_SRIColorMask?.Release();
    }

    RenderTextureDescriptor GetVrsDesc(ref RenderTextureDescriptor cameraDesc)
    {
        var tileSize = ShadingRateImage.GetAllocTileSize(cameraDesc.width, cameraDesc.height);
        RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(tileSize.x, tileSize.y, GraphicsFormat.R8_UInt, 0);
        textureProperties.enableRandomWrite = true;
        textureProperties.enableShadingRate = true;
        textureProperties.autoGenerateMips = false;
        return textureProperties;
    }

    // Return true if the RTHandles were reallocated.
    internal bool Update(ref RenderTextureDescriptor cameraDesc)
    {
        if (cameraDesc.width > 0 && cameraDesc.height > 0 && cameraDesc.graphicsFormat != GraphicsFormat.None)
        {
            var vrsDesc = GetVrsDesc(ref cameraDesc);

            if (IsDirty(ref vrsDesc))
                Reset();

            if (!IsAllocated())
            {
                Alloc(ref vrsDesc);
                return true;
            }
        }

        // Import is from the previous frame. Imports are not persistent. Clear it.
        importedSRITextureHandle = TextureHandle.nullHandle;

        return false;
    }
}

