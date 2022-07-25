# Hair

The Hair Master Stack enables you to render hair and fur in the High Definition Render Pipeline (HDRP). To create a realistic looking hair effect, it uses layers called hair cards. Each hair card represents a different section of hair. If you use semi-transparent hair cards, you must manually sort them so that they are in back-to-front order from every viewing direction.

![](Images/HDRPFeatures-HairShader.png)

## Creating a Hair Shader Graph

To create a Hair material in Shader Graph, you can either:

* Modify an existing Shader Graph.
    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button, and select **HDRP**.
    3. In the **Material** drop-down, select **Hair**.

* Create a new Shader Graph.
    1. Go to **Assets > Create >Shader Graph > HDRP** and click **Hair Shader Graph**.

<a name="hair-approximate-physical"></a>

## Hair Material Types

HDRP’s Hair Master Stack has the following **Material Type** options: 

- [Approximate](#hair-approximate): This mode requires you to adjust nodes to suit the lighting in your scene.
- [Physical](#hair-physical) : This mode automatically produces physically correct results.

<a name="hair-approximate"></a>

### Considerations for choosing a Material Type

This table explains the conditions under which you might prefer to choose the Physical or Approximate hair Material Types:

| **Material Type**                | **GPU requirements** | **Geometry compatibility**   | **Compatible with Path tracing**                             | **Hair tones**                                               |
| -------------------------------- | -------------------- | ---------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| [Approximate](#hair-approximate) | Moderate             | Hair cards.                  | No. <br/>For more information, see [Path tracing](Ray-Tracing-Path-Tracing.md.md#hair). | Works with all hair tones. However, it doesn't give light hair a volumetric appearance. |
| [Physical](#hair-physical)       | High                 | Hair cards and hair strands. | Yes. However, path tracing is not compatible with ribbons. <br/>For more information, see [Path tracing](Ray-Tracing-Path-Tracing.md.md#hair). | Works with all hair tones. Gives lighter hair tones a volumetric appearance when you use [multiple scattering](#hair-scattering) |

<a name="hair-approximate"></a>

### The Approximate hair Material Type

The **Approximate** Material Type mimics the characteristics of human hair. It is based on the [Kajiya-Kay](https://www.cs.drexel.edu/~david/Classes/Papers/p271-kajiya.pdf) hair shading model. HDRP computes this model faster than the [Physical](#hair-physical) Material Type because it is less resource intensive.

The Approximate model doesn’t automatically look realistic in every lighting setup. This means you need to adjust the blocks in the [Fragment context](master-stack-hair.html#fragment-context) to suit the lighting environment in your scene.

The Approximate model is best for darker hair tones. For best results with lighter hair tones, use the Physical model.

![](Images/hair-kajiya.png)

<a name="hair-physical"></a>

### The Physical Material Type

The Physical Material Type automatically creates physically correct results in any light environment. It accurately accounts for the amount of incident light to scatter within a hair fiber (known as an energy-conserving hair model). This means the Physical model works correctly in any lighting environment. 

This model adds the following nodes in the Fragment shader: 

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/smoothness-radial.md)]
[!include[](snippets/shader-graph-blocks/cuticle-angle.md)]
</table>

Change the [**Base Color** block](master-stack-hair.html#fragment-context) to define the color of the hair.

The Physical Material Type is based on the [Marschner](http://www.graphics.stanford.edu/papers/hair/hair-sg03final.pdf) human hair fiber reflectance model.

![](Images/hair-marschner.png)

<a name="hair-scattering"></a>

### Multiple Scattering 

Multiple scattering creates the appearance of light scattering through thousands of hair strands. This gives lighter hair tones a volumetric appearance. To enable multiple scattering in your scene, select a **Scattering Mode** option. 

To select a **Scattering Mode** option:

1. Open the Graph inspector.
2. Set the **Material Type** to Physical**.**
3. Set the **Geometry mode** to Strands.
4. Open the **Advanced Options** section.
5. Select **Scattering Mode.**

The Scattering Mode options appear when you select the **Physical** material type:

| Property        | Description                                                  |
| --------------- | ------------------------------------------------------------ |
| **Physical**    | Physically simulates light transport through a volume of hair (multiple scattering). This feature is not available for public use yet. |
| **Approximate** | Estimates the appearance of light transport through a volume of hair (multiple scattering). This mode does not take into account how transmittance affects the way light travels and slows through a volume of hair. It also ignores the effect that a hair's roughness has on the spread of light. |

![](Images/hair-multiple-scattering.png)

<a name="hair-geometry"></a>

## Geometry Type

You need to select a geometry type in your shader that reflects the geometry you use to represent hair. This allows HDRP to make correct assumptions when it computes the shading model. You can use multiple types of geometry to render hair, but the Hair Master Stack is only compatible with the following geometry types: 

- Cards: Hair cards display high-resolution hair textures on individual pieces of simplified geometry. 
  Card geometry is compatible with the Physical and Approximate Material types.
- Strands: Hair strand geometry represents each individual hair fiber in the shape of tube geometry or ribbons. 
  Strand geometry is compatible with the Physical Material Type.

The hair card method is a simple and efficient way to render hair for games, and doesn’t demand a lot of resources from the GPU. We recommend cards where the user experience will not be negatively impacted by it. For example, for secondary characters, and even as a lower level of detail for main characters. Use strands only for main characters.

### Select a geometry type

To select the geometry type that your shader uses:

1. Open your Hair shader
2. In the Graph inspector, open the **Advanced Options** dropdown

Select a 

Geometry Type

 option.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

When you create a new Hair Master Stack, the Vertex Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/vertex-position.md)]
[!include[](snippets/shader-graph-blocks/vertex-normal.md)]
[!include[](snippets/shader-graph-blocks/vertex-tangent.md)]
</table>

#### Relevant
Depending on the [Graph Settings](#graph-settings) you use, Shader Graph can add the following locks to the Vertex Context:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/tessellation-factor.md)]
[!include[](snippets/shader-graph-blocks/tessellation-displacement.md)]
</table>


This Master Stack material type adds all its Vertex Blocks to the Vertex Context by default and has no extra relevant Blocks.

### Fragment Context

#### Default

When you create a new Hair Master Stack, the Fragment Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/base-color.md)]
[!include[](snippets/shader-graph-blocks/normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/bent-normal.md)]
[!include[](snippets/shader-graph-blocks/hair-strand-direction.md)]
[!include[](snippets/shader-graph-blocks/transmittance.md)]
[!include[](snippets/shader-graph-blocks/rim-transmission-intensity.md)]
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/ambient-occlusion.md)]
[!include[](snippets/shader-graph-blocks/alpha.md)]
[!include[](snippets/shader-graph-blocks/specular-tint.md)]
[!include[](snippets/shader-graph-blocks/specular-shift.md)]
[!include[](snippets/shader-graph-blocks/secondary-specular-tint.md)]
[!include[](snippets/shader-graph-blocks/secondary-specular-shift.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]
</table>

#### Relevant

Depending on the [Graph Settings](#graph-settings) you use, Shader Graph can add the following blocks to the Fragment Context:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-depth-postpass.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-depth-prepass.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-shadow.md)]
[!include[](snippets/shader-graph-blocks/baked-back-gi.md)]
[!include[](snippets/shader-graph-blocks/baked-gi.md)]
[!include[](snippets/shader-graph-blocks/depth-offset.md)]
[!include[](snippets/shader-graph-blocks/normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-screen-space-variance.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-threshold.md)]
[!include[](snippets/shader-graph-blocks/specular-occlusion.md)]
[!include[](snippets/shader-graph-blocks/smoothness-radial.md)]
[!include[](snippets/shader-graph-blocks/cuticle-angle.md)]
[!include[](snippets/shader-graph-blocks/strand-count-probe.md)]
[!include[](snippets/shader-graph-blocks/strand-shadow-bias.md)]
</table>

## Graph Settings

### Surface Options
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/surface-options/material-type-hair.md)]
[!include[](snippets/shader-properties/surface-options/surface-type.md)]
[!include[](snippets/shader-properties/surface-options/rendering-pass.md)]
[!include[](snippets/shader-properties/surface-options/blending-mode.md)]
[!include[](snippets/shader-properties/surface-options/receive-fog.md)]
[!include[](snippets/shader-properties/surface-options/depth-test.md)]
[!include[](snippets/shader-properties/surface-options/depth-write.md)]
[!include[](snippets/shader-properties/surface-options/cull-mode.md)]
[!include[](snippets/shader-properties/surface-options/sorting-priority.md)]
[!include[](snippets/shader-properties/surface-options/back-then-front-rendering.md)]
[!include[](snippets/shader-properties/surface-options/transparent-depth-prepass.md)]
[!include[](snippets/shader-properties/surface-options/transparent-depth-postpass.md)]
[!include[](snippets/shader-properties/surface-options/transparent-writes-motion-vectors.md)]
[!include[](snippets/shader-properties/surface-options/preserve-specular-lighting.md)]
[!include[](snippets/shader-properties/surface-options/alpha-clipping.md)]
[!include[](snippets/shader-properties/surface-options/use-shadow-threshold.md)]
[!include[](snippets/shader-properties/surface-options/alpha-to-mask.md)]
[!include[](snippets/shader-properties/surface-options/double-sided-mode.md)]
[!include[](snippets/shader-properties/surface-options/fragment-normal-space.md)]
[!include[](snippets/shader-properties/surface-options/receive-decals.md)]
[!include[](snippets/shader-properties/surface-options/receive-ssr.md)]
[!include[](snippets/shader-properties/surface-options/receive-ssr-transparent.md)]
[!include[](snippets/shader-properties/surface-options/geometric-specular-aa.md)]
[!include[](snippets/shader-properties/surface-options/ss-depth-offset.md)]
[!include[](snippets/shader-properties/surface-options/conservative-depth-offset.md)]
[!include[](snippets/shader-properties/surface-options/velocity.md)]
[!include[](snippets/shader-properties/surface-options/tessellation.md)]
</table>

### Advanced Options
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/advanced-options/specular-occlusion-mode.md)]
[!include[](snippets/shader-properties/advanced-options/override-baked-gi.md)]
[!include[](snippets/shader-properties/advanced-options/support-lod-crossfade.md)]
[!include[](snippets/shader-properties/advanced-options/add-precomputed-velocity.md)]
[!include[](snippets/shader-properties/advanced-options/geometry-type.md)]
[!include[](snippets/shader-properties/advanced-options/scattering-mode.md)]
</table>
