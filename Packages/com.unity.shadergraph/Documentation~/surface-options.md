# Modify surface options without changing your graph

## Description

Enable **Allow Material Override** to modify a specific set of properties for Universal Render Pipeline Lit and Unlit Shader Graphs and for Built-In Render Pipeline Shader Graphs in the Material Inspector.



<table>
<tr><td><b>Property</b></td><td><b>URP Lit</b></td><td><b>URP Unlit</b></td><td><b>Built-In Render Pipeline</b></td></tr>

<tr><td><b>Workflow Mode</b></td><td rowspan="2">See the URP documentation for the <a href="https://docs.unity3d.com/Manual/urp/lit-shader">Lit</a> URP Shader.</td><td rowspan="2">Not applicable.</td><td rowspan="2">Not applicable.</td></tr>
<tr><td><b>Receive Shadows</td> </tr>

<tr><td><b>Cast Shadows</b></td><td colspan="2">This property is only exposed if <b>Allow Material Override</b> is enabled for this Shader Graph. Enable this property to make it possible for a GameObject using this shader to cast shadows onto itself and other GameObjects. This corresponds to the <a href="https://docs.unity3d.com/Manual/SL-SubShaderTags.html">SubShader Tag</a> <a href="https://docs.unity3d.com/Manual/SL-SubShaderTags.html">ForceNoShadowCasting</a>.</td><td>Not applicable.</td></tr>

<tr><td><b>Surface Type</b></td><td colspan="2" rowspan="3">See the URP documentation for the <a href="https://docs.unity3d.com/Manual/urp/lit-shader.html">Lit</a> and <a href="https://docs.unity3d.com/Manual/urp/unlit-shader.html">Unlit Shaders</a>.</td><td>In the Built-In Render Pipeline, this feature has the same behavior as in URP. Consult the URP documentation.</td></tr>

<tr><td><b>Render Face</b></td><td>In the Built-In Render Pipeline, this feature has the same behavior as in URP. Consult the URP documentation.</td></tr>


<tr><td><b>Alpha Clipping</b></td><td>In the Built-In Render Pipeline, this feature has the same behavior as in URP. Consult the URP documentation.</td></tr>




<tr><td><b>Depth Write</b></td><td colspan="3">
This property is only exposed if Allow Material Override is enabled for this Shader Graph. <br/>
Use this property to determine whether the GPU writes pixels to the <a href="https://en.wikipedia.org/wiki/Z-buffering">depth buffer</a> when it uses this shader to render geometry.  <br/><br>
Options: <br/>
<ul>
<li><b>Auto</b> (default): Unity writes pixels to the depth buffer for opaque materials, but not for transparent materials. </li>
<li><b>Force Enabled</b> Unity always writes pixels to the depth buffer.</li>
<li><b>Force Disabled</b> Unity never writes pixels to the depth buffer.</li> </ul>
This option's functionality corresponds to the command <a href="https://docs.unity3d.com/Manual/SL-ZWrite.html">ZWrite</a> in <a href="https://docs.unity3d.com/Manual/SL-Reference.html">ShaderLab</a>.  To override this setting in a <a href="https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock.html">RenderStateBlock</a>, set the <a href="https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock-depthState.html">depthState</a>.
</td></tr>

<tr><td><b>Depth Test</b></td><td colspan="3">This property is only exposed if <b>Allow Material Override</b> is enabled for this Shader Graph. <br/>Use this property to set the conditions under which pixels pass or fail depth testing. The GPU does not draw pixels that fail a depth test.  If you choose anything other than <b>LEqual</b> (the default setting for this property), consider also changing the rendering order of this material.  <br/> <br/>Options:
<ul>
<li><b>LEqual</b> (default): Unity draws the pixel, if its depth value is less than or equal to the value on the depth texture. Less: Unity draws pixels of the affected surface if their coordinates are less than the current depth buffer value.  </li>
<li><b>Never</b>: Unity never draws the pixels of the affected surface. </li>
<li><b>Less</b>: Unity draws pixels of the affected surface if their coordinates are less than the current depth buffer value. </li>
<li><b>Greater</b>: Unity draws pixels of the affected surface if their coordinates are greater than the current depth buffer value. </li>
<li><b>GEqual</b>: Unity draws pixels of the affected surface if their coordinates are greater than or equal to the current depth buffer value. </li>
<li><b>Equal</b>: Unity draws pixels of the affected surface if their coordinates are equal to the current depth buffer value. </li>
<li><b>NotEqual</b>: Unity draws pixels of the affected surface if their coordinates are not the same as the current depth buffer value. </li>
<li><b>Always</b>: Unity draws this surface to your screen regardless of its z-coordinate. </li> </ul>
This option's functionality corresponds to the command <a href="https://docs.unity3d.com/Manual/SL-ZTest.html">ZTest</a> in <a href="https://docs.unity3d.com/Manual/SL-Reference.html">ShaderLab</a>.  To override this setting in a <a href="https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock.html">RenderStateBlock</a>, set the <a href="https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock-depthState.html">depthState</a> property.</li>
</td></tr>


<tr><td><b>Support VFX Graph</b></td><td colspan="2">This property is only available if the <a href="https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@12.0/manual/">Visual Effect Graph package</a> is installed. Indicates whether this Shader Graph supports the Visual Effect Graph. If you enable this property, output contexts can use this Shader Graph to render particles. The internal setup that Shader Graph does to support visual effects happens when Unity imports the Shader Graph. This means that if you enable this property, but don't use the Shader Graph in a visual effect, there is no impact on performance. It only affects the Shader Graph import time.</td><td>Not applicable.</td></tr>


</table>


## How to use

To use the Material Override feature:
1. Create a new graph in Shader Graph.
2. Save this graph.
3. Open the [Graph Inspector](Internal-Inspector.md).
4. Set **Active Targets** to **Universal** or **Built In**.
5. In the Graph Inspector’s **Universal** or **Built In** section, enable **Allow Material Override**.
6. Create or select a Material or GameObject which uses your Shader Graph.
7. In the Material Inspector, modify **Surface Options** for the target Material or GameObject.
