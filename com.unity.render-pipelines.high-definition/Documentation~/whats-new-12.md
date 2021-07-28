# What's new in HDRP version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Features

The following is a list of features Unity added to version 12 of the High Definition Render Pipeline, embedded in Unity 2021.2. Each entry includes a summary of the feature and a link to any relevant documentation.

### Render the Emissive contribution of a Lit Deferred Material in a separate forward pass.

From HDRP 12.0, you can render the Emissive contribution of a Lit Material in a separate pass when the Lit Shader Mode is set to Both or Deferred in the HDRP settings. You can do this instead of using the GBuffer pass.
This can be used to fix artefacts when using [Screen Space Global Illumination](Override-Screen-Space-GI.md) - With or without Raytracing enabled - and Emissive Material with Lit Shader Mode setup as Both or Deferred. Previously it Emissive contributoin was drop, now it is keep. Same usage for the new Adaptive probe volumes.
Limitation: When Unity performs a separate pass for the Emissive contribution, it also performs an additional DrawCall. This means it uses more resources on your CPU.
Group of Materials / GameObject can be setup to use Force Emissive forward with the script "Edit/Render Pipeline/HD Render Pipeline/Force Forward Emissive on Material/Enable In Selection".

### Adding Tessellation support for ShaderGraph Master Stack

From HDRP 12.0, you can enable [tessellation](Tessellation.md) on any HDRP [Master Stack](master-stack-hdrp.md). The option is in the Master Stack settings and adds two new inputs to the Vertex Block:

* Tessellation Factor
* World Displacement

For more information about tessellation, see the [Tessellation documentation](Tessellation.md).

### Cloud System

![](Images/HDRPFeatures-CloudLayer.png)

From HDRP 12.0, HDRP introduces a cloud system, which can be controlled through the volume framework in a similar way to the sky system.

HDRP includes a Cloud Layer volume override which renders a cloud texture on top of the sky. For more information, see the [Cloud Layer](Override-Cloud-Layer.md) documentation.

For detailed steps on how to create your custom cloud solution, see the documentation about [creating custom clouds](Creating-Custom-Clouds.md).

### Lens Flares

![](Images/LensFlareSamples2.png)

From HDRP 12.0, HDRP (and URP) introduces a new Lens Flare system. You can attach a Lens Flare (SRP) component to any GameObject.
Some Lens Flare properties only appear when you attach this component to a light. Each Lens Flare can have with multiple elements that you can control individually. HDRP also provides a [new asset](lens-flare-data-driven-asset.md) and a [new component](lens-flare-data-driven-component.md) which you can attach to any GameObject.

### Light Anchor

![](Images/LightAnchor0.png)

From HDRP 12.0, HDRP (and URP) introduces a new [Light Anchor](light-anchor.md) component. You can attach this component to any light to control the light in Main Camera view.

## Improvements

### Area Lights

The AxF shader and Fabric and Hair master nodes now correctly support Area lights.

### Density Volume (Local Volumetric Fog) Improvements

Density Volumes are now known as **Local Volumetric Fog**. This is a more accurate, descriptive name that removes confusion with [Volumes](Volumes.md) and makes the relation to fog clearer.

Local Volumetric Fog masks now support using 3D RenderTextures as masks. 3D mask textures now also use all four RGBA channel which allows volumetric fog to have different colors and density based on the 3D Texture.

The size limit of 32x32x32 for the mask textures has also been replaced by a setting in the HDRP asset called "Max Local Volumetric Fog Resolution", under the Lighting > Volumetrics section. The upper limit for mask textures is now 256x256x256, an info box below the field tells you how much memory is allocated to store these textures. Note that increasing the resolution of the mask texture doesn't necessarily improve the quality of the volumetric, what's important is to have a good balance between the **Volumetrics** quality and the Local Volumetric Fog resolution.

There is a new field to change the falloff HDRP applies when it blends the volume using the Blend Distance property. You can choose either Linear which is the default and previous technique, or Exponential which is more realistic.

Finally, the minimal value of the **Fog Distance** parameter was lowered to 0.05 instead of 1 and now allows thicker fog effects to be created.

### Dynamic Resolution Scale
This version of HDRP introduces multiple improvements to Dynamic Resolution Scaling:
- The exposure and pixel to pixel quality now match between the software and hardware modes.
- The rendering artifact that caused black edges to appear on screen when in hardware mode no longer occurs.
- The rendering artifacts that appeared when using the Lanczos filter in software mode no longer occur.
- Hardware mode now utilizes the Contrast Adaptive Sharpening filter to prevent the results from looking too pixelated. This uses FidelityFX (CAS) AMD™. For information about FidelityFX and Contrast Adaptive Sharpening, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx).
- Fixing a corrupted scaling on dx12 hardware mode when a planar reflection probe / secondary camera is present.
- New API in DynamicResolutionHandler to handle multicamera rendering for hardware mode. Changing cameras and resetting scaling per camera should be safe.

### AOV API

From HDRP 12.0, The AOV API includes the following improvements:
- It is now possible to override the render buffer format that is used internally by HDRP when rendering **AOVs**. This can be done by a call to aovRequest.SetOverrideRenderFormat(true);
- There is now a world space position output buffer (see DebugFullScreen.WorldSpacePosition).
- The MaterialSharedProperty.Specular output now includes sensible information even for materials that use the metallic workflow (by converting the metallic parameter to fresnel 0).

### Additional Properties

From HDRP 12.0, More Options have become Additional Properties. The way to access them has also changed. The cogwheel that was present in component headers has been replaced by an entry in the contextual menu. When you enable additional properties, Unity highlights the background of each additional property for a few seconds to show you where they are.

### More path traced materials

![](Images/HDRPFeatures-FabricPT.png)

HDRP path tracing now supports the following Materials:

- Fabric: Cotton/wool and silk variants.
- AxF: SVBRDF and car paint variants.
- Stacklit.

### Top level menus

From HDRP 12.0, various top level menus are now different. This is to make the top level menus more consistent between HDRP and the Universal Render Pipeline. The top level menus this change affects are:

* **Window**
  * **HD Render Pipeline Wizard** is now at **Window > Rendering > HDRP Wizard**
  * **Graphics Compositor** is now at **Window > Rendering > Graphics Compositor**
* **Assets**
  * HDRP Shader Graphs are now in **Assets > Create > Shader Graph > HDRP**
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

From HDRP 12.0, an option has been added in the HDRP asset to allow decal normals to be additively blended with the underlying object normal.
The screenshot on the left below do not use additive normal blending, whereas the screenshot on the right use the new method.

![](Images/HDRPFeatures-SurfGrad.png)

### Physical Camera

HDRP 12.0 includes the following physical camera improvements:
- Many physical camera properties can now be animated with keyframes using a Unity Timeline.
- Added the **Focus Distance** property to the physical camera properties. To improve compatibility with older HDRP versions, this property is only used in DoF computations if the **Focus Distance Mode** in the  [Depth of Field](Post-Processing-Depth-of-Field.md) volume component is set to **Camera**. |

### Depth Of Field

Improved the quality of the physically-based Depth Of Field.
![](Images/HDRPFeatures-BetterDoF.png)

## Issues resolved

For information on issues resolved in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
