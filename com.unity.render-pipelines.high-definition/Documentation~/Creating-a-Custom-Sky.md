# **Creating a custom sky**

The High Definition Render Pipeline (HDRP) uses a sky system that allows you to develop your own custom sky with its own properties and Shaders, while still keeping the sky consistent with the lighting pipeline.

To create your own sky, create some scripts to handle the following:

1. [Sky Settings](#SkySettings)
2. [Sky Renderer](#SkyRenderer)
3. [Sky Rendering Shader](#RenderingShader)

## Using your sky renderer

When you complete all of the above steps, your new sky automatically appears in the **Sky Type** drop-down in the [Visual Environment](Override-Visual-Environment.html) override for [Volumes](Volumes.html) in your Unity Project.

<a name="SkySettings"></a>

## Sky Settings

Firstly, create a new class that inherits from **SkySettings**. This new class contains all of the properties specific to the particular sky renderer you want.

You must include the following in this class:

- **SkyUniqueID** attribute: This must be an integer unique to this particular sky; it must not clash with any other SkySettings. The use the SkyType enum to see what values HDRP already uses.
- **GetHashCode**: The sky system uses this function to determine when to re-render the sky reflection cubemap.
- **CreateRenderer**: The sky system uses this function to instantiate the proper renderer.

For example, here’s the [HDRI sky](Override-HDRI-Sky.html) implementation of SkySettings:


```
[VolumeComponentMenu("Sky/HDRI Sky")]
// SkyUniqueID does not need to be part of built-in HDRP SkyType enumeration.
// This is only provided to track IDs used by HDRP natively. 
// You can use any integer value.
[SkyUniqueID((int)SkyType.HDRISky)]
public class HDRISky : SkySettings
{
    [Tooltip("Specify the cubemap HDRP uses to render the sky.")]
    public CubemapParameter hdriSky = new CubemapParameter(null);
    public override SkyRenderer CreateRenderer()
    {
        return new HDRISkyRenderer(this);
    }
    public override int GetHashCode()
    {
        int hash = base.GetHashCode();
        unchecked
        {
            hash = hdriSky.value != null ? hash * 23 + hdriSky.GetHashCode() : hash;
        }
        return hash;
    }
}
```

<a name="SkyRenderer"></a>

## Sky Renderer

Now you must create the class that actually renders the sky, either into a cubemap for lighting or visually for the background. This is where you must implement specific rendering features.

Your SkyRenderer must implement the SkyRenderer interface:
```
public abstract class SkyRenderer
{
    // Method to initialize any resources for the sky rendering (shaders, …)
    public abstract void Build();

    // Method to clean up any previously allocated resources
    public abstract void Cleanup();

    // SkyRenderer is responsible for setting up render targets provided in builtinParams
    public abstract void SetRenderTargets(BuiltinSkyParameters builtinParams);

    // renderForCubemap: When rendering into a cube map, no depth buffer is available so you must make sure not to use depth testing or the depth texture.
    // renderSunDisk: The sky renderer should not render the sun disk when this value is false (this provides consistent baking)
    public abstract void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk);

    // Returns true if the SkySettings you provide are valid.
    public abstract bool IsValid();
}
```
For example, here’s the [HDRI sky](Override-HDRI-Sky.html) implementation of the SkyRenderer:
```
public class HDRISkyRenderer : SkyRenderer
{
    Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be a 3D cube or 2D)
    MaterialPropertyBlock m_PropertyBlock;
    HDRISky m_HdriSkyParams;
    public HDRISkyRenderer(HDRISky hdriSkyParams)
    {
        m_HdriSkyParams = hdriSkyParams;
        m_PropertyBlock = new MaterialPropertyBlock();
    }
    
    public override void Build()
    {
        var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
        m_SkyHDRIMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.hdriSky);
    }
    
    public override void Cleanup()
    {
        CoreUtils.Destroy(m_SkyHDRIMaterial);
    }
    
    public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
    {
        if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
        {
            HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer);
        }
        else
        {
            HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer, builtinParams.depthBuffer);
        }
    }
    
    public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
    {
        m_PropertyBlock.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky);
        m_PropertyBlock.SetVector(HDShaderIDs._SkyParam, new Vector4(m_HdriSkyParams.exposure, m_HdriSkyParams.multiplier, -m_HdriSkyParams.rotation, 0.0f)); // -rotation to match Legacy...
    
        // This matrix needs to be updated at the draw call frequency.
        m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
    
        CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
    }
    
    public override bool IsValid()
    {
        return m_HdriSkyParams != null && m_SkyHDRIMaterial != null;
    }
}
```
<a name="RenderingShader"></a>

## Sky rendering Shader

Finally, you need to actually create the Shader for your sky. The content of this Shader depends on the effects you want to include.

For example, here’s the [HDRI sky](Override-HDRI-Sky.html) implementation of the SkyRenderer.
```
Shader "Hidden/HDRenderPipeline/Sky/HDRISky"    
{
    HLSLINCLUDE
    \#pragma vertex Vert
    \#pragma fragment Frag
    
    \#pragma target 4.5
    \#pragma only_renderers d3d11 ps4 xboxone vulkan metal
    
    \#include "CoreRP/ShaderLibrary/Common.hlsl"
    \#include "CoreRP/ShaderLibrary/Color.hlsl"
    \#include "CoreRP/ShaderLibrary/CommonLighting.hlsl"
    
    TEXTURECUBE(_Cubemap);
    SAMPLER(sampler_Cubemap);
    
    float4   _SkyParam; // x exposure, y multiplier, z rotation
    float4x4 _PixelCoordToViewDirWS; // Actually just 3x3, but Unity can only set 4x4
    
    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };
    
    struct Varyings
    {
        float4 positionCS : SV_POSITION;
    };
    
    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }
    
    float4 Frag(Varyings input) : SV_Target
    {
        // Points towards the Camera
        float3 viewDirWS = normalize(mul(float3(input.positionCS.xy, 1.0), (float3x3)_PixelCoordToViewDirWS));
        // Reverse it to point into the Scene
        float3 dir = -viewDirWS;
    
        // Rotate direction
        float phi = DegToRad(_SkyParam.z);
        float cosPhi, sinPhi;
        sincos(phi, sinPhi, cosPhi);
        float3 rotDirX = float3(cosPhi, 0, -sinPhi);
        float3 rotDirY = float3(sinPhi, 0, cosPhi);
        dir = float3(dot(rotDirX, dir), dir.y, dot(rotDirY, dir));
    
        float3 skyColor = ClampToFloat16Max(SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0).rgb * exp2(_SkyParam.x) * _SkyParam.y);
        return float4(skyColor, 1.0);
    }
    
    ENDHLSL
    
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
    
            HLSLPROGRAM
            ENDHLSL
    
        }
    
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off
    
            HLSLPROGRAM
            ENDHLSL
        }
    
    }
    Fallback Off
}
```
**Note**: The HDRI Sky uses two passes, one that uses a Depth Test for rendering the sky in the background (so that geometry occludes it correctly), and the other that does not use a Depth Test and renders the sky into the reflection cubemap.