# What's new in HDRP version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Features

The following is a list of features Unity added to version 12 of the High Definition Render Pipeline, embedded in Unity 2021.2. Each entry includes a summary of the feature and a link to any relevant documentation.



## Improvements

### AOV API Improvements

From HDRP 12.0, The AOV API includes the following improvements
- It is now possible to override the render buffer format that is used internally by HDRP when rendering **AOVs**. This can be done by a call to aovRequest.SetOverrideRenderFormat(true);
- There is now a world space position output buffer (see DebugFullScreen.WorldSpacePosition).
- The MaterialSharedProperty.Specular output now includes sensible information even for materials that use the metallic workflow (by converting the metallic parameter to fresnel 0).

### Additional Properties

From HDRP 12.0, More Options have become Additional Properties. The way to access them has also changed. The cogwheel that was present in component headers has been replaced by an entry in the contextual menu. When you enable additional properties, Unity highlights the background of each additional property for a few seconds to show you where they are.

### Top level menus

From HDRP 12.0, various top level menus are now more consistent with the Universal Render Pipeline. The top level menus this change affects are:

* **Window**
  * HD Render Pipeline Wizard is now at **Window > Rendering > HDRP Wizard**
* **Assets**
  * HDRP Shader Graphs are now at **Assets > Create > Shader Graph > HDRP**
  * Custom FullScreen Pass is now at **Assets > Create > Shader > HDRP Custom FullScreen Pass**
  * Custom Renderers Pass is now at **Assets > Create > Shader > HDRP Custom Renderers Pass**
  * Post Process Pass is now at **Assets > Create > Shader > HDRP Post Process**
  * High Definition Render Pipeline Asset is now at **Assets > Create > Rendering > HDRP Asset**
  * Diffusion Profile is now at **Assets > Create > Rendering > HDRP Diffusion Profile**
  * C# Custom Pass is now at **Assets > Create > Rendering > HDRP C# Custom Pass**
  * C# Post Process Volume is now at **Assets > Create > Rendering > HDRP C# Post Process Volume**
* **GameObject**
  * Density Volume is now at **GameObject > Volume > Density Volume**
  * Decal Projector is now at **GameObject > Decal Projector**
  * Sky and Fog Volume is now at **GameObject > Volume > Sky and Fog Global Volume**

## Issues resolved

For information on issues resolved in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
