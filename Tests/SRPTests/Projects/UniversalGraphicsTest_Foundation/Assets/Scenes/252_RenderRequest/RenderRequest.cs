using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public class RenderRequest : MonoBehaviour
{
    public Camera renderRequestCamera;
    
    public RenderTexture texture2D;
    public RenderTexture texture2DArray;
    public RenderTexture cubeMap;
    public RenderTexture texture3D;

    Texture2D blackTexture;

    void CheckSubmitRenderRequestAPI(Camera cam)
    {
        if (cam == null)
            throw new Exception("Null Camera");

        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();

        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            request.destination = texture2D;
            RenderPipeline.SubmitRenderRequest(cam, request);

            request.destination = texture2DArray;
            for (int i = 0; i < texture2DArray.volumeDepth; i++)
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
        }
    }

    bool m_WaitedOneFrame = false;
    bool m_TriggerOnce = true;

    // Update is called once per frame
    public void Update()
    {
        if (!m_WaitedOneFrame)
        {
            m_WaitedOneFrame = true;
            return;
        }

        if (!m_TriggerOnce)
            return;

        // Check if the texture transfers from SRP to user project can be done
        CheckSubmitRenderRequestAPI(renderRequestCamera);

        m_TriggerOnce = false;
    }

    public void OnDisable()
    {
        Destroy(blackTexture);
    }
}
