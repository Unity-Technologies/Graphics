using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteAlways]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class AOVLightingMode : MonoBehaviour
{
    public RenderTexture diffuse;
    public RenderTexture specular;
    public RenderTexture directDiffuse;
    public RenderTexture directSpecular;
    public RenderTexture indirectDiffuse;
    public RenderTexture reflection;
    public RenderTexture emissive;
    public RenderTexture refraction;

    private static RTHandle rtColor;

    RTHandle RTAllocator(AOVBuffers bufferID)
    {
        if (bufferID == AOVBuffers.Color)
        {
            return rtColor ?? (rtColor = RTHandles.Alloc(diffuse.width, diffuse.height, 1, DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));
        }

        return null;
    }


    AOVRequestDataCollection BuildAovRequest()
    {
        var aovRequestBuilder = new AOVRequestBuilder();
        var aovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.DiffuseOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (diffuse != null) cmd.Blit(textures[0], diffuse); } );
        aovRequest.SetFullscreenOutput(LightingProperty.SpecularOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (specular != null) cmd.Blit(textures[0], specular); });
        aovRequest.SetFullscreenOutput(LightingProperty.DirectDiffuseOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (directDiffuse != null) cmd.Blit(textures[0], directDiffuse); });
        aovRequest.SetFullscreenOutput(LightingProperty.DirectSpecularOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (directSpecular != null) cmd.Blit(textures[0], directSpecular); });
        aovRequest.SetFullscreenOutput(LightingProperty.IndirectDiffuseOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (indirectDiffuse != null) cmd.Blit(textures[0], indirectDiffuse); });
        aovRequest.SetFullscreenOutput(LightingProperty.ReflectionOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (reflection != null) cmd.Blit(textures[0], reflection); });
        aovRequest.SetFullscreenOutput(LightingProperty.EmissiveOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (emissive != null) cmd.Blit(textures[0], emissive); });
        aovRequest.SetFullscreenOutput(LightingProperty.RefractionOnly);
        aovRequestBuilder.Add(aovRequest, RTAllocator, null, new[] { AOVBuffers.Color }, (cmd, textures, properties) => { if (refraction != null) cmd.Blit(textures[0], refraction); });

        return aovRequestBuilder.Build();
    }

    void OnDisable()
    {
        var add = GetComponent<HDAdditionalCameraData>();
        add.SetAOVRequests(null);
    }
    
    void OnValidate()
    {
        OnDisable();
        OnEnable();
    }

    void OnEnable()
    {
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(BuildAovRequest());
    }

    void Start()
    {
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(null);
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(BuildAovRequest());
    }
}
