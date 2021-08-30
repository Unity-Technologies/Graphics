# What's new in HDRP version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Features

The following list of features are new to version 12 of the High Definition Render Pipeline (HDRP), as part of  Unity 2021.2.

### Render the Emissive contribution of a Lit Deferred Material in a separate forward pass.

From HDRP 12.0, you can render the Emissive contribution of a Lit Material in a separate forward pass when the **Lit Shader Mode** is set to **Both** or **Deferred** in the [HDRP global settings](Default-Settings-Window.md) window. To do this, enable the new **Force Forward Emissive** property in the **Advanced options** of a [Lit Shader](Lit-Shader.md) or a [Layered Lit Shader](Layered-Lit-Shader.md).  You can do this instead of using a GBuffer pass.

You can also make a group of Materials or GameObjects use Force Forward Emissive in the following Menu path: **Edit > Render Pipeline > HD Render Pipeline > Force Forward Emissive on Material > Enable In Selection**.

You can use this new behaviour to fix artefacts when you use [Screen Space Global Illumination](Override-Screen-Space-GI.md), with or without Raytracing enabled. When you enable the **Force Forward Emissive**  property, Unity renders the Emissive Object and its contribution in the scene. You can use this in the same way for Adaptive probe volumes.

#### Limitations

When Unity performs a separate pass for the Emissive contribution, it also performs an additional `DrawCall`. This means it uses more resources on your CPU.

### Adding Tessellation support for ShaderGraph Master Stack

From HDRP 12.0, you can enable [tessellation](Tessellation.md) on any HDRP [Master Stack](master-stack-hdrp.md). The option is in the Master Stack settings and adds two new inputs to the Vertex Block:

* Tessellation Factor
* World Displacement

For more information about tessellation, see the [Tessellation documentation](Tessellation.md).

### Adding custom Velocity support for ShaderGraph Master Stack

From HDRP 12.0, you can enable custom Velocity on any HDRP [Master Stack](master-stack-hdrp.md). This option is in the Master Stack settings and adds one new input to the Vertex Block:

* Velocity

You can use this Vertex Block to create an additional velocity for procedural geometry (for example, generated hair strands) and to get a correct motion vector value.

### Cloud System

![](Images/HDRPFeatures-CloudLayer.png)

HDRP 12.0 introduces a cloud system that you can control through the volume framework.

HDRP includes a Cloud Layer volume override which renders a cloud texture on top of the sky. For more information, see the [Cloud Layer](Override-Cloud-Layer.md) documentation.

For detailed steps on how to create custom clouds in your scene, see [creating custom clouds](Creating-Custom-Clouds.md).

### Lens Flares

![](Images/LensFlareSamples2.png)

HDRP 12.0 includes a new Lens Flare system. You can attach a Lens Flare (SRP) component to any GameObject.

Some Lens Flare properties only appear when you attach a Lens Flare (SRP) component to a light. Each Lens Flare has optional multiple elements that you can control individually. HDRP also provides a [new lens flare asset](lens-flare-data-driven-asset.md) and a [new lens flare component](lens-flare-data-driven-component.md) that you can attach to any GameObject.

### Light Anchor

![](Images/LightAnchor0.png)

From HDRP 12.0, HDRP includes a new [Light Anchor](light-anchor.md) component. You can attach this component to any light to control the light in Main Camera view.

## Improvements

### Area Lights

- The AxF shader and Fabric and Hair master nodes now correctly support Area lights.

### Density Volume (Local Volumetric Fog) Improvements

HDRP 12.0 introduces multiple improvements to Density Volumes (Local Volumetric Fog):

- Density Volumes have been renamed **Local Volumetric Fog**. This new name removes confusion with [Volumes](Volumes.md) and makes it clear that this feature creates a fog effect.

- Local Volumetric Fog masks now support 3D RenderTextures as masks.
- 3D mask textures now use all four RGBA channels.  This allows volumetric fog to have different colors and density based on the 3D Texture.

- You can now change the mask texture size limit (32x32x32). To do this, use the new setting [HDRP Asset](HDRP-Asset.md) in the **Lighting > Volumetrics** section, called **Max Local Volumetric Fog Resolution** . The upper limit for mask textures is now 256x256x256. An information box below the **Max Local Volumetric Fog Resolution** field tells you how much memory Unity allocates to store these textures. When you increase the resolution of the mask texture, it doesn't always improve the quality of the volumetric fog. Instead, use this property to find a balance between the **Volumetrics** quality and the **Local Volumetric Fog** resolution.

- There is a new field to change the falloff HDRP applies when it blends a volume using the **Blend Distance** property. You can choose the **Linear** setting, which is the default and previous technique, or the **Exponential** setting, which is more realistic.

