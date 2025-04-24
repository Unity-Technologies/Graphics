# Screen Space Global Illumination (SSGI) reference

[!include[](Snippets/Volume-Override-Enable-Properties.md)]

### Screen-space

<table>
<thead>
  <tr>
    <th><strong>Property</strong></th>
    <th><strong>Sub-property</strong></th>
    <th><strong>Description</strong></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>State</strong></td>
    <td>N/A</td>
    <td>When set to <strong>Enabled</strong>, HDRP processes SSGI for Cameras in the influence of this effect's Volume.</td>
  </tr>
  <tr>
    <td><strong>Tracing</strong></td>
    <td>N/A</td>
    <td>Specifies the method HDRP uses to calculate global illumination. Depending on the option you select, the properties visible in the Inspector change. For more information on what the options do, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#tracing-modes">Tracing Modes</a>. The options are:<br>• <strong>Ray Marching</strong>: Uses a screen-space ray marching solution to calculate global illumination. For the list of properties this option exposes, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#screen-space">Screen Space</a>.<br>• <strong>Ray Tracing</strong>: Uses ray tracing to calculate global illumination. For information on ray-traced global illumination, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Traced-Global -Illumination.html">Ray-traced Global Illumination</a>. For the list of properties this option exposes, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#ray-traced">Ray-traced</a>.<br>• <strong>Mixed</strong>: Uses a combination of ray tracing and ray marching to calculate global illumination. For the list of properties this option exposes, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#ray-traced">Ray-traced</a>.</td>
  </tr>
  <tr>
    <td><strong>Quality</strong></td>
    <td>N/A</td>
    <td>Specifies the overall quality of the effect. The higher the quality, the more resource-intensive the effect is to process.</td>
  </tr>
  <tr>
    <td><strong>Max Ray Steps</strong></td>
    <td>N/A</td>
    <td>The number of ray steps to use to calculate SSGI. If you set this to a higher value, the quality of the effect improves, however it's more resource intensive to process.</td>
  </tr>
  <tr>
    <td><strong>Denoise</strong></td>
    <td>N/A</td>
    <td>Enables the spatio-temporal filter that HDRP uses to remove noise from the Ray-Traced global illumination.</td>
  </tr>
  <tr>
  <td>N/A</td>
    <td><strong>Half Resolution Denoiser</strong></td>
    <td>Enable this feature to evaluate the spatio-temporal filter in half resolution. This decreases the resource intensity of denoising but reduces quality.</td>
  </tr>
  <tr>
  <td>N/A</td>
    <td><strong>Denoiser Radius</strong></td>
    <td>Set the radius of the spatio-temporal filter.</td>
  </tr>
  <tr>
  <td>N/A</td>
    <td><strong>Second Denoiser Pass</strong></td>
    <td>Enable this feature to process a second denoiser pass. This helps to remove noise from the effect.</td>
  </tr>
  <tr>
    <td><strong>Full Resolution</strong></td>
    <td>N/A</td>
    <td>Enable this feature to increase the ray budget to one ray per pixel, per frame. Disable this feature to decrease the ray budget to one ray per four pixels, per frame.</td>
  </tr>
  <tr>
    <td><strong>Depth Tolerance</strong></td>
    <td>N/A</td>
    <td>Use the slider to control the tolerance when comparing the depth of the GameObjects on screen and the depth buffer. Because the SSR algorithm can not distinguish thin GameObjects from thick ones, this property helps trace rays behind GameObjects. The algorithm applies this property to every GameObject uniformly.</td>
  </tr>
  <tr>
    <td><strong>Ray Miss</strong></td>
    <td>N/A</td>
    <td>Determines what HDRP does when screen space global illumination (SSGI) ray doesn't find an intersection. Choose from one of the following options: <br>•<strong>Reflection probes</strong>: HDRP uses reflection probes in your scene to calculate the missing SSGI intersection.<br>•<strong>Sky</strong>: HDRP uses the sky defined by the current <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Volumes.html">Volume</a> settings to calculate the missing SSGI intersection.<br>•<strong>Both</strong> : HDRP uses both reflection probes and the sky defined by the current <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Volumes.html">Volume</a> settings to calculate the missing SSGI intersection.<br>•<strong>Nothing</strong>: HDRP doesn't calculate indirect lighting when SSGI doesn't find an intersection.<br><br>This property is set to <strong>Both</strong> by default.</td>
  </tr>
</tbody>
</table>

### Ray-traced

