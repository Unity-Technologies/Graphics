# Feature Examples

The Shader Graph Feature Examples sample content is a collection of Shader Graph assets that demonstrate how to achieve common techniques and effects in Shader Graph.  The goal of this sample pack is to help users see what is required to achieve specific effects and provide examples to make it easier for users to learn.

The sample content is broken into the following categories:

 - **Blending Masks** - these samples generate masks based on characteristics of the surface - such as height, facing angle, or distance from the camera.
 - **Custom Interpolator** - here we show how to use the Custom Interpolator feature in Shader Graph to move calculations from the fragment stage to the vertex stage to save performance.
 - **Detail Mapping** - techniques for adding additional detail to a surface that isn’t contained in the base set of texture maps.
 - **Procedural Noise and Shapes** - methods for creating shapes or patterns that use math instead of texture samples.
 - **Shader Graph Feature Examples** - examples of using specific Shader Graph features - such as the Custom Code node or the Custom Interpolator
 - **UV Projection** - methods of creating texture coordinates to achieve specific effects such as parallax occlusion mapping, or triplanar projection
 - **Vertex Animation** - techniques for adjusting the position of the vertices to create effects such as waves, animated flags, or camera-facing billboards.
 - **Particles** - shows how a full-featured particle system can be built using just Shader Graph
 - **Conditions** - demonstrates branching based on graphics quality setting and based on the active render pipeline.
 - **Custom Lighting** - shows how Shader Graph can be used to build custom lighting models - including PBR, simple, and cel shading.

## Blend Masks
A major part of creating shaders is determining where specific effects should be applied. This is done by creating a mask and then using the mask to separate areas where the effect should be applied versus where it should not be applied.  This set of samples provides examples of various methods of creating these masks.

#### Altitude Mask
The Altitude Mask is black below the minimum altitude, transitions from black to white between the minimum and maximum altitudes, and then stays white above the maximum altitude. You can use the AltitudeMask subgraph to create this effect.

The Altitude Mask example shows how to use the Altitude Mask subgraph in the shader to blend between two materials. Below the minimum altitude, the cobblestones material is used.  Between minimum and maximum, the materials blend from cobblestones to gold, and then above the maximum, the gold material is used.

You can use the Falloff Type dropdown on the Altitude subgraph node to select the type of blend ramp to use. Linear will make the mask a direct line from minimum to maximum, while Smoothstep will create smooth transitions using a more S shaped curve.

#### Angle Mask
The Angle Mask uses the direction that a surface is facing to determine if the mask should be black or white. If the surface is pointing in the direction of the given input vector, the mask is white.  If it’s pointing away from the given vector, the mask is black.

The Angle Mask example uses the AngleMask subgraph to generate a mask, and then the mask is used to blend between the cobblestones material and a white, snow-like material.

In this example, the AngleMask subgraph node’s MaskVector input is set to 0,1,0 - which is a vector pointing in the up direction (positive Y). When the object’s surface is pointing in that direction, the mask is white.  When the surface is pointing away from that direction, the mask is black.

The Max and Min input values on the AngleMask subgraph are used to control the falloff of the mask. Both values should use numbers between zero and one.  When the Max and Min values are close together (0.5 and 0.48 for example), the falloff will be sharper. When they’re farther apart (0.8 and 0.3 for example), the falloff will be more gradual and blurry. When Max and Min are closer to a value of 1, the surface direction must match the MaskVector much more closely for the mask to be white.  When Max and Min are closer to a value of zero, the mask will be white even with a larger difference between the surface direction and the MaskVector.

#### Camera Distance Mask
The Camera Distance Mask uses the distance from the camera location to the object’s surface to determine if the mask should be black or white. When the camera is close to the surface, the mask is black and when it’s further away, the mask is white.

In this example, we apply the cobblestones material when the camera is close to the object and as the camera moves away we blend to the gold material.

