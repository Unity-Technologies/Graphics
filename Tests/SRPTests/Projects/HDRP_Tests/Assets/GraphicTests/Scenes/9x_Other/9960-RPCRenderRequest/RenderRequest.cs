using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Non-graphics related parts of the RenderRequest API are tested in HDRP_PlayMode
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class RenderRequest : MonoBehaviour
{
    public Camera renderRequestCamera;
    
    public RenderTexture texture2D;
    public RenderTexture texture2DArray;
    public RenderTexture cubeMap;
    public RenderTexture texture3D;

    Texture2D blackTexture;
    RTHandle aovDepthTexture;

    bool m_IsAOVCallbackTriggered = false;

    void CheckSubmitRenderRequestAPI(Camera cam)
    {
        if (cam == null)
            throw new Exception("Null Camera");

        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();
        
        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            // Adding AOV request to HDRP in parallel to SRP render requests, it should not be triggered by them
            cam.gameObject.GetComponent<HDAdditionalCameraData>().SetAOVRequests(BuildAovRequest());

            request.destination = texture2D;
            RenderPipeline.SubmitRenderRequest(cam, request);

            request.destination = texture2DArray;
            for(int i = 0; i < texture2DArray.volumeDepth; i++)
            {
                request.slice = i;
                RenderPipeline.SubmitRenderRequest(cam, request);
            }

            //Set all cubemap faces to black, Switch doesn't initialize the data to black
            blackTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] colors = new Color[256 * 256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            blackTexture.SetPixels(colors);
            blackTexture.Apply();
            for (int i = 0; i < 6; ++i)
            {
                Graphics.CopyTexture(blackTexture, 0, cubeMap, i);
            }

            var faces = new[] {
                CubemapFace.NegativeX, CubemapFace.PositiveX,
                CubemapFace.NegativeY, CubemapFace.PositiveY,
                CubemapFace.NegativeZ, CubemapFace.PositiveZ
            };

            request.destination = cubeMap;
            request.slice = 0;
            foreach (var face in faces)
            {
                request.face = face;
                RenderPipeline.SubmitRenderRequest(cam, request);
            }

            request.destination = texture3D;
            for (int i = 0; i < texture3D.volumeDepth; i++)
            {
                request.slice = i;
                RenderPipeline.SubmitRenderRequest(cam, request);
            }

            // StandardRequests should not trigger AOV callbacks
            if (m_IsAOVCallbackTriggered)
                throw new System.Exception("AOV callback should NOT have been triggered");
        }        
    }

    // Callback is done by main thread sequentially after rendering, no data race
    void AovCallback(
        CommandBuffer cmd,
        List<RTHandle> buffers,
        RenderOutputProperties outProps
    )
    {
        m_IsAOVCallbackTriggered = true;
    }

    RTHandle RTAllocator(AOVBuffers bufferID)
    {
        if(bufferID == AOVBuffers.DepthStencil)
            return aovDepthTexture ?? 
            (aovDepthTexture = RTHandles.Alloc(
                256, 256, 1, DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));
    
        return null;
    }

    AOVRequestDataCollection BuildAovRequest()
    {
        return new AOVRequestBuilder().Add(
            AOVRequest.NewDefault(),
            RTAllocator,
            null, // lightFilter
            new[]
            {
                AOVBuffers.DepthStencil,
            },
            AovCallback
        ).Build();
    }

    public void OnDisable()
    {
        RTHandles.Release(aovDepthTexture);
        Destroy(blackTexture);
    }

    bool m_TriggerOnce = true;

    // Update is called once per frame
    public void Update()
    {
        if (!m_TriggerOnce)
            return;

        // Check if the texture transfers from SRP to user project can be done
        CheckSubmitRenderRequestAPI(renderRequestCamera);

        m_TriggerOnce = false;
    }
}
