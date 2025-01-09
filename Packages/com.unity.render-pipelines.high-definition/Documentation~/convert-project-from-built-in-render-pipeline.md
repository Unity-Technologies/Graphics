# Convert a project from the Built-in Render Pipeline

The High Definition Render Pipeline (HDRP) uses a new set of shaders and lighting units, both of which are incompatible with the [Built-in Render Pipeline](https://docs.unity3d.com/Manual/built-in-render-pipeline.html). To upgrade a Unity Project to HDRP, you must first convert all your materials and shaders, then adjust individual light settings accordingly.

| Topic | Description | 
|-|-|
| [Convert post-processing scripts](convert-from-built-in-convert-post-processing-scripts.md) | Remove the Post-Processing Version 2 package from a project and update your scripts to work with HDRP's own implementation for post processing. |
| [Convert lighting and shadows](convert-from-built-in-convert-lighting-and-shadows.md) | Convert a project to physical Light units to control the intensity of Lights, instead of the arbitrary units the Built-in Render Pipeline uses. | 
| [Convert materials and shaders](convert-from-built-in-convert-materials-and-shaders.md) | Upgrade the materials in your scene to HDRP-compatible materials, either automatically or manually. |
| [Convert project with HDRP wizard](convert-from-built-in-convert-project-with-hdrp-wizard.md) | Add the HDRP package to a Built-in Render Pipeline project and set up HDRP. |