The Start Distance and Length values on the CameraDistanceMask subgraph control how the mask functions.  The Start Distance value controls where the mask starts the transition from black to white.  In this case, it’s set to 2 meters - which means that between 0 and 2 meters, the mask will be black.  The Length value controls how long the transition is between black and white.  In this case, it’s also set to 2 meters, so from a 2 meter distance to a 4 meter distance the mask will transition from black to white.  Any distance beyond 4 meters will create a white mask.
#### Height Mask
The Height Mask uses the material height data of two materials to blend them together, creating a much more realistic looking intersection between them. Instead of fading between two materials, we can apply one material in the cracks and crevices of the other.

In this example, we use U texture coordinate as a smooth gradient mask, and then modify the mask using the heights of the two materials. Instead of a smooth blend, we end up with the gold material being applied in the lower areas of the cobblestones first and then gradually rising until just the tops of the cobblestones show before being replaced completely by the gold.

In order for this effect to work correctly, one or both of the materials need good height data. This type of a transition works best on materials with varying heights - like the cobblestones.  Materials that are mostly flat won’t generate interesting effects with this technique.

## Custom Interpolator
The Custom Interpolator feature in Shader Graph allows you to do any type of calculation in the Vertex Stage and then interpolate the results to be used in the Fragment Stage.  Doing calculations in the Vertex Stage can give a major performance boost since math is only done once per vertex instead of for every pixel.

However, per-vertex calculations can cause artifacts as illustrated in the Artifacts example.  Be careful to only do math with low-frequency variations to avoid these artifacts.

#### Interpolation Artifacts
This example shows the types of artifacts that can occur when we do math in the vertex stage. In order to be smooth, lighting needs to be calculated at each pixel - but here we’re doing the calculations per-vertex instead and then interpolating the results to the pixels.  The interpolation is linear, so we don’t get enough accuracy and the result looks angular and jagged instead of smooth - especially on the specular highlight.
#### Interpolation Savings
This example demonstrates a use case where custom interpolators can be highly beneficial. When creating shaders, it's common to tile and offset UVs multiple times. This behavior can become quite costly, especially when scrolling UVs on a fairly large object. For instance, consider a water shader where the water plane covers most of the terrain. Tiling the UVs in the fragment stage means performing the calculation for every pixel the water plane covers. One way to optimize this is to use custom interpolators to calculate the UVs in the vertex stage first and then pass the data to the fragment stage. Since there are fewer vertices than pixels on the screen, the computational cost will be lower. In this case, unlike the Custom Interpolator NdotL example, we do the samping after so that the rendering results are almost unnoticeable. 

When "InFragStage" is set to "true", the UVs calculated in the fragment stage are used. When "InFragStage" is set to "false", the UVs are scrolled in the vertex stage.
In this case, scrolling UVs in either the vertex or fragment stages won't cause a noticeable difference in the rendering result. However, it's much more cost-friendly to perform the calculations in the vertex stage and pass the data using custom interpolators.


## Detail Mapping
Detail Mapping refers to a set of techniques where additional detail is added to the surface of a model that is not contained in the model’s base set of textures.  These techniques are used when the camera needs to get closer to a model than the resolution of the textures would typically allow, or when the object is so large that the resolution of the base textures is insufficient.

In the three Detail Mapping examples, we’re using a texture packing format for our detail texture called NOS - which is short for Normal, Occlusion, Smoothness.  This indicates that the Normal is stored in the red and green channels of the texture, the ambient occlusion is stored in the blue channel, and the smoothness is stored in the alpha channel. Packing the data together in this way allows us to get maximum use from our single detail texture - and we can add detail to the color, normal, smoothness, and AO with just a single texture sample. To simplify the process, we use the UnpackDetailNOS subgraph to sample the NOS detail texture and unpack the data.
#### Detail Map Color 
For the Color Detail Map example, we simply multiply the Albedo output of the UnpackDetailNOS subgraph with the base color texture. The NOS texture format stores ambient occlusion data in the blue channel of the detail texture - and this occlusion data is what the UnpackDetailNOS subgraph passes to the Albedo output port. So for color detail, we’re just multiplying our base color by the detail AO.

Notice that the effect is quite subtle.  In the past, when detail mapping was a new technique, most surfaces used only color textures - so color detail mapping was the main technique used.  Now that materials use normals, smoothness, occlusion, etc, it works much better to use detail mapping for all of the maps instead of just the color.
#### Detail Map Normal 
For the Detail Map Normal example we combine the Normal output of the UnpackDetailNOS subgraph with the base normal map.  Here we’re using the Normal Blend node to combine the two normals together and we have the Reoriented mode selected for best quality.

