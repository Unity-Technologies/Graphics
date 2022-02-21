<tr>
<td></td>
<td>Blending Mode</td>
<td></td>
<td>Specifies the method HDRP uses to blend the color of each pixel of the material with the background pixels. The options are:<br>• Alpha: Uses the Material’s alpha value to change how transparent an object is. 0 is fully transparent. 1 appears fully opaque, but the Material is still rendered during the Transparent render pass. This is useful for visuals that you want to be fully visible but to also fade over time, like clouds.<br>• Additive: Adds the Material’s RGB values to the background color. The alpha channel of the Material modulates the intensity. A value of 0 adds nothing and a value of 1 adds 100% of the Material color to the background color.<br>• Premultiply: Assumes that you have already multiplied the RGB values of the Material by the alpha channel. This gives better results than Alpha blending when filtering images or composing different layers.<br>This property only appears if you set Surface Type to Transparent.</td>
</tr>
