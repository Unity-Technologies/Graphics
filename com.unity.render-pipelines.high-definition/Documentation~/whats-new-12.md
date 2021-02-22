# What's new in HDRP version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Features

The following is a list of features Unity added to version 12 of the High Definition Render Pipeline, embedded in Unity 2021.2. Each entry includes a summary of the feature and a link to any relevant documentation.

### Render the Emissive contribution of a Lit Deferred Material in a separate forward pass.

From HDRP 12.0, you can render the Emissive contribution of a Lit Material in a separate pass when the Lit Shader Mode is set to Both or Deferred in the HDRP settings. You can do this instead of using the GBuffer pass.
This can be used to fix artefacts when using [Screen Space Global Illumination](Override-Screen-Space-GI.md) - With or without Raytracing enabled - and Emissive Material with Lit Shader Mode setup as Both or Deferred. Previously it Emissive contributoin was drop, now it is keep. Same usage for the new Adaptive probe volumes.
Limitation: When Unity performs a separate pass for the Emissive contribution, it also performs an additional DrawCall. This means it uses more resources on your CPU.
Group of Materials / GameObject can be setup to use Force Emissive forward with the script "Edit/Render Pipeline/HD Render Pipeline/Force Forward Emissive on Material/Enable In Selection".


## Improvements

### Dynamic Resolution Scale
This version of HDRP introduces multiple improvements to Dynamic Resolution Scaling:
- The exposure and pixel to pixel quality now match between the software and hardware modes.
- The rendering artifact that caused black edges to appear on screen when in hardware mode no longer occurs.
- The rendering artifacts that appeared when using the Lanczos filter in software mode no longer occur.
- Hardware mode now utilizes the Contrast Adaptive Sharpening filter to prevent the results from looking too pixelated. This uses FidelityFX (CAS) AMD™. For information about FidelityFX and Contrast Adaptive Sharpening, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx).


### AOV API

From HDRP 12.0, The AOV API includes the following improvements:
- It is now possible to override the render buffer format that is used internally by HDRP when rendering **AOVs**. This can be done by a call to aovRequest.SetOverrideRenderFormat(true);
- There is now a world space position output buffer (see DebugFullScreen.WorldSpacePosition).
- The MaterialSharedProperty.Specular output now includes sensible information even for materials that use the metallic workflow (by converting the metallic parameter to fresnel 0).

### Additional Properties

From HDRP 12.0, More Options have become Additional Properties. The way to access them has also changed. The cogwheel that was present in component headers has been replaced by an entry in the contextual menu. When you enable additional properties, Unity highlights the background of each additional property for a few seconds to show you where they are.

### Top level menus

From HDRP 12.0, various top level menus are now different. This is to make the top level menus more consistent between HDRP and the Universal Render Pipeline. The top level menus this change affects are:

* **Window**
  * **HD Render Pipeline Wizard** is now at **Window > Rendering > HDRP Wizard**
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
  * **Density Volume** is now at **GameObject > Volume > Density Volume**
  * **Decal Projector** is now at **GameObject > Decal Projector**
  * **Sky and Fog Volume** is now at **GameObject > Volume > Sky and Fog Global Volume**

## Issues resolved

For information on issues resolved in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