<table>
<thead>
  <tr>
    <th><strong>Property</strong></th>
    <th><strong>Sub-property</strong></th>
    <th><strong>Description</strong></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>Ray Miss</strong></td>
    <td>N/A</td>
    <td>Determines what HDRP does when ray-traced global illumination (RTGI) ray doesn't find an intersection. Choose from one of the following options: <br>•<strong>Reflection probes</strong>: HDRP uses reflection probes in your scene to calculate the last RTGI bounce.<br>•<strong>Sky</strong>: HDRP uses the sky defined by the current <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Volumes.html">Volume</a> settings to calculate the last RTGI bounce.<br>•<strong>Both</strong> : HDRP uses both reflection probes and the sky defined by the current <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Volumes.html">Volume</a> settings to calculate the last RTGI bounce.<br>•<strong>Nothing</strong>: HDRP doesn't calculate indirect lighting when RTGI doesn't find an intersection.<br><br>This property is set to <strong>Both</strong> by default.</td>
  </tr>
  <tr>
    <td><strong>Last Bounce</strong></td>
    <td>N/A</td>
    <td>Determines what HDRP does when ray-traced global illumination (RTGI) ray lights the last bounce. Choose from one of the following options: <br>•<strong>Reflection probes</strong>: HDRP uses reflection probes in your scene to calculate the last RTGI bounce.<br>•<strong>Sky</strong>: HDRP uses the sky defined by the current <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Volumes.html">Volume</a> settings to calculate the last RTGI bounce.<br>•<strong>Both</strong> : HDRP uses both reflection probes and the sky defined by the current <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Volumes.html">Volume</a> settings to calculate the last RTGI bounce.<br>•<strong>Nothing</strong>: HDRP doesn't calculate indirect lighting when it evaluates the last bounce.<br><br>This property is set to <strong>Both</strong> by default.</td>
  </tr>
  <tr>
    <td><strong>Tracing</strong></td>
    <td>N/A</td>
    <td>Specifies the method HDRP uses to calculate global illumination. Depending on the option you select, the properties visible in the Inspector change. For more information on what the options do, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#tracing-modes">Tracing Modes</a>. The options are:<br>• <strong>Ray Marching</strong>: Uses a screen-space ray marching solution to calculate global illumination. For the list of properties this option exposes, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#screen-space">Screen Space</a>.<br>• <strong>Ray Tracing</strong>: Uses ray tracing to calculate global illumination. For information on ray-traced reflections, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Traced-Global-Illumination.html">Ray-Traced Global Illumination</a>. For the list of properties this option exposes, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#ray-traced">Ray-traced</a>.<br>• <strong>Mixed</strong>: Uses a combination of ray tracing and ray marching to calculate global illumination. For the list of properties this option exposes, see <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Screen-Space-GI.html#ray-traced">Ray-traced</a>.</td>
  </tr>
  <tr>
    <td><strong>Layer Mask</strong></td>
    <td>N/A</td>
    <td>Defines the layers that HDRP processes this ray-traced effect for.</td>
  </tr>
  <tr>
    <td><strong>Clamp Value</strong></td>
    <td>N/A</td>
    <td>Set a value to control the threshold that HDRP uses to clamp the pre-exposed value. This reduces the range of values and makes the global illumination more stable to denoise, but reduces quality.</td>
  </tr>
  <tr>
    <td><strong>Mode</strong></td>
    <td>N/A</td>
    <td>Defines if HDRP should evaluate the effect in <strong>Performance</strong> or <strong>Quality</strong> mode.<br>This property only appears if you select set <strong>Supported Ray Tracing Mode</strong> in your HDRP Asset to <strong>Both</strong>.</td>
  </tr>
  <tr>
    <td><strong>Quality</strong></td>
    <td>N/A</td>
    <td>Specifies the preset HDRP uses to populate the values of the following nested properties. The options are:<br>• <strong>Low</strong>: A preset that emphasizes performance over quality.<br>• <strong>Medium</strong>: A preset that balances performance and quality.<br>• <strong>High</strong>: A preset that emphasizes quality over performance.<br>• <strong>Custom</strong>: Allows you to override each property individually.<br>This property only appears if you set <strong>Mode</strong> to <strong>Performance</strong>.</td>
  </tr>
  <tr>
    <td><strong>Max Ray Length</strong></td>
    <td>N/A</td>
    <td>Controls the maximal length of rays. The higher this value is, the more resource-intensive ray traced global illumination is.</td>
  </tr>
  <tr>
    <td><strong>Full Resolution</strong></td>
    <td>N/A</td>
    <td>Enable this feature to increase the ray budget to one ray per pixel, per frame. Disable this feature to decrease the ray budget to one ray per four pixels, per frame.<br/>This property only appears if you set <strong>Mode</strong> to <strong>Performance</strong>.</td>
  </tr>
  <tr>
    <td><strong>Sample Count</strong></td>
    <td>N/A</td>
    <td>Controls the number of rays per pixel per frame. Increasing this value increases execution time linearly.<br/>This property only appears if you set <strong>Mode</strong> to <strong>Quality</strong>.</td>
  </tr>
  <tr>
    <td><strong>Bounce Count</strong></td>
    <td>N/A</td>
    <td>Controls the number of bounces that global illumination rays can do. Increasing this value increases execution time exponentially.<br/>This property only appears if you set <strong>Mode</strong> to <strong>Quality</strong>.</td>
  </tr>
  <tr>
    <td><strong>Max Mixed Ray Steps</strong></td>
    <td>N/A</td>
    <td>Sets the maximum number of iterations that the algorithm can execute before it stops trying to find an intersection with a Mesh. For example, if you set the number of iterations to 1000 and the algorithm only needs 10 to find an intersection, the algorithm terminates after 10 iterations. If you set this value too low, the algorithm may terminate too early and abruptly stop global illumination.<br/>This property only appears if you set <strong>Tracing</strong> to <strong>Mixed</strong>.</td>
  </tr>
  <tr>
    <td><strong>Denoise</strong></td>
    <td>N/A</td>
    <td>Enable this to enable the spatio-temporal filter that HDRP uses to remove noise from the Ray-Traced global illumination.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Half Resolution Denoiser</strong></td>
    <td>Enable this feature to evaluate the spatio-temporal filter in half resolution. This decreases the resource intensity of denoising but reduces quality.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Denoiser Radius</strong></td>
    <td>Set the radius of the spatio-temporal filter.</td>
  </tr>
  <tr>
    <td>N/A</td>
    <td><strong>Second Denoiser Pass</strong></td>
    <td>Enable this feature to process a second denoiser pass. This helps to remove noise from the effect.</td>
  </tr>
</tbody>
</table>

### 