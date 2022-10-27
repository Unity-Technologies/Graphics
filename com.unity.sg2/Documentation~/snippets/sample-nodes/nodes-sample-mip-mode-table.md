---
title: nodes-sample-mip-mode-table
---

<tr>
<td><strong>LOD</strong></td>
<td>Float</td>
<td>LOD</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>LOD</strong> Input port only displays if <strong>Mip Sampling Mode</strong> is <strong>LOD</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> The specific mip that the node uses when sampling the Texture.</td>
</tr>
<tr>
<td><strong>Bias</strong></td>
<td>Float</td>
<td>Bias</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>Bias</strong> Input port only displays if <strong>Mip Sampling Mode</strong> is <strong>Bias</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> If <strong>Use Global Mip Bias</strong> is enabled, Unity adds this Bias amount to the Global Mip Bias for a texture's mip calculation. If <strong>Global Mip Bias</strong> is disabled, Unity uses this Bias amount instead of the Global Mip Bias.</td>
</tr>
<tr>
<td><strong>DDX</strong></td>
<td>Float</td>
<td>DDX</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>DDX</strong> Input port only displays if <strong>Mip Sampling Mode</strong> is <strong>Gradient</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> The specific DDX value to use to calculate the Texture's mip when sampling. For more information on DDX values for mipmaps, see <a href="https://docs.unity3d.com/Documentation/Manual/texture-mipmaps-introduction.html">Mipmaps introduction</a> in the Unity User Manual..</td>
</tr>
<tr>
<td><strong>DDY</strong></td>
<td>Float</td>
<td>DDY</td>
<td><div class="NOTE"><h5>NOTE</h5><p>The <strong>DDY</strong> Input port only displays if <strong>Mip Sampling Mode</strong> is <strong>Gradient</strong>. For more information, see <a href="#additional-node-settings">Additional Node Settings</a>.</p></div> The specific DDY value to use to calculate the Texture's mip when sampling. For more information on DDY values for mipmaps, see <a href="https://docs.unity3d.com/Documentation/Manual/texture-mipmaps-introduction.html">Mipmaps introduction</a> in the Unity User Manual.</td>
</tr>
