---
uid: um-srp-creating-simple-render-loop
---

# Create a simple render loop in a custom render pipeline

A render loop is the term for all of the rendering operations that take place in a single frame. This page contains information on creating a simple render loop in a custom render pipeline that is based on Unity's Scriptable Render Pipeline.

The code examples on this page demonstrate the basic principles of using the Scriptable Render Pipeline. You can use this information to build your own custom Scriptable Render Pipeline, or to understand how Unity's prebuilt Scriptable Render Pipelines work. 

## Preparing your project

Before you begin writing the code for your render loop, you must prepare your project.

The steps are as follows:

1. [Create an SRP-compatible shader](#creating-unity-shader).
2. [Create one or more GameObjects to render](#creating-gameobject).
3. [Create the basic structure of your custom SRP](#creating-srp).
4. *Optional:* If you plan to extend your simple custom SRP to add more complex functionality, install the SRP Core package. The SRP Core package includes the [SRP Core shader library](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/index.html) (which you can use to make your shaders SRP Batcher compatible), and utility functions for common operations.

<a name = "creating-unity-shader"></a>

### Creating an SRP-compatible shader

In the Scriptable Render Pipeline, you use the `LightMode` Pass tag to determine how to draw geometry. For more information on Pass tags, see [ShaderLab: assigning tags to a Pass](https://docs.unity3d.com/6000.0/Documentation/Manual/SL-PassTags).

This task shows you how to create a very simple unlit Shader object with a LightMode Pass tag value of `ExampleLightModeTag`.

1. Create a new shader asset in your project. For instructions on creating a shader asset, see [Shader assets](https://docs.unity3d.com/6000.0/Documentation/Manual/class-Shader).
2. In your Project view, double click the shader asset to open the shader source code in a text editor.
3. Replace the existing code with the following:

```
// This defines a simple unlit Shader object that is compatible with a custom Scriptable Render Pipeline.
// It applies a hardcoded color, and demonstrates the use of the LightMode Pass tag.
// It is not compatible with SRP Batcher.

Shader "Examples/SimpleUnlitColor"
{
    SubShader
    {
        Pass
        {
            // The value of the LightMode Pass tag must match the ShaderTagId in ScriptableRenderContext.DrawRenderers
            Tags { "LightMode" = "ExampleLightModeTag"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

	float4x4 unity_MatrixVP;
            float4x4 unity_ObjectToWorld;

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float4 worldPos = mul(unity_ObjectToWorld, IN.positionOS);
                OUT.positionCS = mul(unity_MatrixVP, worldPos);
                return OUT;
            }

            float4 frag (Varyings IN) : SV_TARGET
            {
                return float4(0.5,1,0.5,1);
            }
            ENDHLSL
        }
    }
}
```

<a name = "creating-gameobject"></a>

### Creating a GameObject to render

To test that your render loop works, you must create something to render. This task shows you how to put GameObjects in your scene that use the SRP-compatible shader that you created in the previous task.

1. Create a new material asset in your Unity project. For instructions see [Materials](https://docs.unity3d.com/6000.0/Documentation/Manual/class-Material).
2. Assign the shader asset to the material asset. For instructions, see [Materials](https://docs.unity3d.com/6000.0/Documentation/Manual/class-Material).
3. Create a cube in your scene. For instructions, see [Primitive objects](https://docs.unity3d.com/6000.0/Documentation/Manual/PrimitiveObjects).
4. Assign the material to it. For instructions, see [Materials](https://docs.unity3d.com/6000.0/Documentation/Manual/class-Material).

<a name = "creating-srp"></a>

### Creating the basic structure of your custom SRP

The final stage of preparation is to create the basic source files needed for your custom SRP, and tell Unity to begin rendering using the custom SRP.

1. Create a class that inherits from `RenderPipeline` and a compatible Render Pipeline Asset, following the instructions in [Creating a Render Pipeline Instance and Render Pipeline Asset](srp-creating-render-pipeline-asset-and-render-pipeline-instance.md)
2. Set the active Render Pipeline Asset, following the instructions in [How to get, set, and configure the active render pipeline](https://docs.unity3d.com/6000.0/Documentation/Manual/srp-setting-render-pipeline-asset.html). Unity will begin rendering using the custom SRP immediately, which means that your Scene view and Game view will be blank until you add code to your custom SRP.

## Creating the render loop

In a simple render loop, the basic operations are:

* [Clearing the render target](#clearing), which means removing the geometry that was drawn during the last frame.
* [Culling](#culling), which means filtering out geometry that is not visible to a Camera.
* [Drawing](#drawing), which means telling the GPU what geometry to draw, and how to draw it.

<a name = "clearing"></a>

### Clearing the render target

Clearing means removing the things that were drawn during the last frame. The render target is usually the screen; however, you can also render to textures to create a "picture in picture" effect. These examples demonstrate how to render to the screen, which is Unity's default behavior.

To clear the render target in the Scriptable Render Pipeline, you do the following:

1. Configure a `CommandBuffer` with a `Clear` command.
2. Add the `CommandBuffer` to the queue of commands on the `ScriptableRenderContext`; to do this, call [ScriptableRenderContext.ExecuteCommandBuffer](xref:UnityEngine.Rendering.ScriptableRenderContext.ExecuteCommandBuffer(UnityEngine.Rendering.CommandBuffer)).
3. Instruct the graphics API to perform the queue of commands on the `ScriptableRenderContext`; to do this, call  [ScriptableRenderContext.Submit](xref:UnityEngine.Rendering.ScriptableRenderContext.Submit).

As with all Scriptable Render Pipeline operations, you use the [RenderPipeline.Render](xref:UnityEngine.Rendering.RenderPipeline.Render(UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Camera[])) method as the entry point for this code. This example code demonstrates how to do this:

```lang-csharp
/* 
This is a simplified example of a custom Scriptable Render Pipeline.
It demonstrates how a basic render loop works.
It shows the clearest workflow, rather than the most efficient runtime performance.
*/

using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline {
    public ExampleRenderPipeline() {
    }

    protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
        // Create and schedule a command to clear the current render target
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // Instruct the graphics API to perform all scheduled commands
        context.Submit();
    }
}
```


<a name = "culling"></a>
### Culling

Culling is the process of filtering out geometry that is not visible to a Camera.

To cull in the Scriptable Render Pipeline, you do the following:

1. Populate a [ScriptableCullingParameters](xref:UnityEngine.Rendering.ScriptableCullingParameters) struct with data about a Camera; to do this, call [Camera.TryGetCullingParameters](xref:UnityEngine.Camera.TryGetCullingParameters(UnityEngine.Rendering.ScriptableCullingParameters&)).
2. Optional: Manually update the values of the `ScriptableCullingParameters` struct.
3. Call [ScriptableRenderContext.Cull](xref:UnityEngine.Rendering.ScriptableRenderContext.Cull(UnityEngine.Rendering.ScriptableCullingParameters&)), and store the results in a `CullingResults` struct.

This example code extends the example above, and demonstrates how to clear the render target and then perform a culling operation:

```lang-csharp
/* 
This is a simplified example of a custom Scriptable Render Pipeline.
It demonstrates how a basic render loop works.
It shows the clearest workflow, rather than the most efficient runtime performance.
*/

using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline {
    public ExampleRenderPipeline() {
    }

    protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
        // Create and schedule a command to clear the current render target
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // Iterate over all Cameras
        foreach (Camera camera in cameras)
        {
            // Get the culling parameters from the current Camera
            camera.TryGetCullingParameters(out var cullingParameters);

            // Use the culling parameters to perform a cull operation, and store the results
            var cullingResults = context.Cull(ref cullingParameters);
        }

        // Instruct the graphics API to perform all scheduled commands
        context.Submit();
    }
}
```

<a name="drawing"></a>

### Drawing

Drawing is the process of instructing the graphics API to draw a given set of geometry with given settings.

To draw in SRP, you do the following:

1. Perform a culling operation, as described above, and store the results in a `CullingResults` struct.
2. Create and configure [FilteringSettings](xref:UnityEngine.Rendering.FilteringSettings) struct, which describes how to filter the culling results.
3. Create and configure a [DrawingSettings](xref:UnityEngine.Rendering.DrawingSettings) struct, which describes which geometry to draw and how to draw it. 
4. *Optional*: By default, Unity sets the render state based on the Shader object. If you want to override the render state for some or all of the geometry that you are about to draw, you can use a [RenderStateBlock](xref:UnityEngine.Rendering.RenderStateBlock) struct to do this.
5. Call [ScriptableRenderContext.DrawRenderers](xref:UnityEngine.Rendering.ScriptableRenderContext.DrawRenderers(UnityEngine.Rendering.CullingResults,UnityEngine.Rendering.DrawingSettings&,UnityEngine.Rendering.FilteringSettings&)), and pass the structs that you created as parameters. Unity draws the filtered set of geometry, according to the settings.

This example code builds on the examples above, and demonstrates how to clear the render target, perform a culling operation, and draw the resulting geometry:

```lang-csharp
/* 
This is a simplified example of a custom Scriptable Render Pipeline.
It demonstrates how a basic render loop works.
It shows the clearest workflow, rather than the most efficient runtime performance.
*/

using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline {
    public ExampleRenderPipeline() {
    }

    protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
        // Create and schedule a command to clear the current render target
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // Iterate over all Cameras
        foreach (Camera camera in cameras)
        {
            // Get the culling parameters from the current Camera
            camera.TryGetCullingParameters(out var cullingParameters);

            // Use the culling parameters to perform a cull operation, and store the results
            var cullingResults = context.Cull(ref cullingParameters);

            // Update the value of built-in shader variables, based on the current Camera
            context.SetupCameraProperties(camera);

            // Tell Unity which geometry to draw, based on its LightMode Pass tag value
            ShaderTagId shaderTagId = new ShaderTagId("ExampleLightModeTag");

            // Tell Unity how to sort the geometry, based on the current Camera
            var sortingSettings = new SortingSettings(camera);

            // Create a DrawingSettings struct that describes which geometry to draw and how to draw it
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);

            // Tell Unity how to filter the culling results, to further specify which geometry to draw
            // Use FilteringSettings.defaultValue to specify no filtering
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        
            // Schedule a command to draw the geometry, based on the settings you have defined
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            // Schedule a command to draw the Skybox if required
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                context.DrawSkybox(camera);
            }

            // Instruct the graphics API to perform all scheduled commands
            context.Submit();
        }
    }
}
```
