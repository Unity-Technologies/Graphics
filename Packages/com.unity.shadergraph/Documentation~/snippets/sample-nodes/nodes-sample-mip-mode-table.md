---
title: nodes-sample-mip-mode-table
---

<tr>
<td><strong>LOD</strong></td>
<td>Float</td>
<td>LOD</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>LOD</strong> Input port only displays if you set <strong>Mip Sampling Mode</strong> to <strong>LOD</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> The specific mip that the node should use when sampling the texture.</td>
</tr>
<tr>
<td><strong>Bias</strong></td>
<td>Float</td>
<td>Bias</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>Bias</strong> Input port only displays if you set <strong>Mip Sampling Mode</strong> to <strong>Bias</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> If <strong>Use Global Mip Bias</strong> is enabled, Unity adds this Bias amount to the Global Mip Bias when calculating the texture's mip. If <strong>Global Mip Bias</strong> is disabled, Unity uses this Bias amount instead of the Global Mip Bias.</td>
</tr>
<tr>
<td><strong>DDX</strong></td>
<td>Float</td>
<td>DDX</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>DDX</strong> Input port only displays if you set <strong>Mip Sampling Mode</strong> to <strong>Gradient</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> The specific DDX value to use for calculating the texture's mip when sampling. For more information on DDX values for mipmaps, see <a href="Mipmaps-Mip-Bias.md">Mips and mipmaps</a>.</td>
</tr>
<tr>
<td><strong>DDY</strong></td>
<td>Float</td>
<td>DDY</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>DDY</strong> Input port only displays if you set <strong>Mip Sampling Mode</strong> to <strong>Gradient</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> The specific DDY value to use for calculating the texture's mip when sampling. For more information on DDY values for mipmaps, see <a href="Mipmaps-Mip-Bias.md">Mips and mipmaps</a>.</td>
</tr>
