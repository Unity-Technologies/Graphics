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

## Issues resolved

For information on issues resolved in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
