# Create custom cloud effects

The High Definition Render Pipeline (HDRP) includes a cloud system which allows you to create your own custom clouds with their own properties and Shaders, while still keeping the clouds consistent with HDRP's lighting pipeline.

To create custom clouds using this system, you need to:

1. Create a script to store [cloud settings](#cloud-settings).
2. Create a script to handle [cloud renderering](#cloud-renderer).
3. Create a [cloud rendering shader](#cloud-rendering-shader).

## Using your cloud renderer

When you complete all of the above steps, your new clouds automatically appear in the **Cloud Type** drop-down in the [Visual Environment](visual-environment-volume-override-reference.md) override for [Volumes](understand-volumes.md) in your Unity Project.

## Cloud Settings

Firstly, create a new class that inherits from **CloudSettings**. This new class contains all of the properties specific to the particular cloud renderer you want.

You must include the following in this class:

- **CloudUniqueID** attribute: This must be an integer unique to this particular cloud component; it must not clash with any other CloudSettings. Check the CloudType enum to see what values HDRP already uses.
- **GetHashCode**: The cloud system uses this function to determine when to re-render the sky reflection cubemap.
- **GetCloudRendererType**: The cloud system uses this function to instantiate the proper renderer.

For an example implementation of CloudSettings, see the following code sample:


```c#
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[VolumeComponentMenu("Sky/New Clouds")]
// CloudUniqueID does not need to be part of built-in HDRP CloudType enumeration.
// This is only provided to track IDs used by HDRP natively.
// You can use any integer value.
[CloudUniqueID(NEW_CLOUD_UNIQUE_ID)]
public class NewCloud : CloudSettings
{
    const int NEW_CLOUD_UNIQUE_ID = 20382390;

    [Tooltip("Specify the cubemap HDRP uses to render the clouds.")]
    public CubemapParameter clouds = new CubemapParameter(null);

    public override Type GetCloudRendererType()
    {
        return typeof(NewCloudRenderer);
    }

    public override int GetHashCode()
    {
        int hash = base.GetHashCode();
        unchecked
        {
            hash = clouds.value != null ? hash * 23 + clouds.GetHashCode() : hash;
        }
        return hash;
    }

    public override int GetHashCode(Camera camera)
    {
        // Implement if your clouds depend on the camera settings (like position for instance)
        return GetHashCode();
    }
}
```

## Cloud Renderer

Now create the class that actually renders the clouds, either into a cubemap for lighting or visually for the background. This is where you must implement specific rendering features.

Your cloud renderer must implement the [CloudRenderer interface](xref:UnityEngine.Rendering.HighDefinition.CloudRenderer):

For an example implementation of the CloudRenderer, see the following code sample:
```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

class NewCloudRenderer : CloudRenderer
{
    public static readonly int _Cubemap = Shader.PropertyToID("_Cubemap");
    public static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");

    Material m_NewCloudMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
    MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

    private static int m_RenderCubemapID = 0; // FragBaking
    private static int m_RenderFullscreenCloudID = 1; // FragRender

    public override void Build()
    {
        m_NewCloudMaterial = CoreUtils.CreateEngineMaterial(GetNewCloudShader());
    }

    // Project dependent way to retrieve a shader.
    Shader GetNewCloudShader()
    {
        // Implement me
        return null;
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_NewCloudMaterial);
    }

    protected override bool Update(BuiltinSkyParameters builtinParams)
    {
        return false;
    }

    public override void RenderClouds(BuiltinSkyParameters builtinParams, bool renderForCubemap)
    {
        using (new ProfilingSample(builtinParams.commandBuffer, "Draw clouds"))
        {
            var newCloud = builtinParams.cloudSettings as NewCloud;

            int passID = renderForCubemap ? m_RenderCubemapID : m_RenderFullscreenCloudID;

            m_PropertyBlock.SetTexture(_Cubemap, newCloud.clouds.value);
            m_PropertyBlock.SetMatrix(_PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_NewCloudMaterial, m_PropertyBlock, passID);
        }
    }
}
```

Note that HDRP creates one instance of the renderer per camera. This means that any data that exists as a member of the renderer is duplicated in memory. This is especially important to be aware of if your cloud renderer manages large amounts of data (like precomputed textures). If this is the case, it is best practise to cache the large data elsewhere and have each CloudRenderer access and use it when necessary.

## Cloud rendering Shader

Finally, create the Shader for your clouds. The content of this Shader depends on the effects you want to include.

For example, the following code sample is the NewCloudRenderer's shader implementation.
```
Shader "Hidden/HDRP/Sky/NewCloud"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

    TEXTURECUBE(_Cubemap);
    SAMPLER(sampler_Cubemap);

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

    float4 RenderClouds(Varyings input, float exposure)
    {
        float3 viewDirWS = GetSkyViewDirWS(input.positionCS.xy);

        // Reverse it to point into the scene
        float3 dir = -viewDirWS;

        float4 clouds = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0).rgba;

        return float4(clouds.rgb * exposure, clouds.a);
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderClouds(input, 1.0);
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return RenderClouds(input, GetCurrentExposureMultiplier());
    }

    ENDHLSL

    SubShader
    {
        // For cubemap
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        // For fullscreen sky
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }
    }
    Fallback Off
}
```
**Note**: The NewCloud example uses two passes, one that uses a Depth Test for rendering the clouds in the background so that geometry occludes them correctly, and the other that does not use a Depth Test and renders the clouds into the reflection cubemap.
In each case, the passes use alpha blending to correctly blend with the sky behind.
