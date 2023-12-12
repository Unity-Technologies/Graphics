<tr>
<td><strong>Enable GPU Instancing</strong></td>
<td>Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material in one batch when possible. This makes rendering faster. HDRP cannot render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you cannot <a href="https://docs.unity3d.com/Manual/DrawCallBatching.html">static-batch</a> GameObjects that have an animation based on the object pivot, but the GPU can instance them.</td>
</tr>