Notice that the Detail Map Normal example is significantly more impactful than the Detail Map Color example.  The detail normals are changing the perceived shape of the surface - which has a very strong effect on the lighting, whereas the Color Detail example is only changing the color - which is less effective. This indicates that if you can only apply detail to one of the textures in your material, the normal is the most effective one to add detail to.

If you wanted to make this effect slightly cheaper to render, you could set the Normal Blend node to the Default mode instead (which uses the Whiteout normal blend technique instead).  For another optimization, you could also set the Quality dropdown on the UnpackDetailNOS subgraph to Fast instead of Accurate.  Both of these optimizations together would reduce the number of instructions required to render the effect.  The results would be slightly less accurate, but this might not be noticeable. Try it out in your own shaders and see.  If you can’t tell the difference, use the cheaper techniques for better performance!
#### Detail Map Full
The Detail Map Full example adds detail to the color, normal, ambient occlusion, and smoothness components of the material - so we’re adding additional detail to almost all of the material components.  This is the most effective way to add detail to a surface, but also a little more expensive than the Color or Normal examples.

Take special note of the way that we’re blending each of the outputs of the UnpackDetailNOS subgraph with their counterparts in the main material.  For color, we’re using Multiply - so the detail color data darkens the base color.  For Normal, we’re using the Normal Blend node.  For Ambient Occlusion, we’re using the Minimum node, so the result is whichever result is darker.  We’re using the Minimum node instead of Multiply to prevent the ambient occlusion from getting too dark. And finally, for the smoothness, we’re using Add.  This is because the Smoothness data is in the -0.5 to 0.5 range and adding this range to the base smoothness acts like an overlay with darks darkening and brights brightening.

## Procedural Noise and Shapes
In shaders, the term “procedural” refers to techniques that generate shapes and patterns using a series of math formulas - computed in real-time - instead of sampling texture maps. Procedural noise patterns and shapes have various advantages over texture maps including using no texture memory, covering infinite surface area without repetition or tiling, and being independent of texture resolution.
#### Hex Grid
A Hexagon grid is useful for all types of projects so we decided to include one in the examples. In the example the Grid output port of the HexGrid subgraph is simply connected to the Base Color of the Master Stack. The HexGrid subgraph also has an EdgeDistance output port and a TileID output port.  The Edge Distance output provides a signed distance field value that represents the distance to the nearest hexagon edge. So the pixels right in the center of each hexagon are white and then the closer you get to hexagon edges, the darker the pixels become.  The TileID output port provides a different random value for each tile.

The HexGrid subgraph node also has some useful input ports.  The UV input allows you to control the UV coordinates that are used to generate the pattern.  The Scale input gives you control over the dimensions of the effect on both the X and Y axis.  Finally, the Line Width input controls the thickness of the grid outlines.  Line Width only applied to the Grid output and does not change the output of Edge Distance or TileID.
#### Procedural Brick
The Procedural Brick example shows how a brick pattern can be generated using pure math and no texture samples.  If you’re developing on a platform that is fast at doing math but slow at texture samples, sometimes it can be much more performant to generate patterns like bricks procedurally rather than by sampling textures.  Another advantage is that the variations in the brick patterns don’t repeat - so if you need a pattern to cover a large area without any repetition, procedural generation might be your best option.
#### SDF Shapes
Shader Graph comes with a set of nodes for creating shapes procedurally (Ellipse, Polygon, Rectangle, Rounded Rectangle, Rounded Polygon) but frequently developers find that a signed distance field of the shape is more useful than the shape itself. SDFs can be joined together in interesting ways and give developers more flexibility and control in generating results than just having the shape itself.  This is why we’ve included these SDF shapes in the examples.

