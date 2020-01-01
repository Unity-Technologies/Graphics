# Custom render passes

The High Definition Render Pipeline (HDRP) allows you to define custom render passes that inject Shaders and C# at certain points inside the render loop. This means that you can draw objects, process full screen passes, or read camera buffers such as the depth, color, or normal buffers.

Below is an example of what you can achieve with custom render passes:
![](Images/TIPS_Effect_Size.gif)

## Custom render pass volume

HDRP implements custom render passes through a volume system, but note that this is different to HDRP's standard [Volume system](Volumes.html). The implementation uses the **Custom Pass Volume** component to define the custom pass and properties relating to it. The normal Volume system and custom render pass volume system have the following differences:

- You can not blend between custom render pass volumes like you can with standard Volumes. You can only fade the effect in and out.
- If multiple volumes with the same injection point overlap, HDRP executes the smallest (in term of bounding volume) and ignores all others.
- HDRP saves data for a custom render pass on the volume GameObject itself. HDRP saves data for a standard Volume as an Asset in your Unity Project (see [Volume Profile](Volume-Profile.html).

Like in standard Volumes, there are two modes for custom render pass volumes:

* **Local**: Local custom render passes define bounds. When a Camera enters these bounds, HDRP processes the custom render pass for that Camera. To define the bounds, the Custom Pass Volume component uses a Collider attached to the same GameObject.

* **Global**: HDRP processes Global custom render passes for Cameras everywhere in your Scene.

The Custom Pass Volume component also includes the **Fade** feature that allow you to smooth the transition between normal rendering and the custom render pass. To control the distance of the fade, use the **Fade Radius** property on the Custom Pass Volume component. The Custom Pass Volume defines the radius in meters and does not scale it regardless of the GameObject's Transform.

To give you full control over custom render passes, you must implement fading manually in your effects. To do this, use `_FadeValue` in the Shader and `CustomPass.fadeValue` in the C# script. These values are both between **0** and **1** and represent how far the Camera is from the Collider bounding volume. For more information about fading in scripts, see [Scripting API](#ScriptingAPI).

Below you can see an example of a custom render pass volume that uses a Box Collider. In the Scene view, Unity represents the bounds of this example volume as a semi-transparent green box and the fade radius as the surrounding wireframe box.
![](Images/CustomPassVolumeBox_Collider.png)

**Note**: You can stack multiple Custom Pass Volumes, but HDRP can only execute one per injection point. The one that HDRP executes is the one with the smallest currently overlapping bounding area (not including the fade radius). This means that HDRP defines global volumes as lower priority than local volumes.

## Injection Points

You can inject custom render passes at six different places. HDRP does not tell you exactly where it executes the custom pass, but it does guarantee that a certain list of buffers are available in each injection point to **Read** from or **Write** to. Each buffer contains a certain subset of objects that HDRP has rendered before your custom render pass. Unlike with the Universal Render Pipeline, HDRP does not guarantee that it executes your pass before or after any HDRP internal operation (due to its async rendering). However, HDRP does guarantee that it executes the injection points from top to bottom in the following order in a frame:

Name | Available Buffers | Description
--- | --- | ---
**BeforeRendering** | **Depth** (Write) | Executes the render pass straight after HDRP clears the depth buffer. This allows you to write to the depth buffer so HDRP does not render depth-tested opaque GameObjects. This is useful if you want to mask a part of the screen. Here you can also clear the buffer you allocated or the `Custom Buffer`. 
**AfterOpaqueDepthAndNormal** | **Depth** (Read \| Write), **Normal + roughness** (Read \| Write) | Buffers for this injection point contain every opaque GameObject. Here you can modify the normal, roughness, and depth buffer. HDRP takes this into account in the lighting and the depth pyramid. **Note**: normals and roughness are in the same buffer. To read and write normal and roughness data, use `DecodeFromNormalBuffer` and `EncodeIntoNormalBuffer`. 
**BeforePreRefraction** | **Color** (no pyramid \| Read \| Write), **Depth** (Read \| Write), **Normal + roughness** (Read) | Buffers for this injection point contain all opaque GameObjects and the sky. Use this point to render any transparent GameObjects that you want to be in the refraction. They end up in the color pyramid that HDRP uses for refraction when it draws transparent GameObjects. 
**BeforeTransparent** | **Color** (Pyramid \| Read \| Write), **Depth** (Read \| Write), **Normal + roughness** (Read) | Here you can sample the color pyramid that HDRP uses for transparent refraction. This is useful for some blur effects, but note that every GameObject that you render at this point will not be in the color pyramid. Here, you can also draw transparent GameObjects that need to refract the Scene. For example, water. 
**BeforePostProcess** | **Color** (Pyramid \| Read \| Write), **Depth** (Read \| Write), **Normal + roughness** (Read) | Buffers for this injection point contain every GameObject in the frame in HDR. 
**AfterPostProcess** | **Color** (Read \| Write), **Depth** (Read) | Buffers for this injection point are in after post-process mode. The depth buffer here is jittered which means that you can not draw depth-tested GameObjects without having artifacts. 

The diagram below shows where HDRP injects the custom passes within a frame.
![](Images/HDRP-frame-graph-diagram.png)

## Custom Pass List

The main part of the **Custom Pass Volume** component is the **Custom Passes** reorderable list. This allows you to add new custom pass effects and configure them. There are two custom render passes that are built-in to HDRP: 

* **FullScreen** 

* **DrawRenderers**

Below, you can see an example of a **FullScreen** render pass:

![](Images/FullScreenCustomPass_Inspector.png)

Every custom render pass contains the following set of base properties:

<a name="CustomRenderPassBaseProperties"></a>

Property | Description
--- | ---
**Name** | The name of the pass. HDRP uses this as the name of the profiling marker for debugging. 
**Target Color Buffer** | The target buffer where HDRP writes the color to. 
**Target Depth Buffer** | The target buffer where HDRP writes and tests depth and stencil. 
**Clear Flags** | Defines how HDRP clears the the **Target Color Buffer** and **Target Depth Buffer**. 

**Note:** by default, HDRP sets the target buffers to the Camera buffers, but you can also select the custom buffer. This is a buffer that HDRP allocates where you can put anything you want. You can then sample it in your custom render pass Shaders. You can choose the format of the custom buffer in the HDRP Asset. This is in the **Rendering** section.

![](Images/CustomPass_BufferFormat_Asset.png)

If you do not want to use custom render passes in your Project, you can disable them in the HDRP Asset. If you do this, HDRP does not allocate the custom buffer. Additionally, in a Camera's [Frame Settings](Frame-Settings.html), you can choose whether or not to render custom render passes. If you disable custom render passes in Frame Settings, this does not affect custom buffer allocation.

### FullScreen custom render pass

The FullScreen render pass allows you to process full screen effects for Cameras in your Scene.

#### Using the FullScreen custom render pass

When you create a **FullScreen** custom render pass, HDRP renders it with the **FullScreen Material** that you specify in the Custom Pass Volume component. To create and assign a Material compatible with the FullScreen pass:

1. Go to **Create > Shader > HDRP**, right click on **Custom FullScreen Pass** and create a new Material.
2. Now set the new Material to the **FullScreen Material** property in the Inspector.
3. When you assign a Material, the Custom Pass Volume exposes the **Pass Name** property. Use this drop-down to select which pass of the Shader HDRP uses to draw the full screen effect. This is useful if you want to do multiple variants of one effect and switch between them.
4. Now that the custom render pass is set up, you can edit the Shader of the full screen Material. In the Shader template, add your code in the **FullScreenPass** function. For an example on how to do this, see [FullScreen example](#FullScreenExample).

**Warning**, when you write full screen Shaders, be aware of the following:

* **You cannot read and write to the same render target**. For example, you cannot sample the camera color, modify it, and then write the result back to the camera color buffer. To do this, you have to use two render passes with a secondary buffer.
* **Depending on which injection point and what settings you use, the contents of the depth buffer changes**. When you use [temporal anti-aliasing](Anti-Aliasing.html#TAA), HDRP jitters the depth buffer. This means that, if you use the **AfterPostProcess** injection point, GameObjects that need depth cause wobbling. Also, the depth buffer never contains transparent GameObjects that write depth.
* **You can only sample the camera color with lods in the BeforePostProcess and AfterPostProcess injection point**. If you call `CustomPassSampleCameraColor` during an injection point before either of these, it returns black.
* **Chaining a DrawRenderers render pass with a FullScreen render pass**. In multi-pass setups where you draw GameObjects in the camera color buffer and then read the camer buffer during a FullScreen custom render pass, you can not see GameObjects you drew in passes before your FullScreen render pass. This is unless you use the **BeforeTransparent** injection point.
* **Multi-sample anti-aliasing (MSAA)**: When you use [MSAA](Anti-Aliasing.html#MSAA), make sure to enable **Fetch Color Buffer**. This is because this boolean determines whether or not can can use the color buffer in the custom render pass or not.

##### Properties

The FullScreen custom render pass contains the following properties alongside the custom render pass [base properties](#CustomRenderPassBaseProperties):

![](Images/FullScreenCustomPass_Inspector.png)

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Fetch Color Buffer**  | Enable this checkbox if your custom render pass samples/fetches the Camera's color buffer. |
| **FullScreen Material** | The Material that HDRP uses to render the full screen effect. |
| - **Pass Name**         | Specifies which pass of the Shader that HDRP uses to draw the full screen effect. |

<a name="FullScreenExample"></a>

#### FullScreen example

This example uses input data like depth, view direction, and position in world space. This example also implements fading. You can see the example effect uses **_FadeValue** which changes depending on the **Fade Radius** on the Custom Pass Volume component.

```HLSL
float4 FullScreenPass(Varyings varyings) : SV_Target
{
    float depth = LoadCameraDepth(varyings.positionCS.xy);
    PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
    float4 color = float4(0.0, 0.0, 0.0, 0.0);

    // Load the camera color buffer at the mip 0 if we're not at the before rendering injection point
    if (_CustomPassInjectionPoint != CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING)
        color = float4(CustomPassSampleCameraColor(posInput.positionNDC.xy, 0), 1);

    // Add your custom pass code here

    // Fade value allow you to increase the strength of the effect while the camera gets closer to the custom pass volume
    float f = 1 - abs(_FadeValue * 2 - 1);
    return float4(color.rgb + f, color.a);
}
```

### DrawRenderers custom render pass

The DrawRenderers render pass allows you to draw a subset of GameObjects in the Camera's view.

#### Using the DrawRenderers custom render pass

To define the subset of GameObjects that a custom render pass draws, use the **Queue** and **Layer Mask** properties under the **Filters** fold-out.

By default, the render pass draws each GameObject with the GameObject's own Material. You can also override the Material that the render pass uses to draw each GameObject. To define how the render pass draws the subset of GameObjects, use the **Material**, **Pass Name**, and **Sorting** properties under the **Overrides** fold-out. For the **Material**, you can use multiple types of Shader:

* [Unlit Shader](Unlit-Shader.html).
* [Unlit Master Node](Master-Node-Unlit.html).
* Custom Unlit Shaders. To create an Unlit Shader compatible with custom render passes, select **Create > Shader > HDRP > Custom Renderers Pass**.

**Note**: Not every injection point supports Lit Shaders because lighting data is not ready at every injection point.

##### Properties

The DrawRenderers custom render pass contains the following properties alongside the custom render pass [base properties](#CustomRenderPassBaseProperties):

![](Images/CustomPassDrawRenderers_Inspector.png)

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Filters**       |                                                              |
| - **Queue**       | Specifies what type of Materials to render (transparent, opaque, everything). |
| - **Layer Mask**  | Specifies which pass of the Shader HDRP uses                 |
| **Overrides**     |                                                              |
| - **Material**    | The Material that HDRP uses to render the GameObjects in this render pass. |
| - - **Pass Name** | Specifies which pass of the Shader that HDRP uses. The default pass is the **SceneSelectionPass** and the pass that HDRP uses to render GameObjects is **ForwardOnly**. If you want to only render the GameObject's depth, use the **DepthForwardOnly**. |
| - **Sorting**     |                                                              |



#### Advanced effects

To create advanced effects, you can use the **Custom Renderers Pass** shader that will create an unlit one pass HDRP shader and inside the `GetSurfaceAndBuiltinData` function you will be able ot put your fragment shader code:

```HLSL
// Put the code to render the objects in your custom pass in this function
void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    float2 colorMapUv = TRANSFORM_TEX(fragInputs.texCoord0.xy, _ColorMap);
    float4 result = SAMPLE_TEXTURE2D(_ColorMap, s_trilinear_clamp_sampler, colorMapUv) * _Color;
    float opacity = result.a;
    float3 color = result.rgb;

#ifdef _ALPHATEST_ON
    DoAlphaTest(opacity, _AlphaCutoff);
#endif

    // Write back the data to the output structures
    ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
    builtinData.opacity = opacity;
    builtinData.emissiveColor = float3(0, 0, 0);
    surfaceData.color = color;
}
```

If you need to modify the vertex shader, you can uncomment the ApplyVertexModification() code block just above the `` function:

```HLSL
#define HAVE_MESH_MODIFICATION
AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
{
    input.positionOS += input.normalOS * 0.0001; // inflate a bit the mesh to avoid z-fight
    return input;
}
```

For reference, here is the definition of the AttributesMesh struct:
```HLSL
struct AttributesMesh
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT; // Store sign in w
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
    float2 uv3          : TEXCOORD3;
    float4 color        : COLOR;
};
```

> **Note that all the transformation in this function are made in object space**

A very important thing to be aware of is the `ATTRIBUTES_NEED` and `VARYINGS_NEED` system, there are a list of defines that controls which data is going to be sent to the vertex and fragment shader. `ATTRIBUTES_NEED` are for the vertex data and `VARYINGS_NEED` are for the fragment, so for example if you want to sample uvs in the fragment shader, you need to define both `ATTRIBUTES_NEED_TEXCOORD0` and `VARYINGS_NEED_TEXCOORD0`. Note that by default you have access to UV 0 and normals.  
Here is the list of all the defines you can enable
```HLSL
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_TEXCOORD3
#define ATTRIBUTES_NEED_COLOR

#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define VARYINGS_NEED_TEXCOORD3
#define VARYINGS_NEED_COLOR
#define VARYINGS_NEED_CULLFACE
```

Note that you can also override the depth state of the objects in your pass. This is especially useful when you're rendering objects that are not in the camera culling mask (they are only rendered in the custom pass). Because in these objects, opaque ones will be rendered in `Depth Equal` test which only works if they already are in the depth buffer. In this case you may want to override the depth test to `Less Equal`.

<a name="ScriptingAPI"></a>

## Scripting API

To do even more complex effect, that may require more than one buffer or even `Compute Shaders`, you have a Scripting API available to extend the `CustomPass` class.  
Every non abstract class that inherit from `CustomPass` will be listed when you click on the `+` button of the custom passes list.

![](Images/CustomPass_Add_Inspector.png)

### C# Template

To Create a new C# Custom pass go to **Create/Rendering/C# Custom Pass**, it will generate a C# file containing a class like this one:

```CSharp
class #SCRIPTNAME# : CustomPass
{
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {}

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult) {}

    protected override void Cleanup() {}
}
```

To code your custom pass, you have three entry point:

- `Setup` to allocate all the resources you'll need to render your pass (RTHandle / render textures, Material, Compute Buffers, etc.).
- `Cleanup` let you release every resources you have allocated in the `Setup` function. Be careful to not forget one resource or it will leak (especially RTHandle which can be pretty heavy in term of memory).
- `Execute` is where you'll write everything you need to be rendered during the pass.

In the `Setup` and `Execute` functions, we gives you access to the [ScriptableRenderContext](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/Rendering.ScriptableRenderContext.html) and a [CommandBuffer](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/Rendering.CommandBuffer.html), these two classes contains everything you need to render pretty much everything but here we will focus on these two functions [ScriptableRenderContext.DrawRenderers](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/Rendering.ScriptableRenderContext.DrawRenderers.html) and [CommandBuffer.DrawProcedural](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/Rendering.CommandBuffer.DrawProcedural.html).

> **Important:** if the a shader is never referenced in any of your scenes it won't get built and the effect will not work when running the game outside of the editor. Either add it to a [Resources folder](https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html) or put it in the **Always Included Shaders** list in `Edit -> Project Settings -> Graphics`. Be careful with this especially if you load shaders using `Shader.Find()` otherwise, you'll end up with a black screen.


> **Pro Tips:**  
> - To allocate a render target buffer that works in every situation (VR, camera resize, ...), use the RTHandles system like so:
> ```CSharp
> RTHandle myBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, useDynamicScale: true, name: "My Buffer");
> ```
> - Don't forget release the buffer afterwards using `myBuffer.Release();`  
> - To create materials, you can use the `CoreUtils.CreateEngineMaterial` function and destroy it with `CoreUtils.Destroy`.  
> - When scripting your pass, the destination render target will be set to what is defined in the UI. It means that you don't need to set the render target if you only use one.
> - **MSAA**: when you enable MSAA and you want to render objects to the main camera color buffer and then in a second pass, sample this buffer, you'll need to resolve it first. To do so you have this function `CustomPass.ResolveMSAAColorBuffer` that will resolve the MSAA camera color buffer into the standard camera color buffer.

Now that you have allocated your resources you're ready to start doing stuff in the `Execute` function.

### Calling a FullScreen Pass in C\#

To do a FullScreen pass using a material, we uses `CoreUtils.DrawFullScreen` which under the hood call `DrawProcedural` on the Command Buffer in parameter. So when we do a FullScreen Pass the code looks like this:

```CSharp
SetCameraRenderTarget(cmd); // Bind the camera color buffer along with depth without clearing the buffers.
CoreUtils.DrawFullScreen(cmd, material, shaderPassId: 0);
```

Where `cmd` is the command buffer and shaderPassId is the equivalent of `Pass Name` in the UI but with indices instead. Note that in this example the `SetCameraRenderTarget` is optional because the render target bound when the `Execute` function is called is the one set in the UI with the `Target Color Buffer` and `Target Depth Buffer` fields.

### Calling DrawRenderers inC\#

Calling the DrawRenderers function on the ScriptableRenderContext require a lot of boilerplate code and to simplify this, HDRP provides a simpler interface:

```CSharp
var result = new RendererListDesc(shaderTags, cullingResult, hdCamera.camera)
{
    rendererConfiguration = PerObjectData.None,
    renderQueueRange = RenderQueueRange.all,
    sortingCriteria = SortingCriteria.BackToFront,
    excludeObjectMotionVectors = false,
    layerMask = layer,
};

HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
```

One of the tricky thing here is the `shaderTags`, it is a filter for which object is going to be rendered (a bit like the layer filter) but it is based on the name of the pass in the shaders that are currently rendered. It means that if a material in the view have a shader that contains one of these tags in these passes names, then it will pass the test, otherwise it will not be rendered.

For the `renderQueueRange`, you can use the `GetRenderQueueRange` function in the `CustomPass` class that converts `CustomPass.RenderQueueType` (which is what you have in the ui to configure the render queue) into a `RenderQueueRange` that you can use in `RendererListDesc`.

> **⚠️ WARNING: Be careful with the override material pass index:** when you call the DrawRenderers with an [override material](https://docs.unity3d.com/ScriptReference/Rendering.DrawingSettings-overrideMaterial.html), then you need to select which pass you're going to render using the override material pass index. But in build, this index can be changed after that the shader stripper removed passes from shader (like every HDRP shaders) and that will shift the pass indices in the shader and so your index will become invalid. To prevent this issue, we recommend to store the name of the pass and then use `Material.FindPass` when issuing the draw.

### Scripting the volume component

You can retrieve the `CustomPassVolume` in script using [GetComponent](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/GameObject.GetComponent.html) and access most of the things available from the UI like `isGlobal`, `fadeRadius` and `injectionPoint`.

You can also dynamically change the list of Custom Passes executed by modifying the `customPasses` list.

### Other API functions

Sometimes you want to render objects only in a custom pass and not in the camera. To achieve this, you disable the layer of your objects in the camera culling mask, but it also means that the cullingResult you receive in the `Execute` function won't contain this object (because by default this cullingResult is the camera cullingResult). To overcome this issue, you can override this function in the CustomPass class:

```CSharp
protected virtual void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera camera) {}
```

it will allow you to add more layers / custom culling option to the cullingResult you receive in the `Execute` function.

> **⚠️ WARNING: Opaque objects may not be visible** if they are rendered only during the custom pass, because we assume that they already are in the depth pre-pass, we set the `Depth Test` to `Depth Equal`. Because of this you may need to override the `Depth Test` to `Less Equal` using the `depthState` property of the [RenderStateBlock](https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock.html).

## Example: Glitch Effect (without code)

To apply a glitch effect on top of objects, we can use ShaderGraph with custom passes:

First, we create an Unlit HDRP ShaderGraph and add some nodes to sample the scene color with a random x offset, to create the distortion like effect. We also have an external parameter to control the strength of the effect called `offset`.

> **Do not forget to put the ShaderGraph in transparent mode otherwise the HD Scene Color Node will return black**

![](Images/CustomPass_Glitch_ShaderGraph.png)

When we're done with this shader, we can create a Material from it by selecting the shader in the project view, **right click** and **Create/Material**.

Then, we can create a new GameObject and add a Custom pass volume component. Select `Before Post Process` injection point and add a new `DrawRenderers` custom pass configured like this:

![](Images/CustomPass_Glitch_Inspector.png)

Note that we use the `ForwardOnly` pass of our ShaderGraph because we want to render the object color.  
The `Selection` Layer here will be used to render the objects with a glitch.

Then you just have to put GameObjects in the `Selection` layer and voila !

![](Images/CustomPass_Glitch.gif)

## Scripting Example: Outline Pass

First, we create a Custom Pass C# Script called Outline:

```CSharp
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class Outline : CustomPass
{
    public LayerMask    outlineLayer = 0;
    [ColorUsage(false, true)]
    public Color        outlineColor = Color.black;
    public float        threshold = 1;

    // To make sure the shader will ends up in the build, we keep it's reference in the custom pass
    [SerializeField, HideInInspector]
    Shader                  outlineShader;

    Material                fullscreenOutline;
    MaterialPropertyBlock   outlineProperties;
    ShaderTagId[]           shaderTags;
    RTHandle                outlineBuffer;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        outlineShader = Shader.Find("Hidden/Outline");
        fullscreenOutline = CoreUtils.CreateEngineMaterial(outlineShader);
        outlineProperties = new MaterialPropertyBlock();

        // List all the materials that will be replaced in the frame
        shaderTags = new ShaderTagId[3]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
        };

        outlineBuffer = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
            useDynamicScale: true, name: "Outline Buffer"
        );
    }

    void DrawOutlineMeshes(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
    {
        var result = new RendererListDesc(shaderTags, cullingResult, hdCamera.camera)
        {
            // We need the lighting render configuration to support rendering lit objects
            rendererConfiguration = PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.BackToFront,
            excludeObjectMotionVectors = false,
            layerMask = outlineLayer,
        };

        CoreUtils.SetRenderTarget(cmd, outlineBuffer, ClearFlag.Color);
        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
    {
        DrawOutlineMeshes(renderContext, cmd, camera, cullingResult);

        SetCameraRenderTarget(cmd);

        outlineProperties.SetColor("_OutlineColor", outlineColor);
        outlineProperties.SetTexture("_OutlineBuffer", outlineBuffer);
        outlineProperties.SetFloat("_Threshold", threshold);
        CoreUtils.DrawFullScreen(cmd, fullscreenOutline, outlineProperties, shaderPassId: 0);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(fullscreenOutline);
        outlineBuffer.Release();
    }
}
```

In the setup function, we allocate a buffer to render the objects that are in the `outlineLayer` layer.

The Execute function is pretty straightforward, we render the objects and then do a fullscreen pass which will perform the outline. When we draw the objects, we don't use an override material here because we want the effect to be working with alpha clip and transparency.  
For the shader, it's the classic way of doing an outline: sample the color in the outline buffer, if it's below the threshold, then it means that we're maybe in an outline. To check if it's the case, we perform a neighbour search and if we find a pixel above the threshold, then we outline it.

To be more efficient, we use a transparent fullscreen pass with a blend mode that will replace pixels that needs to be outlined.

```HLSL
Shader "Hidden/Outline"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D_X(_OutlineBuffer);
    float4 _OutlineColor;
    float _Threshold;

    #define v2 1.41421
    #define c45 0.707107
    #define c225 0.9238795
    #define s225 0.3826834
    
    #define MAXSAMPLES 8
    // Neighbour pixel positions
    static float2 samplingPositions[MAXSAMPLES] =
    {
        float2( 1,  1),
        float2( 0,  1),
        float2(-1,  1),
        float2(-1,  0),
        float2(-1, -1),
        float2( 0, -1),
        float2( 1, -1),
        float2( 1, 0),
    };

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float4 color = float4(0.0, 0.0, 0.0, 0.0);
        float luminanceThreshold = max(0.000001, _Threshold * 0.01);

        // Load the camera color buffer at the mip 0 if we're not at the before rendering injection point
        if (_CustomPassInjectionPoint != CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING)
            color = float4(CustomPassSampleCameraColor(posInput.positionNDC.xy, 0), 1);

        // When sampling RTHandle texture, always use _RTHandleScale.xy to scale your UVs first.
        float2 uv = posInput.positionNDC.xy * _RTHandleScale.xy;
        float4 outline = SAMPLE_TEXTURE2D_X_LOD(_OutlineBuffer, s_linear_clamp_sampler, uv, 0);
        outline.a = 0;

        if (Luminance(outline.rgb) < luminanceThreshold)
        {
            float3 o = float3(_ScreenSize.zw, 0);

            for (int i = 0; i < MAXSAMPLES; i++)
            {
                float2 uvN = uv + _ScreenSize.zw * samplingPositions[i];
                float4 neighbour = SAMPLE_TEXTURE2D_X_LOD(_OutlineBuffer, s_linear_clamp_sampler, uvN, 0);

                if (Luminance(neighbour) > luminanceThreshold)
                {
                    outline.rgb = _OutlineColor.rgb;
                    outline.a = 1;
                    break;
                }
            }
        }

        return outline;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
```
