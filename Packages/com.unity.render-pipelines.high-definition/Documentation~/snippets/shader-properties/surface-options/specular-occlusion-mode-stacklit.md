<tr>
<td><strong>Specular Occlusion Mode</strong></td>
<td>N/A</td>
<td>N/A</td>
<td>The mode that HDRP uses to calculate specular occlusion. The options are:<br/>&#8226; <strong>Off</strong>: Disables specular occlusion.<br/>&#8226; <strong>Direct From AO</strong>: Calculates specular occlusion from the ambient occlusion map and the Camera's view vector.<br/>&#8226; <strong>SPTD Integration of Bent AO</strong>: First uses the bent normal with the ambient occlusion value to calculate a general visibility cone. Then, calculates the specular occlusion using a Spherical Pivot Transformed Distribution (SPTD) that is properly integrated against the bent visibility cone.<br/>&#8226; *Custom*: Allows you to specify your own specular occlusion values.</td>
</tr>
