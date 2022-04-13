---
title: nodes-sample-mip-bias-sample-mode-table.md
---

<table>
<thead>
<tr>
<th><strong>Name</strong></th>
<th><strong>Type</strong></th>
<th><strong>Options</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Use Global Mip Bias</strong></td>
<td>Toggle</td>
<td>Enabled, Disabled</td>
<td>Enable <strong>Use Global Mip Bias</strong> to use the Global Mip Bias from the render pipeline. The bias adjusts the percentage of texture information taken from one mip over another. For more information on mip bias, see <a href="Mipmaps-Mip-Bias.md#mip-bias">Mips and mipmaps</a>.</td>
</tr>
<tr>
<td><strong>Mip Sampling Mode</strong></td>
<td>Dropdown</td>
<td>Standard, LOD, Gradient, Bias</td>
<td>Choose the sampling mode that the Sample Texture 2D Array node should use for calculating the mip level of the texture:
<br/>
<ul>
<li><strong>Standard</strong>: The mip is calculated and selected automatically for the texture.</li>
<li><strong>LOD</strong>: Set an explicit mip for the texture. The texture will always use this mip, regardless of the DDX or DDY calculations between pixels. Setting the Mip Sampling Mode to <strong>LOD</strong> also allows you to connect the node to a Block node in the Vertex Context. For more information on Block nodes and Contexts, see <a href="Master-Stack.md">Master Stack</a>.</li>
<li><strong>Gradient</strong>: Set the DDX and DDY values to use for the texture's mip calculation, instead of using the values calculated from the texture's UV coordinates. For more information on DDX and DDY values, see <a href="Mipmaps-Mip-Bias.md">Mips and mipmaps</a>.</li>
<li><strong>Bias</strong>: Set a bias to adjust the calculated mip for a texture up or down. Negative values bias the mip to a higher resolution. Positive values bias the mip to a lower resolution. This bias value can be added to the value of the Global Mip Bias, or used instead of the Global Mip Bias. For more information on mip bias, see <a href="Mipmaps-Mip-Bias.md#mip-bias">Mips and mipmaps</a>.</li>
</ul>
</td>
</tr>
</tbody>
</table>
