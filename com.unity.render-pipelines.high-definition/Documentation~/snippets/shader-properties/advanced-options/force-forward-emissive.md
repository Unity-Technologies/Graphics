<tr>
<td>**Force Forward Emissive**</td>
<td>Indicates whether to render the emissive contribution of this Material in a separate forward pass when the Lit Shader Mode is set to **Both** or **Deferred**. This removes a rendering artifact that makes emissive Materials appear completely black when HDRP processes them in the deferred rendering path when using either Screen Space or Ray-Traced Global Illumination.<br/>Limitation: When Unity performs a separate pass for the Emissive contribution, it also performs an additional DrawCall. This means it uses more resources on your CPU.</td>
</tr>