## Shader Graph Feature Examples
The Shader Graph tool has several more advanced features - such as port defaults and Custom Interpolators - that can be tricky to set up.  This section contains examples for those features to help users know what is required to set up and use these more advanced features.
#### Subgraph Dropdown
When creating a subgraph node, it’s possible to add a dropdown control to allow users to make a selection. This is useful when there are several different methods of achieving a similar result and you want to allow the user of the subgraph to select which method to use.  This example illustrates how to add a dropdown box to your subgraph.

After creating your subgraph asset, open it in the editor and open the Blackboard panel. Click the plus icon at the top and select Dropdown at the bottom of the list of parameter types. Now give your dropdown a name.  Once the dropdown parameter is named, select it and open the Graph Inspector. Here you can control the number and names of the options in the dropdown list. Use the plus and minus icons at the bottom of the list to add and remove items. And click on individual items in the list to change their names. Once you have the number of items you want in the list, go back to the Blackboard and drag and drop your Blackboard parameter into the graph.  It will appear as a node with input ports to match the list items you created in the Graph Inspector. For each of the input ports, create a graph that generates the results that you want for that option. The dropdown node acts as a switch to switch between the inputs depending on what is selected with the dropdown.

When the subgraph is added to a graph, the user will be able to select an option from the dropdown and the graph will use the branch of the graph that has been selected.  Since the branch is static, only the selected branch will be included in the shader at runtime so no additional shader variants will be generated and no performance penalty will be incurred.
#### Subgraph Port Defaults
You can use the Branch on Input Connection node to set up defaults for the input ports of your subgraphs and even create large graph trees to use if nothing is connected to a specific input port.  The Subgraph Port Defaults example shows how to do that.

After creating your subgraph asset, open it in the editor and open the Blackboard panel. Click the plus icon at the top and select a data type.  In our example, we selected a Vector 2 type because we’re making an input port for UV coordinates. Once you’ve selected a type, give your parameter a name.  We named ours UV. Before adding the parameter to your graph, select it and open the Graph Inspector.  In the Graph Inspector, check the “Use Custom Binding” checkbox and give the parameter a Label.  This is the name that will show up as connected to the port when no external wires are connected. Now, drag and drop your parameter from the Blackboard into the graph. Next, hit the spacebar and add a Branch on Input Connection node.  The node can be found in the Searcher under Utilities->Logic. This node will allow you to set up a default input value or graph branch to use when nothing is connected to the input port. Connect your input parameter’s output port to the Input and Connected input ports of the Branch on Input Connection node.  This will allow the input port to function correctly when a wire is connected to it. Now you can connect a node or node tree to the NotConnected input port.  Whatever is connected here will be what gets used when nothing is connected to this subgraph’s input port. In our example, we’ve connected the UV node and used the Swizzle node so only the X and Y coordinates are used. So with this setup, UV0 will be used if nothing is connected to the input port.

## UV Projection
UV coordinates are used to translate the 3d surface area of models into 2d space that can be used to apply texture maps. This section contains a set of examples for creating, manipulating, and applying UV coordinates to achieve many different types of effects.
#### Flipbook
A flipbook is an effect where a series of frames is played back in sequence.  The frames are arranged in a grid pattern on a texture map. Shader Graph has a built-in node that generates the UVs required to jump from one frame to the next on the texture.

In this example, we show how to use Shader Graph’s built-in Flipbook node to create an animated effect. We also show that you can use a pair of Flipbook nodes to set up an effect that blends smoothly from one frame to the next instead of jumping.

Notice that in the Blackboard, we’ve exposed a Texture parameter for selecting a flipbook texture, Rows and Columns parameters to describe the layout of the flipbook texture, a Speed parameter to control the playback rate of the frames, and a Flip Mode dropdown.

With the Flip Mode dropdown, you can select to Flip from one frame to the next, or to Blend between frames. Notice that if you select the Blend option, the playback appears much more smooth even though the frame rate remains the same. Using this Blend mode is a good way to improve the appearance of the effect and make it feel less choppy, even if the frame rate is low.
#### Flow Mapping
Flow Mapping is a technique that creates the illusion of flowing movement in a texture. It’s achieved by warping the texture coordinates along a specific flow direction over time in two separate phases. When the warping of the first phase becomes too severe, we blend to the second phase which is not yet warped and then warp that while removing the warping from the first phase while it is not displayed. We can blend back and forth between the two phases over and over to create the illusion of motion.

