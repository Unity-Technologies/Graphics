# Shader Graph Asset types reference

The Shader Graph Asset type you select when [creating a new Shader Graph](Create-Shader-Graph.md) changes the settings on your Shader Graph Asset, and might modify the default configuration of Block nodes in your Master Stack.

Each render pipeline has its own Shader Graph Asset types. You can use some Shader Graph Asset types regardless of the pipelines you have installed in your project.

## High Definition Render Pipeline (HDRP) Shader Graphs

The [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/) is one of Unity's Scriptable Render Pipelines. It allows you to create cutting-edge, high-fidelity graphics on high-end platforms.

If you have the HDRP installed in your project with Shader Graph, you can create the Shader Graph Assets described in the following table. They have the HDRP configured as an Active Target in the Graph Inspector:

| **Asset Type**            | **Description** |
| :-----------------------  | :-------------  |
| **Lit Shader Graph**      | The Lit Shader Graph is optimized for creating physically-based materials and supports various effects. You can use it for any material that interacts with the lighting in your scene. For more information on the changes that the Lit Shader Graph makes to your Shader Graph Asset's Master Stack, see [Lit Master Stack](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/master-stack-lit.html) in the HDRP documentation. |
| **Decal Shader Graph**    | The Decal Shader Graph is optimized for creating decals that you can project or place in your Scene. For more information on the changes that the Decal Shader Graph types makes to your Shader Graph Asset's Master Stack, see [Decal Master Stack](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/master-stack-decal.html) in the HDRP documentation.|
| **Fabric Shader Graph**   | The Fabric Shader Graph is optimized for creating fabric materials, using either cotton wool or anisotropic silk as its base. For more information on the changes that the Fabric Shader Graph makes to your Shader Graph Asset's Master Stack, see [Fabric Master Stack](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/master-stack-fabric.html) in the HDRP documentation.  |
| **Eye Shader Graph**      | The Eye Shader Graph is optimized for creating physically-based eye materials, using two different layers. For more information on the changes that the Eye Shader Graph makes to your Shader Graph Asset's Master Stack, see [Eye Master Stack](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/master-stack-eye.html) in the HDRP documentation.  |
| **Hair Shader Graph**     | The Hair Shader Graph is optimized for creating hair and fur using layers. For more information on the changes that the Hair Shader Graph makes to your Shader Graph Asset's Master Stack, see [Hair Master Stack](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/master-stack-hair.html) in the HDRP documentation.  |
| **Unlit Shader Graph**    | The Unlit Shader Graph is optimized for creating shaders or materials that aren't affected by lighting. For more information on the changes that the Unlit Shader Graph makes to your Shader Graph Asset's Master Stack, see [Unlit Master Stack](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/master-stack-unlit.html) in the HDRP documentation.              |
| **StackLit Shader Graph** | The StackLit Shader Graph is optimized for creating more complex materials than a **Lit Shader Graph**, with additional features and improvements. For more information on the changes that the StackLit Shader Graph makes to your Shader Graph Asset's Master Stack, see [StackLit Master Stack](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/master-stack-stacklit.html) in the HDRP documentation.                 |


## Universal Render Pipeline (URP) Shader Graphs