- The minimal value of the **Fog Distance** parameter is 0.05 instead of 1. You can use this value to create fog effects that appear more dense.

### Dynamic Resolution Scale
This version of HDRP introduces multiple improvements to Dynamic Resolution Scaling:
- The exposure and pixel to pixel quality now match between the software and hardware modes.
- The rendering artifact that caused black edges to appear on screen when in hardware mode no longer occurs.
- The rendering artifacts that appeared when using the Lanczos filter in software mode no longer occur.
- Hardware mode now utilizes the Contrast Adaptive Sharpening filter to prevent pixelated results. This uses FidelityFX (CAS) AMD™. For information about FidelityFX and Contrast Adaptive Sharpening, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx).
- Fixed an issue that caused corrupted scaling on Dx12 hardware mode when a planar reflection probe or a secondary Camera is present.
- New API in `DynamicResolutionHandler` to handle multicamera rendering for hardware mode. This allows you to change Cameras and reset scaling per Camera with no issues.

### AOV API

From HDRP 12.0, The AOV API includes the following improvements:
- You can now override the render buffer format that HDRP uses internally when rendering **AOVs**. To do this, call `aovRequest.SetOverrideRenderFormat(true);`.
- This version of HDRP provides a world space position output buffer (see `DebugFullScreen.WorldSpacePosition`).
- The `MaterialSharedProperty.Specular` output now includes information for Materials that use the metallic workflow. This is done by converting the metallic parameter to fresnel 0.

### Additional Properties

From HDRP 12.0, **More Options** have been renamed **Additional Properties**. There is also a new way to access these properties. The cogwheel that was present in component headers has been replaced by an entry in the contextual menu. When you enable Additional Properties, Unity highlights the background of each additional property for a few seconds to show you where they are.

### More path traced materials

![](Images/HDRPFeatures-FabricPT.png)

HDRP path tracing now supports the following Materials:

- Fabric: Cotton/wool and silk variants.
- AxF: SVBRDF and car paint variants.
- Stacklit.

### Top level menus

HDRP 12.0 includes changes to some top level menus. This is to make the top level menus more consistent between HDRP and the Universal Render Pipeline. The top level menus this change affects are:

* **Window**
  * **HD Render Pipeline Wizard** is now at **Window > Rendering > HDRP Wizard**
  * **Graphics Compositor** is now at **Window > Rendering > Graphics Compositor**
* **Assets**
  * **HDRP Shader Graphs** are now in **Assets > Create > Shader Graph > HDRP**
  * **Custom FullScreen Pass** is now at **Assets > Create > Shader > HDRP Custom FullScreen Pass**
  * **Custom Renderers Pass** is now at **Assets > Create > Shader > HDRP Custom Renderers Pass**
  * **Post Process Pass** is now at **Assets > Create > Shader > HDRP Post Process**
  * **High Definition Render Pipeline Asset** is now at **Assets > Create > Rendering > HDRP Asset**
  * **Diffusion Profile** is now at **Assets > Create > Rendering > HDRP Diffusion Profile**
  * **C# Custom Pass** is now at **Assets > Create > Rendering > HDRP C# Custom Pass**
  * **C# Post Process Volume** is now at **Assets > Create > Rendering > HDRP C# Post Process Volume**
* **GameObject**
  * **Density Volume** is now at **GameObject > Rendering > Local Volumetric Fog**
  * **Sky and Fog Volume** is now at **GameObject > Volume > Sky and Fog Global Volume**

### Decal normal blending

From HDRP 12.0 you can use a new option in the [HDRP Asset](HDRP-Asset.md) ( **Rendering > Decals > Additive Normal Blending**) additively blend decal normals with the GameObject's normal map.

The example image on the left below does not this method. The image on the right use the new additive normal blending method.

![](Images/HDRPFeatures-SurfGrad.png)

### Physical Camera

HDRP 12.0 includes the following physical Camera improvements:
- You can now animate many physical Camera properties with keyframes using a Unity Timeline.
- Added the **Focus Distance** property to the physical Camera properties. To improve compatibility with older HDRP versions,  HDRP only includes this property in Depth of Field (DoF) computations if the **Focus Distance Mode** in the [Depth of Field](Post-Processing-Depth-of-Field.md) volume component is set to **Camera**.

### Depth Of Field

Improved the quality of the physically-based Depth Of Field.
![](Images/HDRPFeatures-BetterDoF.png)

### New shader for Custom Render Textures

This HDRP version includes a new shader that is formatted for [Custom Render Textures](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html) in **Assets > Create > Shader > Custom Render Texture**. To use this shader, create a new Material and assign it to the Custom Render Texture's **Material** field.

## Issues resolved

For information on issues resolved in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