In the example, we’re using the UVFlowMap subgraph which does the main work of the effect. We give it a Flow Map - which is the direction to push the movement.  In our case we’ve used a texture (similar to a normal map) to specify the direction. Then we give it a Strength value - which controls the distance that the UVs get warped. Flow Time can be as simple as just the Time node, but you can also connect a Flow Map Time subgraph which varies the time in different areas to break up the strobing effect.  The UV input controls how the texture is applied.  Notice that we’re using a Tiling And Offset node here to tile the texture 8 times. And finally the Offset value controls the midpoint of the stretching effect. The default value of 0.5 means that each of the phases starts out half stretched in the negative direction, moves to unstretched, and then moves to half stretched in the positive direction.  This will give the best results in most cases.

In our example, we have exposed a Temporal Mode dropdown which illustrates the usefulness of the Flow Map Time subgraph.  When Temporal Mode is set to Time Only, we’re only using Time as the Flow Time input.  There is a noticeable strobing effect where the entire model appears to be pulsing in rhythm.  This is because the blending between phase 1 and phase 2 is happening uniformly across the whole surface.  When we set Temporal Mode to Flow Map Time, we’re using the Flow Map Time subgraph as the Flow Time input.  The Flow Map Time subgraph breaks up the phase blending into smooth gradients across the surface so that it’s non-uniform and removes the strobing effect.
#### Interior Cube Mapping
Interior Cube Mapping is a technique that creates the illusion of building interiors as seen through windows where no interior mesh exists.  The effect can be used to make very simple exterior building meshes appear to have complex interiors and is much cheaper than actually modeling interiors.

In our example, the UVInteriorCubemap subgraph generates the direction vector that we need for sampling our cube map. The cube map creates the illusion of the interiors. And then the rest of the graph creates the exterior building and windows.

The UVInteriorCubemap subgraph has inputs for specifying the number of windows and controlling whether or not to randomize the walls of the cube map.  The randomization rotates the walls of the cube map so that each interior has different walls on the sides and back.  There is also a dropdown for controlling whether the projection is happening in object space or in UV space.
#### Lat Long Projection
The Lat Long Projection example demonstrates the math required to use a texture map in the Latitude Longitude format.  Many high dynamic range environment images are stored in this format so it’s useful to know how to use this type of image.  You can tell an image is in LatLong format because it has a 2 to 1 aspect ratio and usually has the horizon running through the middle.

In our example, we’re using the UVLatLong subgraph. This node generates the UV coordinates needed to sample a texture map in the LatLong format.  By default, the UVLatLong subgraph uses a reflection vector as input - so the result acts like a reflection on the surface of your model. But if you wanted the result to be stuck to the surface instead, you could use the Normal Vector.

If you select the Sample Texture 2D node and open the Graph Inspector, notice that the Mip Sampling Mode is set to Gradient.  With that setting, the Sample Texture 2D node has DDX and DDY input ports - which we have connected to the DDX and DDY nodes.  We’re doing this because the texture coordinates generated for the LatLong projection have a hard seam where the left and right sides of the projection wrap around and come together.  If we were to set the Mip Sampling Mode to Standard instead, we would end up with a hard seam where the texture sampler failed because there is a large discontinuity in the mip values of the texture coordinates. The Gradient Mip Sampling Mode allows us to manually calculate our own mip level with the DDX and DDY nodes instead of allowing the sampler to do it.
#### Mat Cap (Sphere Mapping) 
The Mat Cap Material example demonstrates the math required to project a sphere map onto a surface. This effect is often called a Mat Cap - or material capture, because you can represent the properties of a material - like reflections or subsurface scattering - in the way the texture is created.  Some 3d sculpting software uses MatCap projection to render objects.  You can tell that a texture is a sphere map (or MatCap) because it looks a bit like a picture of a chrome ball. Sphere maps are the cheapest form of reflection - both in texture memory and in the low cost of math. But they’re not accurate because they always face the camera.

