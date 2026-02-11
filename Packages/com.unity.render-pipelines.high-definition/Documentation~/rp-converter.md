# Render Pipeline Converter window for HDRP reference

After you [set up your project to use HDRP](convert-from-built-in-convert-project-with-hdrp-wizard.md), you can use the Render Pipeline Converter to convert shaders made for a Built-In Render Pipeline project to assets compatible with the High Definition Render Pipeline (HDRP).

To open the Render Pipeline Converter window, go to **Window** > **Rendering** > **Render Pipeline Converter**. For more information on how to use the Render Pipeline Converter for HDRP, refer to [Convert materials and shaders](convert-from-built-in-convert-materials-and-shaders.md#render-pipeline-converter).

**Warning**: Using the Render Pipeline Converter overwrites several files in your project folder. These can't be restored after Unity overwrites them. Before you start this task, back up any files you don't want to lose.

## Pipeline Converter tab

| **Property** | **Description** |
|:-------------|:----------------|
| **Source Pipeline** | Sets the pipeline you want to convert your assets from. |   
| **Target Pipeline** | Sets the pipeline you want to convert your assets to.   |  
| **Material Shader Converter** | Converts [prebuilt shaders for the Built-In Render Pipeline](https://docs.unity3d.com/Manual/shader-built-in-birp.html) to [shaders in HDRP](materials-and-surfaces.md). This converter doesn't support custom shaders. For more information on how to convert custom shaders for HDRP, refer to [Convert materials manually](convert-from-built-in-convert-materials-and-shaders.md#ManualConversion).|