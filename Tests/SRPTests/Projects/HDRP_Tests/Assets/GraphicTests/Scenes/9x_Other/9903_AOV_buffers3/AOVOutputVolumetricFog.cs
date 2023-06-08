using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(HDAdditionalCameraData))]
public class AOVOutputVolumetricFog : MonoBehaviour
{
    [SerializeField] RenderTexture _volumetricFogTexture = null;
    RTHandle _volumetricFogRT;

    Material _material;
    MaterialPropertyBlock _props;

    RTHandle RTAllocator(AOVBuffers bufferID)
    {
        return _volumetricFogRT ??
            (_volumetricFogRT = RTHandles.Alloc(
            _volumetricFogTexture.width, _volumetricFogTexture.height, 1,
            DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));
    }

    void AovCallback(
        CommandBuffer cmd,
        List<RTHandle> buffers,
        RenderOutputProperties outProps
    )
    {
        if (_material == null)
        {
            var shader = Shader.Find("Hidden/HdrpAovTest/AovShader");   // TODO
            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_props == null)
            _props = new MaterialPropertyBlock();

        _props.SetTexture("_ColorTexture", buffers[0]);

        CoreUtils.DrawFullScreen(cmd, _material, _volumetricFogTexture, _props);
    }

    AOVRequestDataCollection BuildAovRequest()
    {
        var aovRequest = AOVRequest.NewDefault();
        aovRequest.SetFullscreenOutput(DebugFullScreen.VolumetricFogOnly);

        return new AOVRequestBuilder().Add(
            aovRequest,
            RTAllocator,
            null, // lightFilter
            new[]
            {
                AOVBuffers.VolumetricFog
            },
            AovCallback
            ).Build();
    }

    void OnDisable()
    {
        RTHandles.Release(_volumetricFogRT);
    }

    void Start()
    {
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(BuildAovRequest());
    }
}
