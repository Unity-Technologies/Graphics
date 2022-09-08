---
title: nodes-sample-mip-bias-sample-mode-table.md
---

<table>
<thead>
<tr>
<th><strong>Name</strong></th>
<th><strong>Type</strong></th>
<th colspan="2"><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td rowspan="3"><strong>Use Global Mip Bias</strong></td>
<td rowspan="3">Toggle</td>
<td colspan="2">Enable <strong>Use Global Mip Bias</strong> to use the render pipeline's Global Mip Bias. This bias adjusts the percentage of texture information taken from a specific mip when sampling. For more information on mip bias, see <a href="https://docs.unity3d.com/Documentation/Manual/texture-mipmaps-introduction.html">Mipmaps introduction</a> in the Unity User Manual.</td>
</tr>
<tr>
<td><strong>Enabled</strong></td>
<td>Shader Graph uses the render pipeline's Global Mip Bias to adjust the texture information taken when sampling.</td>
</tr>
<tr>
<td><strong>Disabled</strong></td>
<td>Shader Graph doesn't use the render pipeline's Global Mip Bias to adjust texture information when sampling.</td>
</tr>
<tr>
<td rowspan="5"><strong>Mip Sampling Mode</strong></td>
<td rowspan="5">Dropdown</td>
<td colspan="2">Choose the sampling mode to use to calculate the mip level of the texture.</td>
</tr>
<tr>
<td><strong>Standard</strong></td>
<td>The render pipeline calculates and automatically selects the mip for the texture.</td>
</tr>
<tr>
<td><strong>LOD</strong></td>
<td>The render pipeline lets you set an explicit mip for the texture on the node. The texture will always use this mip, regardless of the DDX or DDY calculations between pixels. Set the Mip Sampling Mode to <strong>LOD</strong> to connect the node to a Block node in the Vertex Context. For more information on Block nodes and Contexts, see <a href="Master-Stack.md">Master Stack</a>.</td>
</tr>
<tr>
<td><strong>Gradient</strong></td>
<td>The render pipeline lets you set the DDX and DDY values to use for its mip calculation, instead of using the values calculated from the texture's UV coordinates. For more information on DDX and DDY values, see <a href="https://docs.unity3d.com/Documentation/Manual/texture-mipmaps-introduction.html">Mipmaps introduction</a> in the User Manual.</td>
</tr>
<tr>
<td><strong>Bias</strong></td>
<td>The render pipeline lets you set a bias to adjust the calculated mip for a texture up or down. Negative values bias the mip to a higher resolution. Positive values bias the mip to a lower resolution. The render pipeline can add this value to the value of the Global Mip Bias, or use this value instead of its Global Mip Bias. For more information on mip bias, see <a href="https://docs.unity3d.com/Documentation/Manual/texture-mipmaps-introduction.html">Mipmaps introduction</a> in the User Manual.</td>
</tr>
</tbody>
</table>
