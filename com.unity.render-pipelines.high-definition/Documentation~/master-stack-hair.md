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
    
## Phenomenological and Physically Based Models

The Hair Master Stack offers two model sub-types: **Basic** and **Physical**. By default, a newly created Hair Shader Graph is configured to use the **Basic** mode. To change it, simply navigate to the Graph Inspector and change the Hair's **Material Type** from **Basic** to **Physical**.

![](Images/hair-kajiya.png)

The **Basic** mode is a non energy-conserving model that was originally crafted against perceptual observations of human hair. Effective use of this model requires the artist to carefully balance the energy between the specular terms using multiple color parameters. Generally, the **Basic** model is accurate enough for darker hair, but falls short for lighter hair. Additionally, it is the faster of the two models to compute.

![](Images/hair-marschner.png)

The **Physical** mode puts parameters in much simpler and meaningful terms. This model is considered to be physically-based due to its considerations for how incident light has been measured to scatter in a hair fiber. While the **Basic** variant requires four color parameters to tune overall appearance, the **Physical** variant only requires one. The **Base Color** parameter defines the hair cortex absorption, the fibrous structure underlying the cuticle scale. Additionally, the model is energy conserving, so no careful balancing of inputs should be required for your hair to fit naturally into any lighting scenario. 

A crucial component to the appearance of (especially light colored) hair is *multiple scattering*. Almost always, we never shade just a single hair fiber, but typically many thousands of fibers within close adjacency to one another. Because of this, coupled with the fact that light colored (lower absorbing) hair transmits large amount of light, the overall effect is a volumetric appearance to a head of light colored hair. 

![](Images/hair-multiple-scattering.png)

By default, for both **Basic** and **Physical** models, we use a diffuse approximation term to extremely coarsely approximate this phenomenon (seen left). The approximation is coarse because it does not take into account the propogation and attenuation of light through a hair volume due to transmittance, and ignores the effect that a hair's roughness has on the spread of light. 

For this reason, we offer an **Advanced Scattering Mode** (seen right). The **Advanced Scattering Mode** performs a physically based simulation of light propogating through a volume of hair. This mode is strictly allowed only in **Physical** mode since the computation is dependant on the physically based nature of the model. 

There are currently other important constraints on the **Advanced Scattering Mode** to make note of. Currently, it is dependent on the use of a strand geometry representation (in fact, it is generally recommended at the moment to only use the **Physical** mode when dealing with strands). Additionally, this approach has a dependency on important precomputed information stored in a **Strand Count Probe**. While it can be computed manually, this data is not readily available at the moment. The accessibility of the **Advanced** scattering will be improved in future releases. 

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
