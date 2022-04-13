# Visual Effect Target

The Visual Effect Shader Graph [Target](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Graph-Target.html) enables you to create custom lit and unlit Shader Graphs to use in visual effects.

To create a Shader Graph that uses the Visual Effect Target, select **Assets** > **Create** > **Shader Graph** > **VFX Shader Graph**.

## Contexts

This Shader Graph Target its own set of Graph Settings. Because of the relationship between settings and Blocks, this has consequences on which Blocks are relevant to the Graph. This section contains information on the Blocks this Target adds by default, and which Blocks set properties for this Target's Graph Settings.

### Vertex Context

#### Default

When you create a new Shader Graph with the Visual Effect Target, the Vertex Context contains the following Blocks by default:

| **Property** | **Description**                              | **Setting Dependency** | **Default Value**      |
| ------------ | -------------------------------------------- | ---------------------- | ---------------------- |
| **Position** | The object space vertex normal per vertex.   | None                   | CoordinateSpace.Object |
| **Normal**   | The object space vertex position per vertex. | None                   | CoordinateSpace.Object |
| **Tangent**  | The object space vertex tangent per vertex.  | None                   | CoordinateSpace.Object |

#### Relevant

This Target adds all its Vertex Blocks to the Vertex Context by default and has no extra relevant Blocks.

### Fragment Context

#### Default

When you create a new Shader Graph with the Visual Effect Target, the Fragment Context contains the following Blocks by default:

| **Property**   | **Description**                                              | **Setting Dependency** | **Default Value** |
| -------------- | ------------------------------------------------------------ | ---------------------- | ----------------- |
| **Base Color** | The base color of the material                               | None                   | Color.grey        |
| **Alpha**      | The Material's alpha value. This determines how transparent the material is. The expected range is 0 - 1. | None                   | 1.0               |
| **Emission**   | The color of light to emit from this material's surface. Emissive materials appear as a source of light in your scene. | None                   | Color.black       |

#### Relevant

Depending on the [Graph Settings](#graph-settings) you use, Shader Graph can add the following Blocks to the Fragment Context:

| **Property**               | **Description**                                              | **Setting Dependency**              | **Default Value**       |
| -------------------------- | ------------------------------------------------------------ | ----------------------------------- | ----------------------- |
| **Alpha Clip Threshold**   | The alpha value limit that HDRP uses to determine whether to render each pixel. If the alpha value of the pixel is equal to or higher than the limit, HDRP renders the pixel. If the value is lower than the limit, HDRP does not render the pixel. | &#8226; **Alpha Clipping** enabled  | 0.5                     |
| **Metallic**               | The material's metallic value. This defines how "metal-like" the surface of your Material is (between 0 and 1). When a surface is more metallic, it reflects the environment more and its albedo color becomes less visible. At a full metallic level, the surface color is entirely driven by reflections from the environment. When a surface is less metallic, its albedo color is clearer and any surface reflections are visible on top of the surface color, rather than obscuring it. | &#8226; **Material** set to **Lit** | 0.0                     |
| **Smoothness**             | The material's smoothness. Every light ray that hits a smooth surface bounces off at predictable and consistent angles. For a perfectly smooth surface that reflects light like a mirror, set this to a value of 1. Less smooth surfaces reflect light over a wider range of angles (because the light hits the bumps in the microsurface), so the reflections have less detail and spread across the surface in a more diffused pattern. | &#8226; **Material** set to **Lit** | 0.5                     |
| **Normal (Tangent Space)** | The normal, in tangent space, for the material.              | &#8226; **Material** set to **Lit** | CoordinateSpace.Tangent |

## Graph Settings

| **Property**       | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Material**       | Specifies whether the lighting in the Scene affects the material. The options are:<br/>&#8226; **Unlit**: Scene lighting does not affect the material.<br/>&#8226; **Lit**: Scene lighting affects the material. This option adds extra Blocks to the fragment Context. |
| **Alpha Clipping** | Indicates whether this material acts like a[ Cutout Shader](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html). |
