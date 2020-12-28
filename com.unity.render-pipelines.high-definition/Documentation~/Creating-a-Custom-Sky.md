# **Creating a custom sky**

The High Definition Render Pipeline (HDRP) uses a sky system that allows you to develop your own custom sky with its own properties and Shaders, while still keeping the sky consistent with the lighting pipeline.

To create your own sky, create some scripts to handle the following:

1. [Sky Settings](#SkySettings)
2. [Sky Renderer](#SkyRenderer)
3. [Sky Rendering Shader](#RenderingShader)

## Using your sky renderer

When you complete all of the above steps, your new sky automatically appears in the **Sky Type** drop-down in the [Visual Environment](Override-Visual-Environment.md) override for [Volumes](Volumes.md) in your Unity Project.

<a name="SkySettings"></a>

## Sky Settings

Firstly, create a new class that inherits from **SkySettings**. This new class contains all of the properties specific to the particular sky renderer you want.

You must include the following in this class:

- **SkyUniqueID** attribute: This must be an integer unique to this particular sky; it must not clash with any other SkySettings. Use the SkyType enum to see what values HDRP already uses.
- **GetHashCode**: The sky system uses this function to determine when to re-render the sky reflection cubemap.
- **GetSkyRendererType**: The sky system uses this function to instantiate the proper renderer.

For example, here’s the [HDRI sky](Override-HDRI-Sky.md) implementation of SkySettings:


```c#
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[VolumeComponentMenu("Sky/New Sky")]
// SkyUniqueID does not need to be part of built-in HDRP SkyType enumeration.
// This is only provided to track IDs used by HDRP natively.
// You can use any integer value.
[SkyUniqueID(NEW_SKY_UNIQUE_ID)]
public class NewSky : SkySettings
{
    const int NEW_SKY_UNIQUE_ID = 20382390;

    [Tooltip("Specify the cubemap HDRP uses to render the sky.")]
    public CubemapParameter hdriSky = new CubemapParameter(null);

    public override Type GetSkyRendererType()
    {
        return typeof(NewSkyRenderer);
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

    public override int GetHashCode(Camera camera)
    {
        // Implement if your sky depends on the camera settings (like position for instance)
        return GetHashCode();
    }
}
```

<a name="SkyRenderer"></a>

## Sky Renderer

Now you must create the class that actually renders the sky, either into a cubemap for lighting or visually for the background. This is where you must implement specific rendering features.

Your SkyRenderer must implement the SkyRenderer interface:
```c#
    public abstract class SkyRenderer
    {
        int m_LastFrameUpdate = -1;

        /// <summary>
        /// Called on startup. Create resources used by the renderer (shaders, materials, etc).
        /// </summary>
        public abstract void Build();

        /// <summary>
        /// Called on cleanup. Release resources used by the renderer.
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// HDRP calls this function once every frame. Implement it if your SkyRenderer needs to iterate independently of the user defined update frequency (see SkySettings UpdateMode).
        /// </summary>
        /// <returns>True if the update determines that sky lighting needs to be re-rendered. False otherwise.</returns>
        protected virtual bool Update(BuiltinSkyParameters builtinParams) { return false; }

        /// <summary>
        /// Implements actual rendering of the sky. HDRP calls this when rendering the sky into a cubemap (for lighting) and also during main frame rendering.
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the sky.</param>
        /// <param name="renderForCubemap">Pass in true if you want to render the sky into a cubemap for lighting. This is useful when the sky renderer needs a different implementation in this case.</param>
        /// <param name="renderSunDisk">If the sky renderer supports the rendering of a sun disk, it must not render it if this is set to false.</param>
        public abstract void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk);
```
For example, here’s the a simple implementation of the SkyRenderer:
```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

class NewSkyRenderer : SkyRenderer
{
    public static readonly int _Cubemap = Shader.PropertyToID("_Cubemap");
    public static readonly int _SkyParam = Shader.PropertyToID("_SkyParam");
    public static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");

    Material m_NewSkyMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
    MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

    private static int m_RenderCubemapID = 0; // FragBaking
    private static int m_RenderFullscreenSkyID = 1; // FragRender

    public override void Build()
    {
        m_NewSkyMaterial = CoreUtils.CreateEngineMaterial(GetNewSkyShader());
    }

    // Project dependent way to retrieve a shader.
    Shader GetNewSkyShader()
    {
        // Implement me
        return null;
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_NewSkyMaterial);
    }

    protected override bool Update(BuiltinSkyParameters builtinParams)
    {
        return false;
    }

    public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
    {
        using (new ProfilingSample(builtinParams.commandBuffer, "Draw sky"))
        {
            var newSky = builtinParams.skySettings as NewSky;

            int passID = renderForCubemap ? m_RenderCubemapID : m_RenderFullscreenSkyID;

            float intensity = GetSkyIntensity(newSky, builtinParams.debugSettings);
            float phi = -Mathf.Deg2Rad * newSky.rotation.value; // -rotation to match Legacy
            m_PropertyBlock.SetTexture(_Cubemap, newSky.hdriSky.value);
            m_PropertyBlock.SetVector(_SkyParam, new Vector4(intensity, 0.0f, Mathf.Cos(phi), Mathf.Sin(phi)));
            m_PropertyBlock.SetMatrix(_PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_NewSkyMaterial, m_PropertyBlock, passID);
        }
    }
}

```
### Important note:
If your sky renderer has to manage heavy data (like precomputed textures or similar things) then particular care has to be taken. Indeed, one instance of the renderer will exist per camera so by default if this data is a member of the renderer, it will also be duplicated in memory.
Since each sky renderer can have very different needs, the responsbility to share this kind of data is the renderer's and need to be implemented by the user.

<a name="RenderingShader"></a>

## Sky rendering Shader

Finally, you need to actually create the Shader for your sky. The content of this Shader depends on the effects you want to include.

For example, here’s the [HDRI sky](Override-HDRI-Sky.md) implementation of the SkyRenderer.
```
Shader "Hidden/HDRP/Sky/NewSky"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

    TEXTURECUBE(_Cubemap);
    SAMPLER(sampler_Cubemap);

    float4 _SkyParam; // x exposure, y multiplier, zw rotation (cosPhi and sinPhi)

    #define _Intensity          _SkyParam.x
    #define _CosPhi             _SkyParam.z
    #define _SinPhi             _SkyParam.w
    #define _CosSinPhi          _SkyParam.zw

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    float3 RotationUp(float3 p, float2 cos_sin)
    {
        float3 rotDirX = float3(cos_sin.x, 0, -cos_sin.y);
        float3 rotDirY = float3(cos_sin.y, 0,  cos_sin.x);

        return float3(dot(rotDirX, p), p.y, dot(rotDirY, p));
    }

    float4 GetColorWithRotation(float3 dir, float exposure, float2 cos_sin)
    {
        dir = RotationUp(dir, cos_sin);
        float3 skyColor = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0).rgb * _Intensity * exposure;
        skyColor = ClampToFloat16Max(skyColor);

        return float4(skyColor, 1.0);
    }

    float4 RenderSky(Varyings input, float exposure)
    {
        float3 viewDirWS = GetSkyViewDirWS(input.positionCS.xy);

        // Reverse it to point into the scene
        float3 dir = -viewDirWS;

        return GetColorWithRotation(dir, exposure, _CosSinPhi);
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderSky(input, 1.0);
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return RenderSky(input, GetCurrentExposureMultiplier());
    }

    ENDHLSL

    SubShader
    {
        // Regular New Sky
        // For cubemap
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        // For fullscreen Sky
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }
    }
    Fallback Off
}

```
**Note**: The NewSky example uses two passes, one that uses a Depth Test for rendering the sky in the background (so that geometry occludes it correctly), and the other that does not use a Depth Test and renders the sky into the reflection cubemap.
