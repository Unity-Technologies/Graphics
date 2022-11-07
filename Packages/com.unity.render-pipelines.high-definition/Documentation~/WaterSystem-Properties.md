# Settings and properties related to the Water System

This page explains the settings and properties you can use to configure the:
* [Water Volume Inspector](#volumeinspector)
* [Water System Volume Override](#watervoloverride)
* [Water System in the Rendering Debugger](#waterrenderdebug)
* [Water System in the HDRP Asset](#waterhdrpasset)


<br/>

## Water Volume Inspector
<a name="volumeinspector"></a>

<a name="additionalproperties"></a>
### Additional properties
To see properties related to <b>Fade</b>, <b>Caustics Intensity</b>, and <b>Caustics Plane Band Distance</b>, you must enable them in <b>Preferences</b> > <b>Core Render Pipeline</b>. Select the <b>Visibility</b> option <b>All Visible</b>. These properties are not visible by default because they are not essential to use the feature, being primarily for performance optimization.

<br/>
<br/>



<table>

<tr>
<td colspan="3">
Water type
</td>
<td rowspan="2">
<b>Property</b>
</td>
<td rowspan="2">
<b>Description</b>
</td>
</tr>


<tr>
<td>
<b>Pool</b>
</td>
<td>
<b>River</b>
</td>
<td>
<b>Ocean, Sea, or Lake</b>
</td>
</tr>


<tr>
<td rowspan="6">
X</td>
<td rowspan="6">
X
</td>
<td rowspan="6">
X
</td>
<td colspan="2">
<b>General</b>
</td>
</tr>

<tr>
<td>
<b>Surface Type</b>
</td>
<td>
Specifies the type of water body that this surface should imitate.
</td>
</tr>


<tr>
<td>
<b>Geometry Type</b>
</td>
<td>
Specifies the shape of the water surface.
The options are:
<ul>
<li><b>Quad</b>: Based on a square.</li>
<li><b>Custom Mesh</b>: Based  on a Mesh you provide. Overrides the vertical position of the vertices to keep the surface of the water consistently level.</li>
<li><b>Infinite</b> (<b>Ocean, Sea, or Lake</b> only): Bounds the water surface with the Global Volume.</li>
</ul>
</td>
</tr>

<tr>
<td>
<b>Script Interactions <a name="scriptinteractions"></a></b>
</td>
<td>
Enable to have HDRP calculate the height of the water simulation on the CPU. Increases visual fidelity but demands more from the CPU. See <a href="WaterSystem-scripting.md">Scripting in the Water System</a> for more information.
</td>
</tr>

<tr>
<td>
- <b>Full Resolution</b>
</td>
<td>
Only available if <b>Script Interactions</b> is active. Enable to have HDRP calculate the CPU simulation at full resolution. Otherwise, HDRP calculates the simulation at half resolution. Full resolution simulations demand more from the CPU.
</td>
</tr>

<tr>
<td>
- <b>Evaluate Ripples</b>
</td>
<td>
Only available if <b>Script Interactions</b> is active. Enable to have HDRP include ripples in the simulation on the CPU. Increases visual fidelity, but demands more from the CPU.
</td>
</tr>

<tr>
<td rowspan="3">
X
</td>
<td rowspan="3">
X
</td>
<td rowspan="3" >
X
</td>
<td colspan="2">
<b>Simulation</b>
</td>
</tr>

<tr>
<td>
<b>Time Multiplier</b>
</td>
<td>
Determines the speed at which HDRP presents the water simulation. Values above 1 increase the simulation speed; values lower than 1 decrease it.
</td>
</tr>


<tr>
<td>
<b>Water Mask<a name="watermask"></a></b>
</td>
<td>
A texture HDRP uses to attenuate or supress <b>Ripple</b> (green channel) and <b>Swell</b> or <b>Agitation</b> (red channel) water frequencies. For more information, see <a href="WaterSystem-decals-masking.md">Decals and masking in the Water System</a>.
</td>
</tr>

<tr>
<td rowspan="6">

</td>
<td rowspan="6">
X
</td>
<td rowspan="6">
X
</td>
<td colspan="2">
<b>River</b> surface types: <b>Agitation</b><br/>
<b>Ocean, Sea, or Lake</b> surface types: <b>Swell</b><br/>
</td>
</tr>

<tr>
<td>
<b>Repetition Size</b>
</td>
<td>
The size of the water <a href="WaterSystem-simulation.md#patchgrid">patch</a> in meters. Higher values result in less visible repetition. Also affects the <b>Maximum Amplitude</b> of <b>Swell</b> or <b>Agitation</b> frequency bands.
</td>
</tr>


<tr>
<td>
<b>Distant Wind Speed</b>
</td>
<td>
Represents the speed of distant wind in kilometers per hour. This property indirectly determines the <b>Maximum Amplitude</b> and shape of the swell in a nonlinear way. Nonlinear means that changes to <b>Distant Wind Speed</b> do not have a proportional effect on swells.
</td>
</tr>


<tr>
<td>
<b>Distant Wind Orientation</b>
</td>
<td>
Represents the orientation of distant wind counterclockwise to the world space X vector. (This vector aligns with the blue handle of the <a href="https://docs.unity3d.com/Manual/PositioningGameObjects.html">Transform</a> Gizmo). Only affects a swell with a <b>Chaos</b> value less than 1.
</td>
</tr>




<tr>
<td>
<b>Chaos</b>
</td>
<td>
Determines how much the <b>Local Wind Orientation</b> affects ripples; values less than 1 increase <b>Local Wind Orientation</b>'s influence. Values more than 1 decrease <b>Local Wind Orientation</b>'s influence.
</td>
</tr>

<tr>
<td>
<b>Current</b>
</td>
<td>
Translates the swell at a constant speed in the given direction.
<ul>
<li><b>Speed</b>: Determines how fast the current moves, measured in kilometers per hour.</li>
<li><b>Orientation</b>: Determines the orientation of the current in degrees relative counterclockwise to the world space X vector. (This vector aligns with the blue handle of the <a href="https://docs.unity3d.com/Manual/PositioningGameObjects.html">Transform</a> Gizmo).</li></ul>
</td>
</tr>


<tr>
<td rowspan="5">
</td>
<td rowspan="5">
X
</td>
<td rowspan="5">
X
</td>
<td colspan="2">
Simulation Band properties
</td>
</tr>

<tr>
<td>
<b>Amplitude Multiplier</b>
</td>
<td>
<b>Amplitude Multiplier</b> (<b>Ocean, Sea, or Lake</b>)<br/>
<ul>
<li><b>First band</b>: The degree to which amplitude attenuates on the first frequency band of the Swell.</li>
<li><b>Second Band</b>: The degree to which amplitude attenuates on the second frequency band of the Swell.</li></UL>

<br/>
<b>Amplitude Multiplier</b> (<b>River</b>)<br/>
A multiplier that determines the degree to which amplitude can attenuate on the Agitation frequency band. For example, if your <b>Amplitude</b> value is 10 meters and you set this property to 0.5, your <b>Agitation</b> is 5 meters high.<br/>


</td>
</tr>

<tr>
<td>
<b>Fade</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. When this option is active, HDRP begins fading the contribution of this frequency band at the distance from the camera that the <b>Range</b> value specifies. This helps minimize distant aliasing artifacts.
</td>
</tr>

<tr>
<td>
- <b>Range</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. The distance from the camera in meters at which HDRP begins to fade the contribution of this frequency band.
</td>
</tr>

<tr>
<td>
<b>Total Amplitude</b>
</td>
<td>
The combined amplitude of all bands.
</td>
</tr>



<tr>
<td rowspan="2">
</td>
<td rowspan="2">

</td>
<td rowspan="2">
X
</td>
<td colspan="2">
Simulation Band property specific to <b>Ocean, Sea, or Lake</b>, appears after <b>Amplitude Mulitplier</b> for each band.
</td>
</tr>

<tr>
<td>
<b>Max Amplitude</b>
</td>
<td>
The amplitude of this band, in meters. This is the sum of the original amplitude and the multiplied amplitude.
</td>
</tr>



<tr>
<td rowspan="7">
X
</td>
<td rowspan="7">
X
</td>
<td rowspan="7">
X
</td>
<td colspan="2"><b>Ripples</b></td>
</tr>

<tr>
<td>
<b>Local Wind Speed</b>
</td>
<td>
Represents the speed of local wind blowing over the water surface in kilometers per hour. This determines the maximum amplitude and shape of ripples indirectly, in a nonlinear way. Nonlinear means that changes to <b>Local Wind Speed</b> do not have a proportional effect on ripples.
</td>
</tr>

<tr>
<td>
<b>Local Wind Orientation</b>
</td>
<td>
Represents the orientation of local wind counterclockwise to the world space X vector. (This vector aligns with the blue handle of the <a href="https://docs.unity3d.com/Manual/PositioningGameObjects.html">Transform</a> Gizmo). Only affects ripples with a <b>Chaos</b> value less than 1.

<b>River</b> and <b>Ocean, Sea, or Lake</b> only: If set to 0, matches the <b>Distant Wind Orientation</b>.


</td>
</tr>

<tr>
<td>
<b>Chaos</b>
</td>
<td>
Determines how much the <b>Local Wind Orientation</b> affects ripples; values below 1 increase <b>Local Wind Orientation</b>'s influence. Values above 1 decrease the influence of <b>Local Wind Orientation</b>.
</td>
</tr>

<tr>
<td>
<b>Current</b>
</td>
<td>
<ul>
<li><B>Pool</B>: Determines the orientation and constant speed of the swells that displace ripples in the pool.
</li>
<li><b>River</b>: Determines the orientation and constant speed of the current that displaces ripples in the river. By default, <b>River</b> <b>Current</b> inherits the <b>Agitation</b> values, but you can also set custom orientation and speed values.</li>
<li><b>Ocean, Sea, or Lake</b>:
Determines the orientation and constant speed of the current that displaces ripples in the river. By default, <b>Ocean, Sea, or Lake</b> <b>Current</b> inherits the <b>Swell</b> values, but you can also set custom orientation and speed values.</li>
<li><b>Speed</b>: Determines how fast the current moves, measured in kilometers per hour.</li>
<li><b>Orientation</b>: Determines the orientation of the current in degrees relative counterclockwise to the world space X vector. (This vector aligns with the blue handle of the <a href="https://docs.unity3d.com/Manual/PositioningGameObjects.html">Transform</a> Gizmo).</li></ul>
</td>
</tr>


<tr>
<td>
<b>Fade</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. When this option is active, HDRP begins fading the contribution of this frequency band at the distance from the camera that corresponds to the <b>Range</b> value in meters. This helps minimize distant aliasing artifacts.
</td>
</tr>

<tr>
<td>
- <b>Range</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. The distance from the camera, in meters, at which HDRP begins to fade the contribution of this frequency band.
</td>
</tr>





<tr>
<td rowspan ="7">

</td>
<td rowspan ="7">
X
</td>
<td rowspan ="7">
X
</td>
<td colspan ="2">
<b>Foam</b>
</td>
</tr>

<tr>
<td>
<b>Simulation Foam Amount</b>
</td>
<td>Determines the amount of surface foam. Higher values generate larger foam patches. The <b>Wind Speed Dimmer</b> configuration determines which <b>Distant Wind Speed</b> values generate foam, and how much; see <a href="WaterSystem-foam.md">Foam in the water system</a>.</td>
</tr>

<tr>
<td>
<b>Simulation Foam Smoothness</b>
</td>
<td>Determines the lifespan of surface foam. Higher values cause foam to persist longer and leave a trail.</td>
</tr>

<tr>
<td>
<b>Texture Tiling</b>
</td>
<td>Determines the tile size of the foam texture, in meters.</td>
</tr>

<tr>
<td>
<b>Custom Texture</b>
</td>
<td>Choose a texture Unity can use to define foam's appearance. If this is <b>None</b>, HDRP uses the default texture.</td>
</tr>

<tr>
<td>
<b>Mask</b>
</td>
<td>Select a texture whose red channel Unity uses to attenuate and suppress foam.</td>
</tr>

<tr>
<td>
<b>Wind Speed Dimmer</b>
</td>
<td>Determines foam intensity. The normalized <b>Distant Wind Speed</b> determines the X axis value. The spline editor configures the Y axis value. See <a href="WaterSystem-foam.md">Foam in the water system</a> for more information.</td>
</tr>



<tr>
<td rowspan="16">
X
</td>
<td rowspan="16">
X
</td>
<td rowspan="16">
X
</td>
<td colspan="2">
<b>Appearance</b>
</td>
</tr>

<tr>
<td>
<b>Custom Material</b>
</td>
<td>
Select a custom material Unity can use to render the water surface. If this is <b>None</b>, Unity uses the default material.
</td>
</tr>


<tr>
<td colspan="2">
<b>Smoothness</b>
</td>
</tr>

<tr>
<td>
<b>Close</b>
</td>
<td>
Determines how detailed the water surface is when closer to the Scene camera than the smoothness <b>Fade Start</b> value.
</td>
</tr>


<tr>
<td>
- <b>Distant</b>
</td>
<td>
Determines how detailed the water surface is when further from the Scene camera than the smoothness <b>Fade Distance</b> value.
</td>
</tr>

<tr>
<td>
<b>Fade Range</b>
</td>
<td>
Specifies the range over which Unity interpolates smoothness from close to distant.<br/>
<ul>
<li><b>Start</b>:  Determines the distance in meters from the Scene camera at which HDRP begins removing detail and interpolating the smoothness value for the water surface. </li>
<li><b>Distance</b>: Determines the distance in meters from the <b>Start</b> point at which the <b>Distant</b> smoothness value takes effect. </li>
</ul>
</td>
</tr>


<tr>
<td colspan="2">
<b>Refraction</b>
</td>
</tr>

<tr>
<td>
<b>Color</b>
</td>
<td>
Determines the color HDRP uses to simulate underwater refraction.
</td>
</tr>

<tr>
<td>
<b>Maximum Distance</b>
</td>
<td>
Determines the maximum distance from the Scene camera Unity renders underwater refraction. Higher values increase the distortion amount.
</td>
</tr>

<tr>
<td>
<b>Absorption Distance</b>
</td>
<td>
Determines how deep into the water the camera can perceive, in meters.
</td>
</tr>



<tr>
<td colspan="2">
<b>Scattering</b>
</td>
</tr>

<tr>
<td>
<b>Color</b>
</td>
<td>
Determines the color that Unity uses to simulate underwater scattering.
</td>
</tr>

<tr>
<td>
<b>Ambient Term</b>
</td>
<td>
Determines the intensity of the <a href="https://docs.unity3d.com/Manual/lighting-ambient-light.html">ambient</a> scattering term.
</td>
</tr>

<tr>
<td>
<b>Height Term</b>
</td>
<td>
Determines the intensity of height-based scattering. The higher the vertical displacement, the more the water receives scattering. You can adjust this for artistic purposes.
</td>
</tr>

<tr>
<td>
<b>Displacement Term</b>
</td>
<td>
Determines the intensity of displacement-based scattering. The larger this value is, the more the water receives scattering.
</td>
</tr>

<tr>
<td>
<b>Direct Light Body Term</b>
</td>
<td>
Determines the intensity of direct light scattering on the bodies of waves.
</td>
</tr>

<tr>
<td>

</td>
<td>
X
</td>
<td>
X
</td>
<td>
<b>Direct Light Tip Term</b>
</td>
<td>
Determines the intensity of direct light scattering on the tips of waves. You can perceive this effect more at grazing angles.
</td>
</tr>


<tr>
<td>
X
</td>
<td>
X
</td>
<td>
X
</td>
<td colspan="2">
<b>Caustics</b>
</td>
</tr>


<tr>
<td rowspan="2">
X
</td>
<td rowspan="2">
X
</td>
<td rowspan="2">
X
</td>
<td>
<b>Caustics</b>
</td>
<td>
Enable to render caustics.
</td>
</tr>

<tr>
<td>
<b>Caustics Resolution</b>
</td>
<td>
The resolution at which Unity renders caustics in the simulation.
</td>
</tr>

<tr>
<td>

</td>
<td>
X
</td>
<td>
X
</td>
<td>
<b>Simulation Band</b>
</td>
<td>
Determines which <b>Simulation Band</b> Unity uses for caustics evaluation. <br/>
For <b>Ocean, Sea, or Lake</b> water surfaces, the Swell simulation determines the first (index 0) and second (index 1) simulation band values. Ripples determine the third band value (index 2).<br/>
The <b>River</b> type has two Simulation Bands, one for Agitation simulation and one for Ripples.
For the <b>Pool</b> type, ripples determine the caustics evaluation. A higher <b>Local Wind Speed</b> value results in larger, looser caustics.
</td>
</tr>



<tr>
<td  rowspan="3">
X
</td>
<td  rowspan="3">
X
</td>
<td  rowspan="3">
X
</td>
<td>
<b>Virtual Plane Distance</b>
</td>
<td>
Determines the distance from the camera at which Unity projects simulated caustics. High values generate sharper caustics but can cause artifacts. The larger the waves are, the further the plane distance should be to obtain sharp caustics.
</td>
</tr>

<tr>
<td>
<b>Caustics Intensity</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. The normalized intensity of underwater caustics.
</td>
</tr>

<tr>
<td>
<b>Caustics Plane Blend Distance</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. The vertical blending distance of the water caustics, in meters from the camera.
</td>
</tr>







<tr>
<td rowspan="5">
X
</td>
<td rowspan="5">
X
</td>
<td rowspan="5">
X
</td>
<td colspan="2">
<b>Underwater</b>
</td>
</tr>




<tr>
<td>
<b>Volume Bounds</b>
</td>
<td>
Specifies the collider Unity uses to determine the volume in which it applies the underwater effect for non-infinite water surfaces.
</td>
</tr>

<tr>
<td>
<b>Volume Priority</b>
</td>
<td>
Determines which surface Unity prioritizes for underwater rendering when multiple water surfaces overlap. Unity renders surfaces with a higher value first.
</td>
</tr>

<tr>
<td>
<b>Transition Size</b>
</td>
<td>
Where the distance between the camera and the water surface is lower than or equal to this value, Unity begins to blend the water surface rendering with the underwater rendering to prevent a sharp cutoff between them.
</td>
</tr>

<tr>
<td>
<b>Absorption Distance Multiplier</b>
</td>
<td>
Determines how far the camera can see underwater. For example, a value of 2.0 means the camera can see twice as far underwater as you can from the water surface.
</td>
</tr>

<tr>
<td rowspan="3">
X
</td>
<td rowspan="3">
X
</td>
<td rowspan="3">
X
</td>
<td colspan="2">
<b>Miscellaneous</b>
</td>
</tr>

<tr>
<td>
<b>Decal Layer Mask</b>
</td>
<td>
Specifies the decal layers that render on the water surface. To use this feature, enable <b>Decal Layers</b> in your <a href="HDRP-Asset.md#decallayers">HDRP Asset</a>.
</td>
</tr>


<tr>
<td>
<b>Light Layer Mask</b>
</td>
<td>
Specifies the light layers that affect the water surface. To use this feature, enable <b>Light Layers</b> in your <a href="HDRP-Asset.md#lightlayers">HDRP Asset</a>.
</td>
</tr>

</table>



<br/>


# Water System Volume Override

<a name="watervoloverride"></a>

To use a Volume Override, you must first add a Volume Profile.

See  <a href="WaterSystem-VolOverride.md">The Water System Volume Override</a> for more information.</br>
<table>

<tr>
<td>
<b>Property</b>
</td>
<td>
<b>Description</b>
</td>
</tr>

<tr>
<td colspan="3">
<b>General</b>
</td>
</tr>


<tr>
<td>
<b>Enable</b>
</td>
<td>
Enable the override to render water surfaces.
</td>
</tr>

<tr>
<td colspan="2">
<b>Level of Detail</b>
</td>
</tr>

<tr>
<td>
<b>Min Grid Size</b>
</td>
<td>
Determines the minimum water grid size in meters. The Grid is the geometry Unity uses to render the water, which is always a square. <b>Min Grid Size</b> determines the size of the central patch for the closest <a href ="https://docs.unity3d.com/Manual/LevelOfDetail.html">Level of Detail</a> (LOD), and the other LOD sizes are mulipliers of <b>Min Grid Size</b>.
</td>
</tr>

<tr>
<td>
<b>Max Grid Size</b>
</td>
<td>
Determines the maximum water grid size in meters.
</td>
</tr>

<tr>
<td>
<b>Elevation Transition</b>
</td>
<td>
Determines the elevation at which Unity reaches the maximum water grid size.
</td>
</tr>

<tr>
<td>
<b>Num Level of Details</b>
</td>
<td>
The Patch is the size of the area on which Unity runs the simulation for a particular Simulation Band. Determines the number of LOD patches that Unity renders for water. One level of detail (LOD) means one patch represents the water surface. Two LOD levels means that 8 patches surround the the central patch, three LODs means 16 patches around the central one, and this formula remains the same as the LOD level increases.
</td>
</tr>

<tr>
<td colspan ="2">
Tessellation<br/>
(<a href="#additionalproperties">Additional property set</a>)<br/>
</td>
</tr>

<tr>
<td>
<b>Max Tessellation Factor</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>.  The maximum <a href="Tessellation.md">tessellation</a> factor for the water surface.
Determines how many subdivisions the water surface can have.
</td>
</tr>

<tr>
<td>
<b>Tessellation Factor Fade Start</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. Determines the distance from the camera at which the tessellation factor begins to decrease.
</td>
</tr>

<tr>
<td>
<b>Tessellation Factor Fade Range</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>.  Determines the distance from the camera at which the tessellation factor reaches 0.
</td>
</tr>



<tr>
<td colspan="2">
<b>Lighting</b>
</td>
</tr>

<tr>
<td>
<b>Ambient Probe Dimmer</b>
</td>
<td>
Determines the influence of the <a href="https://docs.unity3d.com/2022.2/Documentation/ScriptReference/RenderSettings-ambientProbe.html">ambient light probe</a> on the water surface.
</td>
</tr>

</table>


<br/>

# Water System in the Rendering Debugger


<a name="waterrenderdebug"></a>

The **Main Camera** and **Scene Camera Rendering** tabs of the [Rendering Debugger](Render-Pipeline-Debug-Window.md) window include **Water** among their frame settings.

<br/>
<a name="waterhdrpasset"></a>

# Water System in the HDRP Asset

You enable the Water System in the [HDRP Asset](HDRP-Asset.md#water) as the [Use the Water System in your Project](WaterSystem-use.md) describes. You can also adjust several related settings in the HDRP Asset.




