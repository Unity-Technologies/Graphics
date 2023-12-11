<tr>
<td><strong>Add Custom Velocity</strong></td>
<td></td>
<td></td>
<td>Indicates whether HDRP changes the motion vector according to the provided velocity. HDRP adds the provided velocity (the difference between the current frame position and the last frame position in Object space) to the motion vector calculation. This provides correct motion vector calculations for any procedural geometry that HDRP calculates outside of Shader Graph. The motion vector still takes into account other deformations (for example, skinning or vertex animation).</td>
</tr>
