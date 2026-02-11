# Convert materials and shaders

After you [set up your project to use HDRP](convert-from-built-in-convert-project-with-hdrp-wizard.md), use the material upgraders or the [Render Pipeline Converter](rp-converter.md) to convert prebuilt material and shaders made for a Built-In Render Pipeline project to be compatible with the High Definition Render Pipeline (HDRP).

Material upgraders and the Render Pipeline Converter don't support converting custom materials or shaders. To convert custom materials and shaders, you must [convert them manually](#ManualConversion).

**Warning**: Using material upgraders or the Render Pipeline Converter overwrites several files in your project folder. These can't be restored after Unity overwrites them. Before you start this task, back up any files you don't want to lose.

## Convert materials and shaders using material upgraders

To upgrade the materials and shaders from the Built-In Render Pipeline to HDRP with material upgraders, do the following:

1. Go to **Edit** > **Rendering** > **Materials**
2. Choose one of the following options:

    * **Convert All Built-in Materials to HDRP**: Converts every compatible material in your project to an HDRP material.
    * **Convert Selected Built-in Materials to HDRP**: Converts every compatible material currently selected in the Project window to an HDRP Material.
    * **Convert Scene Terrains to HDRP Terrains**: Replaces the built-in default standard terrain material in every [Terrain](https://docs.unity3d.com/Manual/script-Terrain.html) in the scene with HDRP default Terrain material.

### Limitations

The automatic upgrade options described can't upgrade all materials to HDRP correctly:

* HDRP can only convert materials from the **Assets** folder of your project. HDRP uses the [error shader](xref:shader-error) for GameObjects that use the default read-only material from the Built-In Render Pipeline, for example [primitives](xref:um-primitive-objects).  
* Height mapped materials might look incorrect. This is because HDRP supports more height map displacement techniques and decompression options than the Built-In Render Pipeline. To upgrade a material that uses a heightmap, modify the Material's **Amplitude** and **Base** properties until the result more closely matches the Built-In Render Pipeline version.
* You can't upgrade particle shaders. HDRP doesn't support particle shaders, but it does provide Shader Graphs that are compatible with the [Built-in Particle System](https://docs.unity3d.com/Manual/Built-inParticleSystem.html). These Shader Graphs work in a similar way to the built-in particle shaders. To use these Shader Graphs, import the **Particle System Shader Samples** sample:

    1. Open the Package Manager window (menu: **Window** > **Package Management** > **Package Manager**).
    2. Find and click the **High Definition RP** entry.
    3. In the package information for **High Definition RP**, go to the **Samples** section and click the **Import into Project** button next to **Particle System Shader Samples**.

<a name="render-pipeline-converter"></a>

## Convert shaders using the Render Pipeline Converter

To convert prebuilt shaders from the Built-In Render Pipeline to HDRP with the Render Pipeline Converter, do the following:

1. Go to **Window** > **Rendering** > **Render Pipeline Converter**. 

    Unity opens the [Render Pipeline Converter window](rp-converter.md).  
2. Set **Source Pipeline** to **Built-in**.  
3. Set **Target Pipeline** to **High Definition Render Pipeline (HDRP)**.  
4. Select the checkbox next to **Material Shader Converter**. 
5. Select **Scan**. 

    Unity processes the assets in your project and displays the list of shaders it can convert under your selected converter.  
6. Select the checkboxes next to the assets you want to convert.  
7. Select **Convert Assets**. 

    When Unity finishes the conversion process, the window displays the status of each conversion.

### Shader mapping

The following table shows which HDRP resource path the Built-In Render Pipeline materials map to when you use the Render Pipeline Converter.

| **Built-In Render Pipeline material** | **HDRP resource path**                                                   |
| ------------------------------------- | ------------------------------------------------------------------------ |
| `Default-Diffuse`                     | `Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat`         |
| `Default-Material`	                | `Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat`         |
| `Default-ParticleSystem`              | `Runtime/RenderPipelineResources/Material/DefaultHDParticleMaterial.mat` |
| `Default-Particle`	                | `Runtime/RenderPipelineResources/Material/DefaultHDParticleMaterial.mat` |
| `Default-Terrain-Diffuse`	            | `Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat`  |
| `Default-Terrain-Specular`	        | `Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat`  |
| `Default-Terrain-Standard`            | `Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat`  |

<a name="ManualConversion"></a>

## Convert materials manually

HDRP uses multiple processes to automatically convert Built-in Standard and Unlit Materials to HDRP Lit and Unlit materials respectively. These processes use an overlay function to blend the color channels together, similar to the process you would use in image editing software like Adobe Photoshop.

To help you convert custom materials manually, this section describes the maps that the converter creates from the Built-in Materials.

### Mask maps

The Built-in Shader to HDRP Shader conversion process combines the different material maps of the Built-in Standard Shader into the separate RGBA channels of the mask map in the HDRP [Lit Material](lit-material.md). For information on which color channel each map goes in, refer to [Mask map](Mask-Map-and-Detail-Map.md#MaskMap).

### Detail maps

The Built-in Shader to HDRP Shader conversion process combines the different detail maps of the Built-in Standard Shader into the separate RGBA channels of the detail map in the HDRP [Lit Material](lit-material.md). It also adds a smoothness detail too. For information on which color channel each map goes in, refer to [Detail map](Mask-Map-and-Detail-Map.md#DetailMap).
