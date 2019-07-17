# High Definition Render Pipeline overview

This is a high level overview of all of the features you can use in the High Definition Render Pipeline (HDRP). This document presents the features in the following categories:

- [Project](#Project)
- [Material](#Material)
- [Lighting](#Lighting)
- [Camera](#Camera)
- [Tools](#Tools)
- [Programming](#Programming)

<a name="Project"></a>

## Project

### Platform support

You can run HDRP Projects on XboxOne, PS4, and compute Shader compatible devices for DirectX 11, DirectX 12, Metal, and Vulkan.

### Camera-relative rendering

Use HDRP to render distance GameObjects with large world-space coordinates in a more robust and numerically stable way than the built-in render pipeline. For information on how HDRP achieves this, see the [Camera-relative rendering documentation](Camera-Relative-Rendering.html).

### Dynamic resolution

When you use dynamic resolution, HDRP renders Scenes at the maximum resolution for your target platforms, and rescales the viewport accordingly, so it can render at varying resolutions. This helps maintain a stable frame rate in your Unity Project. For more information on dynamic resolution, as well as the types that HDRP supports, see the [dynamic resolution documentation](Dynamic-Resolution.html).

### Volumes

Use Volumes to localize environmental Scene settings and post-processing effects. You can have multiple Volumes inside a Scene, and make effects like fog density, sky color, or exposure depend on the position of the Camera. For information on how to use Volumes in HDRP, see the [Volumes documentation](Volumes.html).

<a name="Material"></a>

## Materials

The HDRP Shaders allow you to use the following features:

- [Opaque/transparent surfaces](Surface-Type.html).

- - Different blend modes for transparent surfaces.
  - Transparent surfaces that work with fog.
  - Refraction and distortion for transparent surfaces.

- [Material types](Material-Type.html).

- - Anisotropy, for surfaces that have highlights that change when you view them from different angles, like brushed metal or velvet.
  - Iridescence, for surfaces that appear to change color as you view them from different angles, like soap bubbles or insect wings.
  - Metallic, for surfaces that appear smooth, reflective, and have specular highlights.
  - Specular Color, for surfaces that you want to have a colored specular highlight.
  - Subsurface Scattering, for translucent surfaces that simulate light interaction and scattering, like skin or plant leaves.
  - Translucent, for surfaces that simulate light interaction, but do not blur light that transmits through the Material.

- Pixel and vertex displacement, for surfaces that you want to displace based on a height map.

- Emission, for Materials that you want to act as a self-illuminating light source.

- [Decals](Decal-Shader.html), for surfaces that you want to project a Texture onto.

- Detail mapping, for surfaces that you want to add micro details to.

### Lit Shader

![](Images/HDRPFeatures-LitShader.png)

Use the Lit Shader to easily create realistic Materials and includes options for effects including subsurface scattering, iridescence, and translucency. This is the default Shader in HDRP. For more information, including the full list of Shader properties, see the [Lit Shader documentation](Lit-Shader.html).

### Layered Lit Shader

![](Images/HDRPFeatures-LayeredLitShader.png)

The Layered Lit Shader combines a main Material with other tileable Materials in order to produce visuals with a similar quality to a single high-resolution texture, but at a lower performance cost than a [Lit Shader](Lit-Shader.html) using the single high-resolution Texture. For more information, including a full list of Shader properties, see the [Layered Lit Shader documentation](Layered-Lit-Shader.html).

### Unlit Shader

![](Images/HDRPFeatures-UnlitShader.png)

The Unlit Shader allows you to create Materials that are not affected by lighting. Unlit Shaders are perfect for visual effects. For more information, including a full list of Shader properties, see the [Unlit Shader documentation](Unlit-Shader.html).

### StackLit Shader

![](Images/HDRPFeatures-StackLitShader.png)

The StackLit Shader improves on the Lit Shader in favor of quality over performance. This Shader coats surfaces more accurately than the Lit Shader, and, unlike the Lit Shader, allows you to use multiple Material features like anisotropy, subsurface scattering iridescence, and hazy parametrization at the same time.

### Hair Shader

![](Images/HDRPFeatures-HairShader.png)

The Hair Shader is purpose-built to accurately render realistic hair in your Unity Project. It uses an improved Kajiya Kay lighting model which features better energy conservation and provides you with more flexibility.

### Fabric Shader

![](Images/HDRPFeatures-FabricShader.png)

The Fabric Shader allows you to render realistic fabric Materials in HDRP. You can use the cotton wool or silk lighting model to create a wide variety of fabrics.

### AxF Shader

![](Images/HDRPFeatures-AxFShader.png)

The AxF Shader supports the [X-Rite AxF](<https://www.xrite.com/axf>) measured Material format. The AxF importer, which is part of the [Unity Industry Bundle](<https://unity.com/solutions/automotive-transportation/industry-bundle>), automatically populates an AxF Material when it imports AxF Assets.

### Decal Shader

![](Images/HDRPFeatures-DecalShader.png)

The Decal Shader allows you to place decals on surfaces in your Scene. To apply a decal to a surface, you can either use the [Decal Projector component](Decal-Projector.html) to project the decal onto the surface, or assign the decal directly to a Mesh and then place the Mesh on the surface. For more information, including the full list of Shader properties, see the [Decal Shader documentation](Decal-Shader.html).

### Terrain Shader

![](Images/HDRPFeatures-TerrainShader.png)

The Terrain Lit Shader is compatible with the built-in terrain system and supports up to eight layers in a single draw call. This Shader uses the same lighting model as the Lit Shader.

<a name="Lighting"></a>

## Lighting

### Light types

![](Images/HDRPFeatures-LightTypes.png)

The HDRP light types use [physical light units](#PLU) to help you light your Scene in the most realistic way possible. The HDRP light types are:

- **Directional**

- - Color temperature
  - Colored cookie
  - Shadow mask support

- **Spot**

- - Color temperature
  - Colored cookie
  - Shadow mask support
  - Cone, pyramid and box shapes

- **Point**

- - Color temperature
  - Colored cookie
  - Shadow mask support

- **Area**

- - Color temperature
  - Colored Cookie
  - Shadow mask support

- **Line**

- - Color temperature
  - No shadow masks

For more information, including the full list of light properties, see the [Light component documentation](Light-Component.html).

### Reflection probes

![](Images/HDRPFeatures-ReflectionProbes.png)

HDRP supports cubemap and planar GPU Reflection Probes to help you produce realistic reflections in your Scene in real time. For more information, see the [introduction to Reflection Probes](Reflection-Probes-Intro.html)

### Emission

![](Images/HDRPFeatures-Emission.png)

HDRP Materials support light [emission](Lit-Shader.html#EmissionInputs), which allows you to make a Material into a self-illuminated visible source of light. Global illumination takes emission into account and you can use [physical light units](#PLU) to set the intensity based on real-world lights. 

### Shadows

![](Images/HDRPFeatures-Shadows.png)

HDRP uses multiple methods to calculate shadows:

- [Shadow cascades](Override-Shadows.html): HDRP uses cascade splits to control the quality of shadows cast by Directional Lights based on the distance from the Camera.
- [Contact Shadows](Override-Contact-Shadows.html): HDRP [raymarches](Glossary.html#RayMarching) inside the depth buffer to process these shadows. That way, HDRP captures small details that regular shadow algorithms fail to capture.
- [Micro Shadows](Override-Micro-Shadows.html): HDRP uses the normal map and ambient occlusion map of a Material to estimate the shadows that those maps would cast if they were Meshes.

To decrease aliasing for the main cascade shadow maps, you can apply different filters to those maps. For more information on the filtering algorithms that HDRP uses, see the [filtering qualities documentation](Shadows-in-HDRP.html#FilteringQualities).

<a name="SkyOverview"></a>

### Sky 

![](Images/HDRPFeatures-Sky.png) 

In HDRP, you set up the sky inside a [Volume](Volumes.html), so you can change sky settings, or even the type of sky itself, depending on the position of the Camera in the Scene. HDRP allows you to separate the visual sky from the sky that you use for environment lighting. HDRP includes the following built-in sky types for you to use in your Unity Project:

- [Gradient Sky](Override-Gradient-Sky.html): Renders a simple sky with three color zones for the top, middle, and bottom sections of the sky.
- [HDRI Sky](Override-HDRI-Sky.html): Constructs a sky environment based on a cubemap texture you set within the HDRI Volume component.
- [Procedural Sky](Override-Procedural-Sky.html): Produces an environment based on the values you choose within the Procedural Sky Volume component. This is similar to Unity’s [built-in render pipeline procedural sky](https://docs.unity3d.com/Manual/class-Skybox.html).

<a name="FogOverview"></a>

### Fog

![](Images/HDRPFeatures-Fog.png)

In HDRP, you set up fog, inside a [Volume](Volumes.html),so you can change fog settings, or even the fog type itself, depending on the position of the Camera in the Scene. You can set the color of the fog yourself or use the color of the sky. HDRP fog affects Materials with both opaque and transparent surfaces. HDRP includes the following built-in fog types for you to use in your Unity Project:

- [Exponential Fog](Override-Exponential-Fog.html): Increases fog density exponentially with view distance and world space height. This fog type offers a more realistic fog effect than linear fog, but is more resource intensive. 
- [Linear Fog](Override-Linear-Fog.html): Increases fog density linearly with view distance and world space height. This is useful for applying fog to rendering large low-priority areas of your Scene because it is less resource intensive than the other fog types. 
- [Volumetric Fog](Override-Volumetric-Fog.html): Volumetric fog realistically simulates lights that interact with fog, such as glow and crepuscular rays. Volumetric fog works with Directional, Point, and Spot lights.

### Light Layers 

![](Images/HDRPFeatures-LightLayers.png)

Light Layers are LayerMasks that you specify for Lights and Meshes. Lights only illuminate Meshes that are on Light Layers that you enable on the Light. For more information on Light Layers, see the [Light Layers documentation](Light-Layers.html).

### Screen space ambient occlusion 

HDRP includes a [screen space ambient occlusion](Override-Ambient-Occlusion.html) effect that approximates ambient occlusion in real time. It approximates the intensity and position of ambient light on a GameObject’s surface, based on the light in the Scene and the environment around the GameObject.

### Screen space specular occlusion

HDRP also provides an extension to the screen space ambient occlusion effect which supports directional information. HDRP applies this directly to indirect specular lighting.

<a name="PLU"></a>

### Physical light units

HDRP uses real-world physical light units, so you can easily light your Scene in a realistic way. For more information on physical light units, including a list of which units HDRP uses, see the [physical light units documentation](Physical-Light-Units.html).

<a name="Camera"></a>

## Camera

### Post-processing

![](Images/HDRPFeatures-PostProcessing.png)

HDRP includes its own purpose-built implementation for post-processing to produce exceptionally high-quality graphics. You can use post-processing to apply full-screen filters and effects to the Camera to drastically improve the visuals of your Unity Project with little set-up time. For an overview on HDRP post-processing, see the [post-processing documentation](Post-Processing-Main.html).

### Anti-Aliasing

![](Images/HDRPFeatures-AntiAliasing.png) 

HDRP includes the following anti-aliasing  methods to help you remove aliasing effects with performance and quality in mind:

- Multisample anti-aliasing: Samples multiple locations within every pixel and combines these samples to produce the final pixel. This is the most resource intensive anti-aliasing technique in HDRP.
- Temporal anti-aliasing: Uses frames from a history buffer to smooth edges more effectively than fast approximate anti-aliasing. It is substantially better at smoothing edges in motion, but you must enable motion vectors for this.
- Subpixel morphological anti-aliasing: Finds patterns in borders of the image and blends the pixels on these borders according to the pattern.
- Fast approximate anti-aliasing: Smooths edges on a per-pixel level. This is the least resource intensive anti-aliasing technique in HDRP.

### Physical Camera

HDRP uses a physically-based Camera system that works seamlessly with the other physical features of HDRP, such as [physical light units](#PLU), to produce physically-accurate, unified results. A physically-based camera works like a real-world camera, and therefore uses the same properties. This allows you to configure an HDRP Camera to mimic the behavior of a real-world camera, with expected results for effects such as exposure and depth of field. For more information on HDRP's physically-based camera, includings a description of how to use it, see the [Camera component documentation](HDRP-Camera.html).

<a name="Tools"></a>

## Tools

### Render Pipeline Debugger

The Render Pipeline Debugger contains many debugging and visualization tools to help you to understand and solve any issues quickly. For more information on the Render Pipeline Debugger, includings a description of how to use it, see the [Render Pipeline Debugger documentation](Render-Pipeline-Debug-Window.html).

### Render Pipeline Wizard

To help you set up HDRP quickly and correctly, HDRP provides the Render Pipeline Wizard. Use the wizard to fix configuration issues with a single button click, and create a template Scene that is already configured with default Scene settings. For more information on the Render Pipeline Wizard, including a description of how to use it, see the [Render Pipeline Wizard documentation](Render-Pipeline-Wizard.html).

<a name="Programming"></a>

## Programming

### Material architecture

HDRP supports forward and deferred Materials. Deferred Materials are more efficient than Forward Materials, but support a slightly limited set of properties. Forward Material have no property limitations, but are slower to render than Deferred Materials.

HDRP's Material architecture allows you to add your own forward Materials and includes a list of rules you must follow to compile your Material in HDRP.

### Lighting architecture

HDRP uses a hybrid tile and cluster renderer for [forward and deferred rendering](Forward-And-Deferred-Rendering.html) of opaque and transparent GameObjects. This creates a local light list to allow HDRP to render a high number of Lights. Use the forward renderer to light more complex Materials, such as those that use subsurface scattering or are anisotropic. Use the deferred renderer to increase the processing speed of lighting for common Materials, such as standard Lit or Unlit Materials. For more information on HDRP's lighting architecture, including an explanation of tile and cluster rendering, see the [lighting pipeline documentation](<https://docs.unity3d.com/Manual/BestPracticeLightingPipelines.html>).