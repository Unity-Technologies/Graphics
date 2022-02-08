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
<td>Enable <strong>Use Global Mip Bias</strong> to use the render pipeline's Global Mip Bias. This bias adjusts the percentage of Texture information taken from a specific mip when sampling. For more information on mip bias, see <a href="Mipmaps-Mip-Bias.md#mip-bias">Mips and mipmaps</a>.</td>
</tr>
<tr>
<td><strong>Mip Sampling Mode</strong></td>
<td>Dropdown</td>
<td>Standard, LOD, Gradient, Bias</td>
<td>Choose the sampling mode that the node should use for calculating the mip level of the Texture:
<br/> <br/>
<ul>
<li><strong>Standard</strong>: The render pipeline calculates and automatically selects the mip for the Texture.</li>
<li><strong>LOD</strong>: The render pipeline lets you set an explicit mip for the Texture on the node. The Texture will always use this mip, regardless of the DDX or DDY calculations between pixels. Setting the Mip Sampling Mode to <strong>LOD</strong> also allows you to connect the node to a Block node in the Vertex Context. For more information on Block nodes and Contexts, see <a href="Master-Stack.md">Master Stack</a>.</li>
<li><strong>Gradient</strong>: The render pipeline lets you set the DDX and DDY values to use for its mip calculation, instead of using the values calculated from the Texture's UV coordinates. For more information on DDX and DDY values, see <a href="Mipmaps-Mip-Bias.md">Mips and mipmaps</a>.</li>
<li><strong>Bias</strong>: The render pipeline lets you set a bias to adjust the calculated mip for a Texture up or down. Negative values bias the mip to a higher resolution. Positive values bias the mip to a lower resolution. The render pipeline can add your value to the value of the Global Mip Bias, or use your value instead of its Global Mip Bias. For more information on mip bias, see <a href="Mipmaps-Mip-Bias.md#mip-bias">Mips and mipmaps</a>.</li>
</ul>
</td>
</tr>
</tbody>
</table>
