using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;

public class AOVOutputBuffer : MonoBehaviour
{
    public RenderTexture _outputTexture = null;
    public MaterialSharedProperty _materialOutput = MaterialSharedProperty.None;
    public DebugFullScreen _bufferOutput = DebugFullScreen.None;

    RTHandle _rt;

    RTHandle RTAllocator(AOVBuffers bufferID)
    {
        return _rt ?? (_rt = RTHandles.Alloc(_outputTexture.width, _outputTexture.height));
    }
    void AovCallback(
        CommandBuffer cmd,
        List<RTHandle> buffers,
        RenderOutputProperties outProps
        )
    {
        if(buffers.Count > 0)
        {
            cmd.Blit(buffers[0], _outputTexture);
        }
    }

    AOVRequestDataCollection BuildAovRequest()
    {
        var aovRequest = AOVRequest.NewDefault();
        if (_materialOutput != MaterialSharedProperty.None)
            aovRequest.SetFullscreenOutput(_materialOutput);

        if (_bufferOutput != DebugFullScreen.None)
            aovRequest.SetFullscreenOutput(_bufferOutput);

        return new AOVRequestBuilder().Add(
            aovRequest,
            RTAllocator,
            null, // lightFilter
            new[] {
                    AOVBuffers.Output,
            },
            AovCallback
        ).Build();
    }

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(BuildAovRequest());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
