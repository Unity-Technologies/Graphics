# Settings and properties related to the water system

This page explains the settings and properties you can use to configure the:
* [Water Volume Inspector](#volumeinspector)
* [Water system volume override](#watervoloverride)
* [Water system in the rendering debugger](#waterrenderdebug)
* [Water system in the HDRP asset](#waterhdrpasset)


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
<td rowspan="10">
X</td>
<td rowspan="10">
X</td>
<td rowspan="10">
X</td>
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
<li><b>Instanced Quads</b>: Creates a finite water surface with multiple instanced grids to keep a higher vertex density.</li>
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
Enable to have the ability to query the water surface position and current direction from the simulation. Refer to <a href="water-scripting-in-the-water-system.md">Scripting in the water system</a> for more information.
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
<td>
<b><a name="tessellation"></a>Tessellation</b>
</td>
<td>
Enable to implement tessellation.
</td>
</tr>

<tr>
<td>
- <b>Max Tessellation Factor</b>
</td>
<td>
Set the level of detail HDRP applies to the surface geometry relative to the camera's position. A higher maximum tessellation factor makes the water surface more detailed and responsive to waves but increases the computational load.
</td>
</tr>

<tr>
<td>
- <b>Tessellation Factor Fade Start</b>
</td>
<td>
Set the distance from the camera where the tessellation detail begins to decrease.
</td>
</tr>

<tr>
<td>
- <b>Tessellation Factor Fade Range</b>
</td>
<td>
Set the distance from the camera at which the tessellation factor reaches 0.
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
<b><a name="watermask"></a>Water Mask</b>
</td>
<td>
Set the texture HDRP uses to reduce or stop water frequencies depending on the water surface type.<br/><ul> <li><b>Ocean:</b> Reduces swell (red channel), agitation (green), and ripples (blue).</li> <li><b>River:</b> Reduces agitation (red channel) and ripples (green channel).</li> <li><b>Pool:</b> Reduces ripples (red channel).</li></ul>The Water Mask reduces the intensity of these water effects by multiplying the mask values with the corresponding water properties in the shader. Darker areas (closer to black) reduce the intensity, while lighter areas (closer to white) increase it.<br/>For more information, refer to <a href="water-decals-and-masking-in-the-water-system.html">Decals and masking in the Water System</a>.
</td>
</tr>

<tr>
<td rowspan="5">
X
</td>
<td rowspan="5">
X
</td>
<td rowspan="5" >
X
</td>
<td colspan="2">
<b>Water Decals</b>
</td>
</tr>

<tr>
<td>
<b>Region Size</b>
</td>
<td>
Set the width and length in meters of the region HDRP applies the Water Decal to.
</td>
</tr>


<tr>
<td>
<b>Region Anchor</b>
</td>
<td>
Anchor the Water Decal to a GameObject. By default, the region follows the camera. To make the region static, anchor it to the water surface.
</td>
</tr>


<tr>
<td>
<b>Deformation</b>
</td>
<td>
Enable to activate the option for creating a deformation decal.
</td>
</tr>


<tr>
<td>
<b>Foam</b>
</td>
<td>
Enable to activate the option for creating a foam decal.
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
The size of the water <a href="water-water-system-simulation.html#patchgrid">patch</a> in meters. Higher values result in less visible repetition. Also affects the <b>Maximum Amplitude</b> of <b>Swell</b> or <b>Agitation</b> simulation bands.
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
<b>Amplitude Dimmer</b>
</td>
<td>
<b>Amplitude Dimmer</b> (<b>Ocean, Sea, or Lake</b>)<br/>
<ul>
<li><b>First band</b>: The degree to which amplitude reduces on the first simulation band of the Swell.</li>
<li><b>Second Band</b>: The degree to which amplitude reduces on the second simulation band of the Swell.</li></UL>

<br/>
<b>Amplitude Dimmer</b> (<b>River</b>)<br/>
A dimmer that determines the degree to which amplitude can reduce on the Agitation simulation band. For example, if your <b>Amplitude</b> value is 10 meters and you set this property to 0.5, your <b>Agitation</b> is 5 meters high.<br/>


</td>
</tr>

<tr>
<td>
<b>Fade</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. When this option is active, HDRP begins fading the contribution of this simulation band at the distance from the camera that the <b>Range</b> value specifies. This helps minimize distant aliasing artifacts.
</td>
</tr>

<tr>
<td>
- <b>Range</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. The distance from the camera in meters at which HDRP begins to fade the contribution of this simulation band.
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
<a href="#additionalproperties">Additional property</a>. When this option is active, HDRP begins fading the contribution of this simulation band at the distance from the camera that corresponds to the <b>Range</b> value in meters. This helps minimize distant aliasing artifacts.
</td>
</tr>

<tr>
<td>
- <b>Range</b>
</td>
<td>
<a href="#additionalproperties">Additional property</a>. The distance from the camera, in meters, at which HDRP begins to fade the contribution of this simulation band.
</td>
</tr>


<tr>
<td rowspan ="5">
x
</td>
<td rowspan ="5">
X
</td>
<td rowspan ="5">
X
</td>
<td colspan ="2"><b>Deformation </b></td>
</tr>

<tr>
<td>
<b>Enable</b>
</td>
<td>Specify if this surface supports deformation.</a>.</td>
</tr>

<tr>
<td>
<b>Resolution</b>
</td>
<td>The resolution of the deformation texture used to represent the deformation area.
<ul>
<li><b>256 x 256</b>: Set the deformation texture to 256 x 256 pixels.</li>
<li><b>512 x 512</b>: Set the deformation texture to 512 x 512 pixels.</li>
<li><b>1024 x 1024</b>: Set the deformation texture to 1024 x 1024 pixels.</li>
<li><b>2048 x 2048</b>: Set the deformation texture to 2048 x 2048 pixels.</li></ul>
</td>
</tr>

<tr>
<td>
<b>Area Size</b>
</td>
<td>Set the size of the deformation area in meters.</td>
</tr>
<tr>
<td>
<b>Area Offset</b>
</td>
<td>Set the offset of the deformation area in meters</td>
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
<td>Determines the amount of surface foam. Higher values generate larger foam patches. The <b>Wind Speed Dimmer</b> configuration determines which <b>Distant Wind Speed</b> values generate foam, and how much; refer to <a href="water-foam-in-the-water-system.html">Foam in the water system</a>.</td>
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
<td>Select a texture whose red channel Unity uses to reduce or remove foam.</td>
</tr>

<tr>
<td>
<b>Wind Speed Dimmer</b>
</td>
<td>Determines foam intensity. The normalized <b>Distant Wind Speed</b> determines the X axis value. The spline editor configures the Y axis value. Refer to <a href="water-foam-in-the-water-system.html">Foam in the water system</a> for more information.</td>
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
<b>Rendering Layer Mask</b>
</td>
<td>
Specifies the rendering layers that render on the water surface. To use this feature, enable <b>Decal Layers</b> and/or <b>Light Layers</b> in your HDRP Asset</a>.
</td>
</tr>


<tr>
<td>
<b>Debug Mode</b>
</td>
<td>
Specifies the view of the debug mode used for the water surface.
</td>
</tr>

</table>



<br/>


# Water system volume override

<a name="watervoloverride"></a>

To use a Volume Override, you must first add a Volume Profile.

Refer to <a href="water-the-water-system-volume-override.md">The water system Volume Override</a> for more information.</br>
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
<b>State</b>
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
<b>Triangle Size</b>
</td>
<td>
Sets the size of the triangle edge in screen space.
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

# Water system in the rendering debugger


<a name="waterrenderdebug"></a>

The **Main Camera** and **Scene Camera Rendering** tabs of the [Rendering Debugger](rendering-debugger-window-reference.md) window include **Water** among their frame settings.

<br/>
<a name="waterhdrpasset"></a>

# water system in the HDRP Asset

You enable the water system in the [HDRP Asset](HDRP-Asset.md) as the [Use the water system in your Project](water-use-the-water-system-in-your-project.md) describes. You can also adjust several related settings in the HDRP Asset.

