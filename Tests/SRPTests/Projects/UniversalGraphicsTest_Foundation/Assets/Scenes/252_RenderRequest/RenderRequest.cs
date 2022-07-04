using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderRequest : MonoBehaviour
{
    [SerializeField]
    RenderTexture texture2D, texture2DArray, cubeMap, texture3D, singleCamera;
    Texture2D blackTex;

    // Start is called before the first frame update
    void Start()
    {
        Camera cam = GetComponent<Camera>();

        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();

        request.destination = texture2D;
        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, request);

        request.destination = texture2DArray;
        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, request);

        //Set all cubemap faces to black, Switch doesn't initialize the data to black
        blackTex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        Color[] colors = Enumerable.Repeat(Color.black, 256 * 256).ToArray();
        blackTex.SetPixels(colors);
        blackTex.Apply();

        for (int i = 0; i < 6; ++i)
        {
            Graphics.CopyTexture(blackTex, 0, cubeMap, i);
        }

        request.destination = cubeMap;
        request.face = CubemapFace.NegativeX;

        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, request);

        request.destination = texture3D;
        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, request);

        UniversalRenderPipeline.SingleCameraRequest singleCamRequest = new UniversalRenderPipeline.SingleCameraRequest();
        singleCamRequest.destination = singleCamera;
        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, singleCamRequest);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
