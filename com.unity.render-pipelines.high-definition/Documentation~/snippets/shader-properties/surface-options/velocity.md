<tr>
<td>**Add Custom Velocity**</td>
<td>Indicates whether HDRP modifies the motion vector according to the provided velocity. The provided velocity (difference between current frame position and last frame position) in object space is added on top of the motion vector calculation. This allows to have correct motion vector for procedural geometry calculated outside of Shader Graph and the motion vector will still take into account other deformations (Skinning, Vertex Animation...).</td>
</tr>