The [Universal Render Pipeline (URP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/) is one of Unity's Scriptable Render Pipeline. It's quick and easy to customize, and lets you create optimized graphics across a wide range of platforms.

If you have URP installed in your project with Shader Graph, you can create the Shader Graph Assets described in the following table. They have URP configured as an Active Target in the Graph Inspector:

<table>
<thead>
<tr>
<th><strong>Asset Type</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Lit Shader Graph</strong></td>
<td>The URP's Lit Shader Graph lets you create real-world surfaces like stone, wood, glass, plastic, and metals in photo-realistic quality. You can use the Lit Shader Graph for any material that interacts with the lighting in your scene. If you select the URP Lit Shader Graph, it adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: Position, Normal, and Tangent nodes.</li>
<li><strong>Fragment</strong>: Base Color, Normal, Metallic, Smoothness, Emission, and Ambient Occlusion nodes.</li>
</ul></td>
</tr>
<tr>
<td><strong>Decal Shader Graph</strong></td>
<td>The Decal Shader Graph works with the URP's Decal Renderer Feature to project a material onto other objects in a Scene. You can use a Decal Shader Graph for any dynamic effect you want to project onto another surface in your scene. For more information on the Decal Renderer Feature, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/renderer-feature-decal.html">the URP's documentation on the Decal Renderer Feature</a>. <br/> The Decal Shader Graph adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: No nodes are added by default.</li>
<li><strong>Fragment</strong>: Base Color, Alpha, Normal, and Normal Alpha nodes.</li>
<ul><li>Metallic, Ambient Occlusion, Smoothness, and MAOS Alpha are added but inactive.</li></ul>
</ul></td>
</tr>
<tr>
<td><strong>Unlit Shader Graph</strong></td>
<td>The Unlit Shader Graph is for effects or unique objects that don't require lighting. It adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: Position, Normal, and Tangent nodes.</li>
<li><strong>Fragment</strong>: Base Color node.</li>
</ul>
</td>
</tr>
<tr>
<td><strong>Sprite Custom Lit Shader Graph</strong></td>
<td>The Sprite Custom List Shader Graph is specific to URP's 2D graphics features. Instead of using Unity's lighting system, you can add your own custom lighting calculations to the Sprite Custom Lit Shader Graph. It adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: Position, Normal, and Tangent nodes.</li>
<li><strong>Fragment</strong>: Base Color, Sprite Mask, Normal, and Alpha nodes.</li>
</ul>
</td>
</tr>
<tr>
<td><strong>Sprite Unlit Shader Graph</strong></td>
<td>The Sprite Unlit Shader Graph is specific to URP's 2D graphics features and is for effects or unique objects that don't require lighting. It adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: Position, Normal, and Tangent nodes.</li>
<li><strong>Fragment</strong>: Base Color and Alpha nodes.</li>
</ul>
</td>
</tr>
<tr>
<td><strong>Sprite Lit Shader Graph</strong></td>
<td>The Sprite Lit Shader Graph is specific to URP's 2D graphics features and is for any 2D material that needs to interact with the lighting in your scene. It adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: Position, Normal, and Tangent nodes.</li>
<li><strong>Fragment</strong>: Base Color, Sprite Mask, Normal, and Alpha nodes.</li>
</ul></td>
</tr>
</tbody>
</table>


## Built-In Render Pipeline Shader Graphs

The Built-In Render Pipeline is the default render pipeline in the Unity Editor. It's a general-purpose renderer that has limited options for customization. For more information about using the Built-In Render Pipeline, see [Using the Built-In Render Pipeline](https://docs.unity3d.com/Manual/built-in-render-pipeline.html) in the Unity User Manual.

The following Shader Graphs have the Built-In Render Pipeline configured as an Active Target in the Graph Inspector:

<table>
<thead>
<tr>
<th><strong>Asset Type</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Lit Shader Graph</strong></td>
<td>The Lit Shader Graph is for any material that needs to interact with lighting in your scene. It adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: Position, Normal, and Tangent nodes.</li>
<li><strong>Fragment</strong>: Base Color, Normal, Metallic, Smoothness, Emission, and Ambient Occlusion nodes.</li>
</ul></td>
</tr>
<tr>
<td><strong>Unlit Shader Graph</strong></td>
<td>The Unlit Shader Graph is for any material that doesn't need lighting. It adds the following Block nodes to your Master Stack by default:
<ul>
<li><strong>Vertex</strong>: Position, Normal, and Tangent nodes.</li>
<li><strong>Fragment</strong>: Base Color node.</li>
</ul>
</td>
</tr>
</tbody>
</table>

## Other Shader Graphs

The following Shader Graphs are available for every render pipeline:

| **Asset Type**         | **Description** |
| :--------------------- | :-------------  |
| **Blank Shader Graph** | The Blank Shader Graph contains no default Block nodes in its Master Stack, and has no Targets pre-configured. You can configure the graph to work with any render pipeline. |
| **Sub Graph**          | The Subgraph is a special Shader Graph type that can be referenced from inside other Shader Graph Assets. It doesn't require a Target configuration. For more information about Subgraphs, see [Subgraph](Sub-graph.md). |
| **VFX Shader Graph**   | The VFX Shader Graph is deprecated, but is designed to work with Unity's VFX system. Use a Universal Render Pipeline (URP) or High Definition Render Pipeline (HDRP) graph instead, and enable **Support VFX Graph** in the Graph Inspector. |