In our example, we’re using the UVSphereMap subgraph to generate the texture coordinates to sample the sphere map. The subgraph has an input for the surface normal - and you can use the dropdown to select the space that the normal is in.  By default, the Vertex Normal is used, but you could also connect a normal map to it if you wanted to give the surface more detail.
#### Parallax Mapping
There are many techniques that attempt to add more detail to the shape of a surface than is actually represented in the geometry of the surface.  The Parallax Mapping example demonstrates three of these examples, and you can select which example to display with the Bump Type dropdown box in the material.
##### Normal Only
This technique is the cheapest and most common. It uses a normal map to change the apparent shape of the surface - where each pixel in the map represents the direction that the surface is facing at that point. Because there is no offsetting of the surface happening, this technique also looks fairly flat compared to the other two.

##### Parallax
Parallax Mapping samples a height map and then uses the value to offset the UV coordinates based on their height relative to the view direction. This causes parallax motion to occur on the surface and makes the surface feel like it has actual depth.  However, when seen at steep angles, the effect often has artifacts.  Where there are steep changes in the height map, there are visible stretching artifacts.
##### Parallax Occlusion
Parallax Occlusion Mapping samples a height map multiple times (based on the number of Steps) in a path along the view vector and reconstructs the scene depth of the surface. It uses this depth information to derive UV coordinates for sampling the textures. This process is expensive - especially with a high number of Steps, but can be made cheaper by creating a mask to reduce the number of Steps based on the camera distance and angle of the surface. Our example illustrates this technique.
#### Triplanar Projection
Triplanar projection projects a texture onto the surface of a model from the front, side, and top.  It’s useful when you want to apply a texture but the model doesn’t have good UV coordinates, or when the UVs are laid out for something else.  It’s also useful when you want to project the same texture or material on many objects in close proximity and have the projection be continuous across them.

There are several methods for projecting a texture onto a surface.  Our example shows four of them and you can select the method you want to see using the Projection Type dropdown in the material.  They’re in order from most expensive to least.

##### Triplanar Texture projection
The Triplanar Textures technique uses the built-in Triplanar node in Shader Graph.  This node samples each of your textures three times - for the top, front, and side projections and then blends between the three samples.  This technique is the nicest looking since it blends between the samples, but it’s also the most expensive.

##### Biplanar Texture projection
This is a clever optimization to triplanar projection that uses two texture samples instead of three. The shader figures out which two faces are most important to the projection and only samples those two instead of all three. On most platforms, it will be cheaper than Triplanar Textures, but more expensive than Triplanar UVs. Depending on the textures you’re sampling, you may notice a small singularity artifact at the corner where the three faces come together.

##### Triplanar UV projection
The Triplanar UVs technique uses the UVTriplanar subgraph to project the UV coordinates from the top front and side and then use those to sample the textures only one time instead of three. Because it’s only sampling each texture one time, this technique is cheaper. The UV coordinates can’t be blended like the textures can - so this technique has hard seams where projections come together instead of blending like the Triplanar Textures technique.  However, these seams aren’t very noticeable on some materials, so this may be an acceptable alternative if you need to do triplanar projection more cheaply.

Notice that the normal map needs to be plugged into the UVTriplanarNormalTransform node when using this technique in order to get the normals transformed correctly.

##### UV
This option just applies the textures using standard UV coordinates.  It’s here so that you can easily compare it with the other two techniques.

## Vertex Animation
Generally, we think of shaders as changing the color and appearance of pixels.  But shaders can also be applied to vertices - to change the position of the points of a mesh. The model’s vertices can be manipulated by a shader to create all sorts of animated effects - as shown in this section.
#### Animated Flag
This example shows a simple method for making a flag that ripples in the wind. The effect centers around the Sine node - which is what creates the rippling motion. We take the X position of the vertices and multiply them by a value that controls the length of the waves. We multiply that by Time and then pass that into the Sine node.  Finally, we multiply the result of the Sine wave with a mask that goes from 0 at the point where the flag attaches to the pole, 1 at the tip of the flat where the effect should be strongest.  The result is a simple rippling flag.

You could add additional detail to this effect if you combined several different sine waves that move at different speeds and wavelengths to vary and randomize the results. But something as simple as this example may be all that is needed if your flag is seen at a distance.

