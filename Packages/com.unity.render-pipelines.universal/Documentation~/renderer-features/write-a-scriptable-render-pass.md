# Write a Scriptable Render Pass

The following example is a `ScriptableRenderPass` instance that performs the following steps:

1. Creates a temporary render texture using the `RenderTextureDescriptor` API.
2. Applies two passes of the [custom shader](#example-shader) to the camera output using the `RTHandle` and the `Blit` API.

After you write a Scriptable Render Pass, you can inject the pass using one of the following methods:

- [Use the `RenderPipelineManager` API](../customize/inject-render-pass-via-script.md)
- [Use a Scriptable Renderer Feature](scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md)

## Create the scriptable Render Pass

This section demonstrates how to create a scriptable Render Pass.

1. Create a new C# script and name it `RedTintRenderPass.cs`.

2. In the script, remove the code that Unity inserted in the `RedTintRenderPass` class. Add the following `using` directive:

    ```C#
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    ```

3. Create the `RedTintRenderPass` class that inherits from the **ScriptableRenderPass** class.

    ```C#
    public class RedTintRenderPass : ScriptableRenderPass
    ```

4. Add the `Execute` method to the class. Unity calls this method every frame, once for each camera. This method lets you implement the rendering logic of the scriptable Render Pass.

    ```C#
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    { }
    ```

Below is the complete code for the RedTintRenderPass.cs file from this section.

```C#
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RedTintRenderPass : ScriptableRenderPass
{
    public override void Execute(ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        
    }
}
```

## Implement the settings for the custom render pass

1. Add a field for the Material, and the constructor that uses the field.

    ```C#
    private Material material;

    public RedTintRenderPass(Material material)
    {
        this.material = material;
    }
    ```

2. Add the `RenderTextureDescriptor` field and initialize it in the constructor:

    ```C#
    using UnityEngine;

    private RenderTextureDescriptor textureDescriptor;

    public RedTintRenderPass(Material material)
    {
        this.material = material;

        textureDescriptor = new RenderTextureDescriptor(Screen.width,
            Screen.height, RenderTextureFormat.Default, 0);
    }
    ```

3. Declare the `RTHandle` field to store the reference to the temporary red tint texture. 

    ```C#
    private RTHandle textureHandle;
    ```

4. Implement the `Configure` method. Unity calls this method before executing the render pass.

    ```C#
    public override void Configure(CommandBuffer cmd,
        RenderTextureDescriptor cameraTextureDescriptor)
    {
        //Set the red tint texture size to be the same as the camera target size.
        textureDescriptor.width = cameraTextureDescriptor.width;
        textureDescriptor.height = cameraTextureDescriptor.height;

        //Check if the descriptor has changed, and reallocate the RTHandle if necessary.
        RenderingUtils.ReAllocateIfNeeded(ref textureHandle, textureDescriptor);
    }
    ```

5. Use the Blit method to apply the two passes from the custom shader to the camera output.

    ```C#
    public override void Execute(ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        //Get a CommandBuffer from pool.
        CommandBuffer cmd = CommandBufferPool.Get();

        RTHandle cameraTargetHandle =
            renderingData.cameraData.renderer.cameraColorTargetHandle;

        // Blit from the camera target to the temporary render texture,
        // using the first shader pass.
        Blit(cmd, cameraTargetHandle, textureHandle, material, 0);
        // Blit from the temporary render texture to the camera target,
        // using the second shader pass.
        Blit(cmd, textureHandle, cameraTargetHandle, material, 1);

        //Execute the command buffer and release it back to the pool.
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    ```

6. Implement the `Dispose` method that destroys the Material and the temporary render texture after the render pass execution.

    ```C#
    public void Dispose()
    {
        #if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
                    Object.Destroy(material);
                }
                else
                {
                    Object.DestroyImmediate(material);
                }
        #else
                Object.Destroy(material);
        #endif
        
        if (textureHandle != null) textureHandle.Release();
    }
    ```

### <a name="code-render-pass"></a>Custom render pass code

Below is the complete code for the custom Render Pass script.

```C#
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RedTintRenderPass : ScriptableRenderPass
{
    private Material material;

    private RenderTextureDescriptor textureDescriptor;
    private RTHandle textureHandle;

    public RedTintRenderPass(Material material)
    {
        this.material = material;

        textureDescriptor = new RenderTextureDescriptor(Screen.width,
            Screen.height, RenderTextureFormat.Default, 0);
    }

    public override void Configure(CommandBuffer cmd,
        RenderTextureDescriptor cameraTextureDescriptor)
    {
        // Set the texture size to be the same as the camera target size.
        textureDescriptor.width = cameraTextureDescriptor.width;
        textureDescriptor.height = cameraTextureDescriptor.height;

        // Check if the descriptor has changed, and reallocate the RTHandle if necessary
        RenderingUtils.ReAllocateIfNeeded(ref textureHandle, textureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        //Get a CommandBuffer from pool.
        CommandBuffer cmd = CommandBufferPool.Get();

        RTHandle cameraTargetHandle =
            renderingData.cameraData.renderer.cameraColorTargetHandle;

        // Blit from the camera target to the temporary render texture,
        // using the first shader pass.
        Blit(cmd, cameraTargetHandle, textureHandle, material, 0);
        // Blit from the temporary render texture to the camera target,
        // using the second shader pass.
        Blit(cmd, textureHandle, cameraTargetHandle, material, 1);

        //Execute the command buffer and release it back to the pool.
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
    #if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Object.Destroy(material);
        }
        else
        {
            Object.DestroyImmediate(material);
        }
    #else
            Object.Destroy(material);
    #endif

        if (textureHandle != null) textureHandle.Release();
    }
}
```

## <a name="example-shader"></a>The custom shader for the red tint effect

This section contains the code for the custom shader that implements the red tint effect.

```c++
Shader "CustomEffects/RedTint"
{
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    
        float4 RedTint (Varyings input) : SV_Target
        {
            float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
            return float4(1, color.gb, 1);
        }

        float4 SimpleBlit (Varyings input) : SV_Target
        {
            float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
            return float4(color.rgb, 1);
        }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "RedTint"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment RedTint
            
            ENDHLSL
        }
        
        Pass
        {
            Name "SimpleBlit"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment SimpleBlit
            
            ENDHLSL
        }
    }
}
```