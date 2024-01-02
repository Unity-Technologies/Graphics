# Motion vectors in URP

URP calculates the frame-to-frame screen-space movement of surface fragments using a [motion vector render pass](#motion-vectors-pass). URP stores the movement data in a full-screen texture and the stored per-pixel values are called [motion vectors](#definition).

Unity runs the motion vector render pass only when there are active features in the frame with render passes that request it. For example, the following URP features request the motion vector pass: [temporal anti-aliasing (TAA)](../anti-aliasing.md#temporal-anti-aliasing-taa) and [motion blur](../Post-Processing-Motion-Blur.md). For information on how to request the motion vector pass in a custom passes, refer to section [Using the motion vector texture in your passes](#motion-vector-texture-in-passes).

Incorrect or missing motion vectors can result in [visual artifacts](#motion-vectors-accuracy) in effects that rely on them. Follow the instructions on this page to ensure that your object renderers, Materials, and shaders are set up correctly to support motion vectors.

URP supports motion vectors only for opaque materials, including alpha-clipped materials. URP does not support motion vectors for transparent materials.

## Implementation details

This section describes how URP implements motion vectors.

### <a name="definition"></a>Motion vector definition

A motion vector is a 2D vector representing a surface fragment's motion relative to the camera since the last frame, projected onto the camera's near clipping plane. The motion vector texture uses 2 channels (R and G). Each texel stores the UV offset of each visible surface fragment. If you subtract the motion vector for a given texel from its current UV coordinate, you will get the UV coordinate of where this texel would have been on the screen last frame. The computed last frame UV coordinate can be outside the screen bounds.

### Object motion vectors and camera-only motion vectors

There are two categories of motion vectors:

* **Camera motion vectors**: motion vectors caused only by the camera's own motion.

* **Object motion vectors**: motion vectors caused by a combination of the camera's motion and the world-space motion of the object the fragment belongs to.

Given only the motion vector texture it's impossible to infer whether a fragment's motion on screen is due to only camera motion, object motion or a combination of both.

A single full-screen pass is enough to calculate camera motion vectors. Such pass has a fixed per-frame computation load independent of scene complexity. It's only necessary to know the current 3D positions of all pixels on screen and how the camera moved, which can be inferred from the depth buffer and the camera matrices for the current and the previous frames.

Object motion vectors have computation load which depends on the number and complexity of the moving objects in the scene because a draw per-object is required to account for each object's motion. Each draw needs to apply the camera's motion contribution too.

> **NOTE**: if a camera is locked to an object that moves, for example, a model of a car in a racing game, select the **Per Object Motion** option in the **Motion Vectors** property of that object. If you don't select that option, the object will have incorrectly large motion vectors. This happens because Unity calculates camera motion vectors assuming that the geometry of the object is static in the world, and that the camera is moving relative to it. This might cause significant TAA or motion blur artifacts.

### Motion vectors accuracy

When the motion vector texture is used by full-screen post-processing effects, such as TAA and motion blur, any screen regions with incorrect motion vectors (either missing, or inaccurate) will likely exhibit visual artifacts. The artifacts can include: texture blurring, movement ghosting, improper anti-aliasing, non-realistic or inappropriate motion blur, and so on.

If you are experiencing artifacts that you suspect are caused by incorrect motion vectors, check if the affected objects have object motion vector rendering enabled. In the **Frame Debugger**, the object should be present in the **MotionVectors** pass. To troubleshoot missing or incorrect motion vectors for a particular object, refer to the section [Cases when Unity renders motion vectors](#cases-when-motion-vectors).

### <a name="cases-when-motion-vectors"></a>Cases when Unity renders per-object motion vectors

Unity renders object motion vectors for a mesh in a frame when the following three conditions are met:

1. The shader associated with the mesh has a [MotionVectors pass](#motion-vectors-in-shaderlab) in an active SubShader block.

2. The mesh is being rendered through any of the following renderers:
    
    1. **SkinnedMeshRenderer**.
    
    2. **MeshRenderer**, when its **Motion Vectors** property is not set to `Camera Motion Only`.
    
    3. Using the following APIs: [Graphics.RenderMesh](https://docs.unity3d.com/ScriptReference/Graphics.RenderMesh.html), [Graphics.RenderMeshInstanced](https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshInstanced.html) or [Graphics.RenderMeshIndirect](https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshIndirect.html), with the `MotionVectorMode` member of the `RenderParams` struct not set to `Camera`.

3. If any of the following conditions is true:
    
    1. The `MotionVectorGenerationMode` property is set to `ForceNoMotion` on the `MeshRenderer` or the `RenderParams.MotionVectorMode` struct member of the `Grphics.Render...` APIs.
    
        **Note**: the `ForceNoMotion` option requires a per-object motion vector pass to be rendered every frame so that the camera motion vectors for such objects can be overwritten with zeros.
    
    2. The **MotionVectors** pass [is enabled on the material](https://docs.unity3d.com/ScriptReference/Material.SetPass.html) (for example, when a material has a vertex animation in Shader Graph or alembic animation).

    3. The **MotionVectors** pass [is disabled on the material](https://docs.unity3d.com/ScriptReference/Material.SetPass.html) but the model matrices for the previous and the current frame don't match, or if the object has skeletal animation. Stationary renderers without skeletal animation are not rendered with an object motion vector pass if their shader has a `MotionVectors` pass but it's disabled on their material.

**MotionVectors pass in URP pre-built shaders**

When the **MotionVectors** pass is enabled for the pre-built shaders, Unity renders object motion vectors for mesh renderers even if they don't move.

Unity enables the **MotionVectors** pass in the pre-built URP shaders when the following conditions are true:

* URP ShaderLab shaders: on a Material, the **Alembic Motion Vectors** property is enabled in the **Advanced Options** section.

* URP Lit and Unlit Shader Graph shaders: in the **Graph Inspector**, any of the following properties is set:

    * **Alembic Motion Vectors** is enabled.
    * **Additional Motion Vectors** property is set to **TimeBased** or **Custom**.

## GameObject MeshRenderer setting

To specify how a GameObject contributes to the motion vector buffer, use the **Motion Vectors** property: **Mesh Renderer** >> **Additional Settings** >> **Motion Vectors**. This property lets you disable motion vector rendering for a specific object, or fill the motion vector texture with zeros for the visible fragments of the object's renderer.

The following table describes the available **Motion Vectors** property options.

| **Motion Vectors** option | Description |
|---|---|
| Camera&#160;Motion&#160;Only | Unity treats the object as stationary in the world when rendering camera motion vectors. Unity does not draw a per-object motion vector pass for this MeshRenderer. If motion vector rendering is a GPU bottleneck, you can use this option as an optimization for objects which move slowly. |
| Per Object Motion | Unity renders the per-object **Motion Vectors** pass for this object. |
| Force No Motion | Unity renders the per-object **Motion Vectors** pass every frame for this object, but sets a special shader uniform variable to tell the pass to skip calculations and to write zero values. A per-object pass is still necessary to overwrite any non-zero camera motion vectors from the full-screen pass.<br/>You can use this option to avoid artefacts from the camera motion blur on a 3D HUD, or a third person character, or a race car, or if you have some other artefacts related to incorrect motion vectors on an object that you would like to avoid. |

## <a name="motion-vectors-pass"></a>Motion vectors render pass

This section describes the **MotionVectors** render pass. This render pass renders the motion vector texture.

### Location in the frame loop

URP renders motion vectors at the `BeforeRenderingPostProcessing` event. Before that event the motion vector texture might not be set or might contain previous frame's motion vector data.

### MotionVectors pass structure

URP renders the motion vector texture in 2 steps:

1. URP renders the camera motion vectors in the **MotionVectors** full-screen pass. This pass uses the depth texture and the camera matrices for the current and the previous frames to calculate the camera motion vectors. This pass has a fixed per-camera computation load and does not require special motion vector support from renderers or materials.

2. URP draws a per-object motion vector shader pass for [each renderer and material combination that supports motion vectors](#cases-when-motion-vectors).

### <a name="motion-vector-texture-in-passes"></a>Using the motion vector texture in your passes

Any `ScriptableRenderPass` implementation can request the motion vector texture as input. To do that, add the `ScriptableRenderPassInput.Motion` flag in the [ScriptableRenderPass.ConfigureInput](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@16.0/api/UnityEngine.Rendering.Universal.ScriptableRenderPass.html#UnityEngine_Rendering_Universal_ScriptableRenderPass_ConfigureInput_UnityEngine_Rendering_Universal_ScriptableRenderPassInput_) method in the [AddRenderPasses](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@16.0/api/UnityEngine.Rendering.Universal.ScriptableRendererFeature.html#UnityEngine_Rendering_Universal_ScriptableRendererFeature_AddRenderPasses_UnityEngine_Rendering_Universal_ScriptableRenderer_UnityEngine_Rendering_Universal_RenderingData__) callback of your custom Renderer Feature. If no other effect in the frame is using motion vectors, setting this input flag forces the URP renderer to inject the motion vector render pass into the frame.

To sample the motion vector texture in your shader pass, declare the shader resource for it in the `HLSLPROGRAM` section:

```c++
    TEXTURE2D_X(_MotionVectorTexture);
    SAMPLER(sampler_MotionVectorTexture);
```

To perform the sampling, use the following macro:

```c++
    SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_MotionVectorTexture, uv);
```

The `_X` postfix ensures that the texture is correctly declared and sampled when targeting XR platforms.

## URP shader support

This section describes which types of motion different shader types support for calculating motion vectors.

URP supports motion vectors only for opaque materials, including alpha-clipped materials. URP does not support motion vectors for transparent materials.

### <a name="urp-shaderlab-shaders"></a>URP ShaderLab shaders

URP `Lit`, `Unlit`, `Simple Lit`, `Complex Lit`, and `Baked Lit` shaders support per-object motion vectors for the following motion types:

* Rigid transform motion.
* Skeletal animation.
* Blend shape animation.
* Alembic animation.

To enable alembic motion vectors for particular material, enable the **Alembic Motion Vectors** checkbox in the **Advanced** section of the material inspector.

> **NOTE**: use materials with the **Alembic Motion Vectors** checkbox enabled only on alembic vertex animation caches rendered with a **PlayableDirectors** component. When using such materials with regular draw calls and MeshRenderers, the materials cannot read the correct motion vector attribute stream, which results in incorrect motion vectors.

### <a name="motion-vectors-in-shaderlab"></a>Motion vectors for custom ShaderLab shaders

For URP to render the **MotionVectors** pass for a ShaderLab shader, make sure that its active SubShader contains a pass with the following [LightMode tag](../urp-shaders/urp-shaderlab-pass-tags.md#lightmode):

```c++
Tags { "LightMode" = "MotionVectors" }
```

For example:

```c++
Shader “Example/MyCustomShaderWithPerObjectMotionVectors"
{
    SubShader
    {
        // ...other passes, SubShader tags and commands
    
        Pass
        {
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG
            
            HLSLPROGRAM
            
            // Your shader code goes here.
            
            ENDHLSL
        }
    }
}
```

For an example of adding motion vector pass support for features such as alpha clipping, LOD cross-fade, or alembic animation, refer to the implementation of the `MotionVectors` pass in URP pre-built ShaderLab shaders, for example, the `Unlit.shader` file. The rendering of your `MotionVectors` pass should match your non-motion-vector passes and should reflect any custom deformation and/or vertex animation a pass is performing.

If a custom shader is only intended for objects with transform motion or skinned animation, without using alpha clipping, LOD cross-fade, alembic animation, custom deformation, or vertex animation, the motion vector fallback shader provided by URP might be enough. To add the pre-built fallback shader, add the following ShaderLab command to your SubShader blocks:

```c++
Shader “Example/MyCustomShaderWithPerObjectMotionVectorFallback"
{
    SubShader
    {
        // ...other passes, SubShader tags, and commands
    
        UsePass "Hidden/Universal Render Pipeline/ObjectMotionVectorFallback/MOTIONVECTORS"
    }
}
```

> **NOTE:** in Unity versions earlier than 2023.2, URP would automatically use the fallback pass for all SubShader blocks which don't have a pass tagged with the `MotionVectors` [LightMode tag](../urp-shaders/urp-shaderlab-pass-tags.md). Starting from Unity 2023.2, this fallback logic is disabled for the following reasons:
>
>    * The fallback logic was only an initial implementation detail meant for URP's own Materials.
>    * URP Materials now use material-type-specific motion vector passes to support features like alpha clip, LOD cross-fade, or alembic animation, making the fallback obsolete.
>    * The fallback logic was causing unintended visual artifacts for content which it was not applicable to (for example, decals which should not draw any object motion vectors).

### URP Lit and Unlit Shader Graph targets

The URP Shader Graph `Lit` and `Unlit` targets support all of the motion vector features described in the [URP ShaderLab shaders](#urp-shaderlab-shaders) section. In addition to that, they have the **Additional Motion Vectors** setting with the following options:

* **Time-Based**: motion vectors for a Shader Graph vertex animation are generated automatically by running the vertex position subgraph twice with the current and the previous frame values for the **Time** node. This mode only works correctly for vertex animations that are computed procedurally based on the **Time** node, and that use only user-defined parameters (constants, attributes, buffers, textures) that do not change between frames.

* **Custom**: selecting this option adds an extra **Motion Vector** vertex output. The output lets you specify a custom object motion vector: a 3D local object space offset for each vertex from where it was in the previous frame. If you know how to compute the position of a vertex in the previous frame, you can author custom motion vectors for your vertex animations. Unity applies the custom motion vectors additively to transform motion, skeletal animation, and alembic animation before projecting the motion vector to the final 2D screen space vector.

* **None**: the default value. Use this option when the shader either does not modify the vertex output or if it does not animate the vertex deformation.

The options **Time-Based** and **Custom** enable the **MotionVectors** pass on the materials. The object motion vector pass is rendered every frame for them, even when the object's transform is stationary between frames, unless the **MotionVectorGenerationMode** property is set to `Camera`.