#### Bend Deformer - Grass
This example shows the math required to bend a rectangle-shaped strip in an arc shape without changing its length. This can be used for animating blades of grass. The BendDeformer subgraph adjusts both the position and the normal of the vertices - so we get proper lighting on the updated shape. 
#### Billboard
The billboard example illustrates the math that we use to make a flat plane face the camera.  Notice that the Billboard subgraph has a dropdown that allows us to select the initial direction that our plane is facing. Selecting the correct option here will ensure that our plane turns toward the camera correctly and not some other direction.
#### Gerstner Wave
In this example, we use several instances of the GerstnerWave subgraph to animate waves in our mesh.  The GerstnerWave subgraph does the math required to realistically simulate the movement of a single wave. Notice that each of the three instances has a different direction, wave length, and wave height.  Combining these three different wave sizes together creates a really nice-looking wave simulation.  The Offset values are added together and then added to the Position.  The normals are combined using the Normal Blend node and then used directly as the Normal.

## Particles
This example shows that it’s possible to create a simple particle system using nothing but Shader Graph! This method of creating particles is cheap because it’s done 100% on the GPU and almost all of the shader work happens in the vertex shader.  This shader is not intended as a replacement for any of the other particle systems in Unity, but simply as an illustration of what’s possible to do with just Shader Graph alone. It could potentially be cheaper to make simple particle effects using this shader than with other methods.  It’s definitely not as powerful or as full-featured as something made with VFX Graph, for example.

We start by creating a stack of planes where each plane in the stack has a slightly different vertex color. We use this value as an ID to differentiate each plane in the stack. This sample set comes with 3 stacks of planes that can be used.  One with 25 planes, one with 50 planes, and one with 100 planes.  

> Note that most particle systems dynamically generate particles based on the number that the system needs, but we’re using static geometry, so we’re locked in to using the number of planes in the geometry we choose. If the system we create with the material parameters requires more particles, the only way to fix it is to swap out the mesh that we’re using - so this is one major downside to this method.

We use the Billboard subgraph to make all of the planes face the camera and we use the Flipbook node to add an animated effect to the particles. We also add gravity and wind to control the movement of the particles. In the pixel shader, we expose control over the opacity and even fade out particle edges where they intersect with other scene geometry.

Here’s a description of the exposed material parameters that control the appearance and behavior of the particles:

Emitter Dimensions - controls the size of the particle emitter in X,Y, and Z. Particles will be born in random locations within the volume specified by these dimensions.
#### Color
This is a color value that gets multiplied by the FlipbookTexture color. The StartColor blends to the EndColor throughout the lifetime of the particle. The alpha value of the color gets multiplied by the alpha value of the FlipbookTexture to contribute to the opacity of the particles.
**Start Color** - the color multiplier for the particle at the beginning of its life
**EndColor** - the color multiplier for the particle at the end of its life

