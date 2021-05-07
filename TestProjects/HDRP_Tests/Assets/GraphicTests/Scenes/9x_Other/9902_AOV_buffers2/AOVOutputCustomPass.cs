using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;

public class AOVOutputCustomPass : MonoBehaviour
{
    public RenderTexture _outputTexture = null;
    public CustomPassInjectionPoint _injectionPoint = CustomPassInjectionPoint.BeforePostProcess;

    RTHandle _rt;

    RTHandle RTAllocator(AOVBuffers bufferID)
    {
        return _rt ?? (_rt = RTHandles.Alloc(_outputTexture.width, _outputTexture.height));
    }
    void AovCallbackEx(
        CommandBuffer cmd,
        List<RTHandle> buffers,
        List<RTHandle> customBuffers,
        RenderOutputProperties outProps
        )
    {
        if (buffers.Count > 0)
        {
            cmd.Blit(buffers[0], _outputTexture);
        }

        if (customBuffers.Count > 0)
        {
            cmd.Blit(customBuffers[0], _outputTexture);
        }
    }

    AOVRequestDataCollection BuildAovRequest()
    {
        var aovRequest = AOVRequest.NewDefault();
        CustomPassAOVBuffers[] customPassAovBuffers = null;
        customPassAovBuffers = new[] { new CustomPassAOVBuffers(CustomPassInjectionPoint.BeforePostProcess, CustomPassAOVBuffers.OutputType.CustomPassBuffer) };

        var bufAlloc = _rt ?? (_rt = RTHandles.Alloc(_outputTexture.width, _outputTexture.height));

        return new AOVRequestBuilder().Add(
            aovRequest,
            RTAllocator,
            null, // lightFilter
            null,
            customPassAovBuffers,
            bufferId => bufAlloc,
            AovCallbackEx
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
