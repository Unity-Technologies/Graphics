# Decal

The Decal Master Stack material type enables you to author decals that you can [project](Decal-Projector.md) or place into your Scene. The Decal Master Stack material type is similar to the standard [Decal Shader](Decal-Shader.md), except that you cannot use this version to author decals projected on transparent materials.



![](Images/HDRPFeatures-DecalShader.png)

## Creating a Decal Shader Graph

To create a Decal material in Shader Graph, you can either:

* Modify an existing Shader Graph.
    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button, and select **HDRP**.
    3. In the **Material** drop-down, select **Decal**.

* Create a new Shader Graph.
    1. Go to **Assets > Create >Shader Graph > HDRP** and click **Decal Shader Graph**.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

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

This Master Stack material type adds all its Vertex Blocks to the Vertex Context by default and has no extra relevant Blocks.

### Fragment Context

#### Default

When you create a new Decal Master Stack, the Fragment Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/base-color.md)]
[!include[](snippets/shader-graph-blocks/alpha.md)]
[!include[](snippets/shader-graph-blocks/normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/normal-alpha.md)]
[!include[](snippets/shader-graph-blocks/metallic.md)]
[!include[](snippets/shader-graph-blocks/ambient-occlusion.md)]
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/maos-alpha.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]
</table>

#### Relevant

This Master Stack material type adds all its Fragment Blocks to the Fragment Context by default and has no extra relevant Blocks.

## Graph Settings

### Surface Options

| **Setting**                  | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Affect BaseColor**         | Indicates whether the decal shader affects the base color of materials it is projected or placed on. |
| **Affect Normal**            | Indicates whether the decal shader affects the normals of GameObjects it is projected or placed on. When enabled, the shader uses the **Normal** Block to override the receiving Material's normals. |
| **Affect Metal**             | Indicates whether the decal shader affects the metallic property of materials it is projected or placed on. When enabled, the shader uses the **Metallic** Block to override the receiving Material's metallic property. |
| **Affect Ambient Occlusion** | Indicates whether the decal shader affects the ambient occlusion property of materials it is projected or placed on. When enabled, the shader uses the **Ambient Occlusion** Block to override the receiving Material's ambient occlusion. |
| **Affect Smoothness**        | Indicates whether the decal shader affects the smoothness property of materials it is projected or placed on. When enabled, the shader uses the **Smoothness** Block to override the receiving Material's smoothness property. |
| **Affect Emissive**          | Indicates whether the decal shader affects the emission property of materials it is projected or placed on. When enabled, the shader uses the **Emission** Block to override the receiving Material's emission property. Emissive Materials appear self-illuminated and act as a visible source of light. This property does not work with receiving Materials that are transparent. |