#### Opacity
These controls change the opacity/transparency behavior of the particles.
**Opacity** - the overall opacity multiplier. Values above one are acceptable and can make subtle particles more visible.
**FadeInPower** - controls the falloff curve of the particle fade-in.
**FadeOutPower** - controls the falloff curve of the particle fade-out.
**SoftEdges** - enables the soft edges feature which fades out the particles where they intersect with scene geometry.
**AlphaClipThreshold** - controls the opacity cut-off below which pixels are discarded and not drawn. The higher this value is, the more pixels can be discarded to reduce particle overdraw.
#### Scale
Controls the size of the particles. Particles transition from the ParticleStartSize to the ParticleEndSize over their lifetime.
**ParticleStartSize** - the size of the particle (in meters) when it is born.
**ParticleEndSize** - the size of the particle (in meters) when it dies.
#### Movement
These controls affect the movement of the particles.
**ConstantFlow** - the smoothness of the flow of the particles. A value of 1 distributes particle flow evenly over time. A value of 0 spawns all of the particles and once right at the beginning of the phase.  Values in between make particle birthrate/flow more random and hitchy.
**ParticleSpeed** - controls the overall speed of the particles
**ParticleDirection** - the main direction of particle movement
**ParticleSpread** - the width of the particle emission cone in degrees. A value of 0 will emit particles in single direction and a value of 360 will emit particles in all directions (a sphere)
**ParticleVelocityStart** - controls how fast the particles are moving when they’re first born.
**ParticleVelocityEnd** - controls how fast the particles are moving when they die.
#### Rotation
Controls the rotation behavior of the particles.
**Rotation** - the static amount of rotation to apply to each particle in degrees.
**RotationRandomOffset** - when checked, applies a random rotation amount to each particle
**RotationSpeed** - the speed of rotation of each particle
**RandomizeRotationDirection** - when true, each particle randomly either goes clockwise or counterclockwise.
#### Flipbook
Controls the behavior of the animated texture that is applied to the particles.
**FlipbookTexture** - the flipbook texture to apply to the particles
**FlipbookDimensions** - the number of rows and columns in the selected flipbook texture
**FlipbookSpeed** - the playback frame rate of the flipbook.
**MatchParticlePhase** - when true, the first frame of the flipbook will play when the particle is born and the last frame will play just before the particle dies - so the flipbook playback length will match the particle’s lifetime.
#### Forces
Control the external forces that affect the particle movement.
**Gravity** - the pull of gravity on the particles.  This is typically 0,-9.8, 0 - but some types of material, such as smoke or mist may be warm or lighter than air, which would cause them to move upward instead of getting pulled down by gravity.
**Wind** - the direction and strength of the wind.
#### Debug
These controls allow you to debug specific parts of the particle system.
**DebugTime** - When true, allows you to scrub time backwards and forward manually with the ManualTime slider.
**ManualTime** - when DebugTime is true, you can use this slider to scrub time backwards and forward to see how the particles behave at different points during their lifetime.

## Conditions
This section illustrates two ways to branch your shader.

#### Branch On Render Pipeline
Shader Graph allows you to create shaders that can be used in multiple render pipelines- Built-In, URP, and HDRP.  This can be done by opening the shader in Shader Graph and adding the targets for all of the pipelines you want the shader to support in the Active Targets section under Graph Settings in the Graph Inspector window.

When supporting multiple render pipelines, it’s occasionally necessary to do different things in the graph depending on which pipeline is being used.  In order to do that, you need to branch the shader based on the active render pipeline.  There isn’t an official node in Shader Graph for performing that branching operation, but it is possible to create a subgraph that contains a Custom Function node that does the branch.

In this example, we use that Branch On RP to create a different outcome depending which render pipeline is active.  In our simple example, we just make the cube a different color - green for URP, blue for HDRP, and yellow for the Built-In render pipeline - but you can do much more complex operations that are specific to each render pipeline using this same technique.

#### Branch On Material Quality

With Shader Graph, you can create one shader that has multiple different ways of achieving the same effect depending on how much GPU processing power you want to dedicate to it.  This example illustrates that.  We show three different methods of combining two normal maps together.  The first method (at the top of the graph) is using the Normal Blend node set to Reoriented mode.  This is the most accurate method that provides the best looking results, but it also requires the most compute power. The second method (in the middle of the graph) is almost as nice and a little bit cheaper.  The third method (at the bottom of the graph) is the cheapest and produces the lowest quality result.

On the right side of the graph, you can see that the three different methods are connected to the Material Quality node. You can add a Material Quality node by opening the Blackboard and selecting Keyword->Material Quality from the add menu.  Then drag the Material Quality parameter from the Blackboard into your graph.   This node will select the top, middle, or bottom part of the graph depending on the Quality level that is selected.

In HDRP, the Quality setting is defined by the Default Material Quality Level setting found in the Material section of the HD Render Pipeline Asset. So for each Quality level, you define a pipeline asset, and that asset has the setting that controls which quality level the shader uses.

In a URP project, you can use the SetGlobalShaderKeywords command in the script that gets run when the user selects options in the application’s UI.  For example, the following command will set Material Quality to High:

MaterialQualityUtilities.SetGlobalShaderKeywords( MaterialQuality.High );

Using the Material Quality node in Shader Graph enables you to provide the user with the ability to customize their experience in the application.  They can choose to see higher quality visuals at a lower frame rate, or lower-quality visuals at a higher frame rate. And you control what these options do in the shader itself.

