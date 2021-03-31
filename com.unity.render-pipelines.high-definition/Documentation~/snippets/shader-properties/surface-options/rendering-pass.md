<tr>
<td>- **Rendering Pass**</td>
<td>Specifies the rendering pass that HDRP processes this material in. <br />&#8226; **Before Refraction**: Draws the GameObject before the refraction pass. This means that HDRP includes this Material when it processes refraction. To expose this option, select **Transparent** from the **Surface Type** drop-down.<br />&#8226; **Default**: Draws the GameObject in the default opaque or transparent rendering pass pass, depending on the **Surface Type**.<br />&#8226; **Low Resolution**: Draws the GameObject in half resolution after the **Default** pass.<br />&#8226; **After post-process**: For [Unlit Materials](../../../Unlit-Shader.md) only. Draws the GameObject after all post-processing effects.</td>
</tr>
