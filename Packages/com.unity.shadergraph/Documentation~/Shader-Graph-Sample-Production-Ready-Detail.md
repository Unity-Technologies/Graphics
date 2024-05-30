# Production Ready Shaders

The Shader Graph Production Ready Shaders sample is a collection of Shader Graph shader assets that are ready to be used out of the box or modified to suit your needs.  You can take them apart and learn from them, or just drop them directly into your project and use them as they are. The sample also includes a step-by-step tutorial for how to combine several of the shaders to create a forest stream environment.

The sample content is broken into the following categories:

 - **Lit shaders** - Introduces Shader Graph versions of the HDRP and URP Lit shaders. Users often want to modify the Lit shaders but struggle because they’re written in code. Now you can use these instead of starting from scratch.
 - **Decal shaders** - Introduces shaders that allow you to enhance and add variety to your environment. Examples include running water, wetness, water caustics, and material projection.
 - **Detail shaders** - Introduces shaders that demonstrate how to create efficient [terrain details](https://docs.unity3d.com/Manual/terrain-Grass.html) that render fast and use less texture memory. Examples include clover, ferns, grass, nettle, and pebbles
 - **Rock** - A robust, modular rock shader that includes base textures, macro and micro detail, moss projection, and weather effects.
 - **Water** - Water shaders for ponds, flowing streams, lakes, and waterfalls. These include depth fog, surface ripples, flow mapping, refraction and surface foam.
 - **Post-Process** - Shaders to add post-processing effects to the scene, including edge detection, half tone, rain on the lens, an underwater look, and VHS video tape image degradation.
 - **Weather** - Weather effects including rain drops, rain drips, procedural puddles, puddle ripples, and snow
  - **Miscellaneous** - A couple of additional shaders - volumetric ice, and level blockout shader.

## Lit Shaders
Both URP and HDRP come with code-based shaders. The most commonly used shader for each of the SRPs is called Lit. For projects that use it, it’s often applied to just about every mesh in the game. Both the HDRP and URP versions of the Lit shader are very full-featured.  However, sometimes users want to add additional features to get just the look they’re trying to achieve, or remove unused features to optimize performance. For users who aren’t familiar with shader code, this can be very difficult.

For that reason, we’ve included Shader Graph versions of the Lit shader for both URP and HDRP in this sample pack. Users will be able to make a copy of the appropriate Shader Graph Lit shader, and then change any material that’s currently referencing the code version of the Lit shader with the Shader Graph version. All of the material settings will correctly be applied and continue to work.  They’ll then be able to make changes to the Shader Graph version as needed.

Please note that most *but not all* of the features of the code-based shaders are duplicated in the Shader Graph versions. Some lesser-used features may be missing from the Shader Graph versions due to the differences in creating shader with Shader Graph vs creating them with code.

Also note - If you’re going to use the Lit shader *as is*, we recommend sticking with the code version.  Only swap out the shader for the Shader Graph version if you’re making changes.  We also recommend removing unused features from the Shader Graph version for better performance.  For example, if you’re not using Emissive or Detail Maps, you can remove those parts of the shader (both graph nodes and Blackboard parameters) for faster build times and better performance. The real power of Shader Graph is its flexibility and how easy it is to change, update, and improve shaders.

#### URP Lit
Just like the code version, this shader offers the Metallic workflow or the Specular workflow. Shaders can be either opaque or transparent, and there are options for Alpha Clipping, Cast Shadows, and Receive Shadows. For the main surface, users can apply a base map, metallic or specular map, normal map, height map, occlusion map, and emission map. Parameters are available to control the strength of the smoothness, height, normal, and occlusion and control the tiling and offset of the textures.

Users can also add base and normal detail maps and mask off where they appear using the mask map.

For more details on each of the parameters in the shader, see the [Lit Shader documentation for URP](http://UnityEditor.Rendering.Universal.ShaderGUI.LitShader).

##### Shader Variant Limit
In order to be able to use this shader, you’ll need to increase the Shader Variant Limit to at least 513.  This should be done on both the Shader Graph tab in Project Settings as well as the Shader Graph tab in the Preferences.

##### Custom Editor GUI
In order to create a more compact and user-friendly GUI in the material, this shader uses the same Custom Editor GUI that the code version of the Lit shader uses.  Open the Graph Inspector and look at the Graph Settings. At the bottom of the list, you’ll see the following under Custom Editor GUI:

        UnityEditor.Rendering.Universal.ShaderGUI.LitShader

This custom GUI script enables the small texture thumbnails and other features in the GUI. If you need to add or remove parameters in the Blackboard, we recommend removing the Custom Editor GUI and just using Shader Graph’s default material GUI instead.  The custom GUI depends on the existence of many of the Blackboard parameters and won’t function properly if they’re removed.

#### HDRP Lit
Just like the code version, this shader offers opaque and transparent options. It supports Pixel displacement (Parallax Occlusion mapping) and all of the parameters that go with it. (It does not support Material Types other than standard.) For the main surface, users can apply a base map, mask map, normal map, bent normal map, and height map. Options are also available to use a detail map and emissive map.

For more details on each of the parameters in the shader, see the [Lit Shader documentation for HDRP](http://UnityEditor.Rendering.Universal.ShaderGUI.LitShader).

##### Custom Editor GUI
In order to create a more compact and user-friendly GUI in the material, this shader uses the same Custom Editor GUI that the code version of the Lit shader uses.  Open the Graph Inspector and look at the Graph Settings. At the bottom of the list, you’ll see the following under Custom Editor GUI:

        Rendering.HighDefinition.LitGUI

This custom GUI script enables the small texture thumbnails and other features in the GUI. If you need to add or remove parameters in the Blackboard, we recommend removing the Custom Editor GUI and just using Shader Graph’s default material GUI instead.  The custom GUI depends on the existence of many of the Blackboard parameters and won’t function properly if they’re removed.

## Decals
Decals allow you to apply local material modifications to specific places in the world. You might think of things like applying graffiti tags to a wall or scattering fallen leaves below a tree. But decals can be used for a lot more. In these examples, we see decals making things look wet, making surfaces appear to have flowing water across them, projecting water caustics, and blending specific materials onto other objects.

Decals are available to use in both HDRP and URP, but they need to be enabled in both render pipelines. To use decals, see the documentation in both [HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/decals.html) and [URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/renderer-feature-decal.html).

#### Material Projection
This decal uses triplanar projection to project a material in 3D space. It projects materials correctly onto any mesh that intersects the decal volume.  It can be used to apply terrain materials on to other objects like rocks so that they blend in better with the terrain.
#### Water Caustics
When light shines through rippling water, the water warps and focuses the light, casting really interesting rippling patterns on surfaces under the water.  This shader creates these rippling caustic patterns. If you place decals using this shader under your water planes, you’ll get projected caustics that imitate the behavior of light shining through the water.
#### Running Water
This decal creates the appearance of flowing water across whatever surfaces are inside the decal. It can be used on the banks of streams and around waterfalls to support the appearance of water flowing. With material parameters, you can control the speed of the water flow, the opacity of both the wetness and the water, and the strength of the flowing water normals.
#### Water Wetness
The wetness decal makes surfaces look wet by darkening color and increasing smoothness. It uses very simple math and no texture samples so it is very performance efficient. It can be used along the banks of bodies of water to better integrate the water with the environment.

## Details
In this context, Details refer to meshes that are added to terrain - such as grass, weeds, undergrowth, pebbles, etc. To learn more, read the terrain documentation on [details](https://docs.unity3d.com/Manual/terrain-Grass.html). Detail meshes have some specific requirements for shaders.  First, because of the high number of these meshes used on the terrain, we have to make their shaders as fast and efficient as possible. That mainly means keeping the number of texture samples low and doing more work in the vertex shader instead of the pixel shader.  And second, because these meshes stop rendering and pop out at a specific distance, we have to use a method to dissolve them out to prevent the harsh pop and make it less obvious that they’re being removed. In each of the shaders, you’ll see the Distance Mask or Distance Cutoff node used to create a mask that dissolves away the mesh at a distance before the mesh stops rendering.
#### Clover
The shader for the clover uses just one single channel texture to reduce cost and save texture memory. Color is generated by lerping between a bright color and a dark color using the greyscale value from the texture. Each instance of clover uses a slightly different color variation.  We generate a random value based on the position of each instance and that's used to give each instance a color variation.
The Distance Mask is calculated in the vertex shader to save performance and then passed to the pixel shader where it’s combined with the texture and some screen space noise. Together, these elements are passed into the Fade Transition node which makes the mesh dissolve between the Clip Offset and Clip Distance values.
#### Ferns
The fern shader uses a color, normal, and mask texture to define the fern material.  It animates the ferns based on wind settings. It also creates a subsurface scattering effect so that the fern fronds are illuminated on the reverse side from the sunlight. For ambient occlusion, we darken the AO close to the ground. As with the other detail shaders, we also dissolve the fern as we move away to prevent it from popping out.
#### Grass
Usually, grass is created with billboards using a grass texture. This shader is different. We use mesh for each individual blade of grass. To keep the meshes as cheap as possible, our grass blade meshes have only 12 vertices and 10 triangles. They don’t have UV coordinates, normals, or vertex colors - so the only data stored in the mesh is position. The meshes are as simple as they can possibly be. We also do as much work as possible in the vertex shader for lower cost. Wind, color, translucency, and distance fade are all calculated in the vertex shader.

The shader generates wind forces and then uses them to bend the blades of grass. The wind forces vary in direction and gust strength so the movement of the blades feels natural.
#### Nettle
The nettle shader is for simple, broad-leaf undergrowth.  It’s a variation of the fern shader - so it has similar features.  The main difference is that it has been adapted to only use one texture sample to reduce both texture memory usage and shader cost. The texture has the normal X and Y in the red and green channels. The blue channel is a combination of the opacity and a grayscale mask that is used to modulate smoothness, AO, and color.
#### Pebbles
As with the rest of the detail shaders, the pebble shader is designed to be as cheap as possible. It only uses one small noise texture. It creates color variation using the noise texture and the instance IDs so that each pebble cluster has its own unique color. And it fades the pebbles out at a distance to prevent popping.

## Rock
This is a full-featured, modular rock shader that can be used for everything from small pebbles to boulders up to large cliff faces. It has features that can be turned on and off in the material depending on the application. Each of the features is encapsulated in a subgraph so it’s easy to remove features that you don’t need.  You can also add new features in the chain of modules if you need something else. Each module takes in color and smoothness in one input port and normal and ambient occlusion in a second input. Inside the subgraph, it alters these, and then it outputs the result again in the same format - color and smoothness in the first output port, and normal and AO in the second. Using this input/output port format keeps all of the modules organized and in a nice, neat line.

To help with performance, the shader has an LOD0 boolean parameter exposed to the material. For materials applied to LOD0 of your rocks, this should be true.  For materials applied to the other LODs, this boolean should be false. This feature turns off extra features that are only visible when the rock is close so that non-LOD0 versions of your rocks render faster.

Additionally, this shader branches using the Material Quality built-in enum keyword. This means that the shader is already set up to create a low quality, medium quality, and high quality version of itself depending on project settings. Features will be turned on and off, or different variations of features will be used depending on the project’s Material Quality setting.

#### Base Textures
In order to reduce the total number of texture samples in the shader (sampling textures is the most expensive operation that shaders do, so reducing the number of texture samples can significantly improve shader performance), we’ve used a two-texture format for our base textures instead of 3.  The format is as follows:

**CS Texture** (BC7 format) - RGB - color, Alpha - smoothness
**NO Texture** (BC7 format) - RGB - normal, Alpha - ambient occlusion

    Note that the NO texture is NOT saved as a normal map. If you set it to be a normal map, the ambient occlusion data in the alpha channel will be lost.

#### Macro Detail
The purpose of the Macro Detail module is to add large-scale details to the color, smoothness, normal, and ambient occlusion of the rock’s base material. For rocks that are large, a single texture set is often not high enough resolution and the base textures look blurry or blocky, even from a distance. The Macro Detail module solves this problem. For rocks that are smaller than 1 meter cubed, this feature should be turned off by unchecking Rock Features/MarcoDetail in the rock’s material.

This feature references a texture - Rock_Macro_NOS - which you are welcome to use, or you can create your own.  The texture format is as follows:

R: normal X
G: normal Y
B: ambient occlusion
A: smoothness/color overlay

This texture needs to be set to Default 2D format with Compression set to High Quality. It is recommended to just use one single macro detail texture for all of the rocks in your project both to save on texture memory and to maintain a consistent visual style.  For this reason, the texture itself is not exposed as a material parameter but is set directly in the shader.
#### Color Projection
For large boulders and cliff faces, you may want to add colored effects to the rocks such as bleaching. The Color Projection module can handle this. It projects color alterations using world space. The nice thing about this effect is that if your rock formation is made up of multiple rocks all jammed together, the color projection will tie them together and make them feel more cohesive - as if they’re one unified formation rather than just a collection of jammed-together rocks.

This effect does use 5 texture samples, so if you don’t need it, or if you’re on a very performance sensitive platform such as a mobile device, you should definitely turn it off in the material to improve performance.
#### Micro Detail
The purpose of the Micro Detail module is to add small-scale details to the color, smoothness, normal, and ambient occlusion of the rock’s base material.  When you get really close to the rocks, sometimes the resolution of the base textures is not high enough and they look blurry or blocky. The Micro Detail module solves this problem by adding very high resolution micro detail to the rock surface.

This feature references a texture - Rock_Micro_NOS - which you are welcome to use, or you can create your own.  The texture format is as follows:

R: normal X
G: normal Y
B: ambient occlusion
A: smoothness/color overlay

This texture needs to be set to Default 2D format with Compression set to High Quality. It is recommended to just use one single micro detail texture for all of the rocks in your project both to save on texture memory and to maintain a consistent visual style.  For this reason, the texture itself is not exposed as a material parameter but is set directly in the shader.
#### Deposition Moss
The Deposition Moss module applies moss to the tops of the rocks. To define the moss, it uses a Moss_CO texture (color with occlusion in the alpha channel) and a Moss_N texture (normal). The alpha channel of the Moss_CO texture is also used to create smoothness.

It’s also possible to use this module to apply other types of materials to the tops of the rocks - such as sand, ash, snow, etc. To do that, you’d just need to set the Deposition Moss module to use textures for your chosen material instead.  These textures are not exposed to the material, but they are available to be changed on the module.
#### Rain
When the IsRaining parameter is set to 1, the Rain module applies rain effects to the rocks, including animated rain drops on the tops of the rocks, and drips running down the sides of the rocks.  The module also makes the rocks look wet.

## Water
The sample set comes with four different water shaders. Each one uses reflection, refraction, surface ripples using scrolling normal maps, and depth fog. Each also uses a few additional features that are unique to that type of water.
#### WaterLake
This is the simplest water shader of the group.  Because it’s meant to be applied to larger bodies of water, it has two different sets of scrolling normals for the surface ripples - one large, one small - to break up the repetition of the ripples. It also fades the ripples out at a distance both to hide the tiling patterns and to give the lake a mirror finish at a distance.
#### WaterSimple_FoamMask
This shader is intended to be used on ponds or other small bodies of non-flowing water. It uses 3 Gerstner waves subgraphs to animated the vertices in a chaotic wave pattern. It also adds foam around the edges of the water where it intersects with other objects. The unique thing about the foam implementation in this shader is that it allows you to additionally paint a texture mask that determines where foam can be placed manually. This manual placed foam could be used for a spot where a waterfall is hitting the water - for example.
#### WaterStream
The water stream shader is intended to be used on small, flowing bodies of water. Instead of standard scrolling normal maps for ripples, it uses flow mapping to make the water flow slowly along the edges of the stream and faster in the middle. It also uses the same animated foam technique as the WaterSimple shader - but without the mask.

This shader uses the puddle_norm texture.  Notice that we save a little bit of shader performance by NOT storing this texture as a normal map. After the two samples are combined, then we expand the data to the -1 to 1 range, so we don’t have to do it twice.
#### WaterStreamFalls
This is the same shader as the WaterStream, but it’s intended to be used on a waterfall mesh. It fades out at the start and the end of the mesh so that it can be blended in with the stream meshes at the top and bottom of the falls, and it adds foam where the falls are vertical.

## Post-Process
The post process shaders can be used to apply modifications to the rendered image once the scene has been drawn.
#### Edge Detection
The edge detection shader checks the four neighboring pixels to the current one to find “edges” or places where the normal or depth has changed rapidly. It creates a mask where edges exist and then uses the mask to blend between the original scene color and the edge color.
#### Half Tone
The halftone shader turns the rendered image into a halftone image - simulating the pattern of larger and smaller circle patterns that you might see in newsprint or comic books. First it generates a procedural grid of signed distance field circles - one for each of red, green, and blue. Then it uses inverse lerp to convert the SDF circle grid into dots - where the size of the dot represents the brightness of the color at that location. Finally it combines the red, green, and blue dot grids into one color.
#### Rain On Lens
The rain-on-the-lens post process shader applies refraction to the rendered scene as if there were rain on the camera lens - so some areas of the image are warped by rain drops and other areas are distorted by drips running down the screen.
#### Underwater
The underwater post process shader makes the scene look like it’s under water by applying several effects including blurring the screen around the edges, distorting the image is large, ripple patterns, and applying a blue/green fog based on the scene depth.
#### VHS
The VHS post process shader mimics the appearance of the scene being played back on an old VHS video cassette recorder. Artifacts include scan line jitter, read head drift, chromatic aberration, and color degradation in the YIQ color space.

## Weather
This sample comes with a full set of weather-related subgraphs (rain and snow) that can be mixed and matched depending on the requirements of the object type they’re applied to.
### Rain
There are several subgraphs that generate rain effects. Each has a different subset of the available rain effects. Applying all of the effects at once is a bit expensive on performance, so it’s best to choose the option with just the effects you need for the specific type of object/surface. 

#### Rain
The Rain subgraph combines all of the rain effect - drops, drips, puddles, wetness - to create a really nice rain weather effect - but it’s the most expensive on performance. Puddles are a bit expensive to generate as are drips, so this version should only be used on objects that will have both flat horizontal surfaces as well as vertical surfaces. 
#### Rain Floor
The Rain Floor subgraph creates puddle and drop effects, but it does not have the drip effects that would run down vertical surfaces. This subgraph is best used for flat, horizontal surfaces.
#### Rain Props
The Rain Props subgraph has the drop and drip effects but does not include the puddles.  It’s best for small prop objects.
#### Rain Rocks
The Rain Rocks subgraph has been specifically tuned for use on rocks. It includes drips and drops, but not puddles. It also includes the LOD0 parameter that is meant to turn off close-up features on LODs other than the first one.
#### Components
##### Puddles
The puddles subgraph creates procedurally-generated puddles on flat, up-facing surfaces. It outputs a mask that controls where the puddles appear and normals from the puddles. It uses the PuddleWindRipples and RainRipples subgraph to generate both wind and rain ripples in the puddles.
##### PuddleWindRipples
The PuddleWindRipples subgraph creates puddle wind ripples by scrolling two normal map textures.  It’s used by the Puddles subgraph.
##### Rain_Drips
This subgraph creates drips that drip down the sides of an object. The drips are projected in world space, so they work well for static objects but are not meant for moving objects. The speed of the drips is controlled by the permeability of the material. Smooth, impermeable surfaces have fast moving drips while permeable surfaces have slow-moving drips.
##### Rain_DripsOnTheLens
This subgraph is very similar to the Rain_Drips subgraph, but it’s adapted to work correctly for the RainOnTheLens post-process shader.
##### Rain_Drops
This subgraph applies animated rain drops to objects.  The drops are projected in world space from the top down. Because of the world space projection, these rain drops are not designed to be added to objects that move in the scene but instead should be used for static objects. The IsRaining input port turns the effect on and off (when the input value is 1 and 0).
##### Rain_Parameters
A common set of rain parameters used by most of the rain subgraphs. Setting parameters once in this subgraph means you don’t have to set them all over in multiple places.
##### RainDropsOnTheLens
This subgraph is very similar to the RainDrops subgraph, but it’s adapted to work correctly for the RainOnTheLens post-process shader.
##### RainRipple
Creates an animated circular ripple pattern. This subgraph is used multiple times in the RainRipples subgraph to create a really nice-looking pattern of multiple overlapping ripples.
##### RainRipples
The RainRipples subgraph creates ripples from rain drops in a puddle or pool of water. It combines four instances of the RainRipple subgraph (each with its own scale, position, and timing offset) to create the chaotic appearance of multiple ripples all happening at once. It’s used by the Puddles subgraph to add rain ripples to the puddles.
##### Wet
This subgraph makes surfaces look wet by darkening and saturating their base color and by increasing their smoothness. The effect is different depending on how permeable the surface is.
### Snow
The Snow subgraph creates a snow effect and applies it to the tops of objects. The snow material includes color, smoothness, normal, metallic, and emissive - where the emissive is used to apply sparkles to the snow.

## Miscellaneous shaders
#### Blockout Grid
Apply this simple shader to a 1 meter cube.  You can then scale and stretch the cube to block out your level.  The grid projected on the cube doesn't stretch but maintains its world-space projection so it's easy to see distances and heights.  It's a great way to block out traversable paths, obstacles, and level layouts. Turn on the **EnableSlopeWarning** parameter to shade meshes red where they’re too steep to traverse.
#### Ice
This ice shader uses up to three layers of parallax mapping to create the illusion that the cracks and bubbles are embedded in the volume of the ice below the surface though there is no transparency or actual volume. It also uses a Fresnel effect to brighten the edges and create a frosted look.

## Forest Stream Construction Tutorial
This tutorial shows you, step by step, how to use the assets included in this sample to construct a forest stream environment.

#### Step 1 - Sculpt Terrain
We start by blocking out the main shapes of the terrain. We use the Set Height brush to create a sloping terrain by creating a series of terraces and then using the Smooth brush to smooth out the hard edges between the terraces.

Then we cut in our stream channel with the Set Height brush in several different tiers heading down the slope. After cutting in the stream, we smooth out the hard edges using the Smooth brush.

We finalize the terrain shape by adding polish using the Raise/Lower Height brush and the Smooth brush to add touch-ups and variety.  In this process, we start out with large brushes and end up using small ones.

Once this step is done, we do revisit the terrain shape occasionally to add additional touch ups, especially after adding in the water meshes in steps 3 and 4, to ensure that the water meshes and terrain shape work together.


#### Step 2 - Paint Terrain Materials
Next, it’s time to add materials to our terrain.  We have four material layers - cobblestone rocks for our stream bed, dry dirt, rocky moss, and mossy grass. To apply the materials, we begin by establishing guidelines.  The stones material goes in the stream bed.  The dirt material goes along the banks of the stream. As a transition between the first and the grass, we use the rocky moss material. And finally, we use the grass material for the background.

We first block in the materials according to our guidelines with large, hard-edged brushes. Then we go back and blend the materials together using smaller brushes. We paint one material over the other using brushes with a low opacity value to blend the two materials together.

Even though our terrain materials exhibit tiling artifacts by themselves, we’re able to hide the tiling by giving each material a different tiling frequency. When the materials are blended, they break up each others tiling artifacts.  We also cover the terrain with detail meshes (step 7) which further hides the tiling.

#### Step 3 - Add Water Planes
The stream itself is constructed from simple planes that are added to the scene by right clicking in the Hierarchy panel and selecting 3D Object->Plane. Then we apply the WaterStream material.  The planes are placed in the stream channel that’s cut into the terrain, and then scaled along the Z axis to stretch them along the length of the stream. Water flows in the local -Z direction of the planes. Planes are scaled as long as they need to be in order to reach from one stream height drop to the next.

Notice that the edges of the stream mesh are transparent at the start and at the end.  This is to allow the stream meshes to blend together correctly with the waterfall meshes that link the stream planes together.

#### Step 4 - Add Waterfall Meshes
The waterfall meshes are designed to connect one level of stream plane to the next lower level. They are placed at the end of a stream plane and slope down to connect to the next stream plane. We rotate the waterfall meshes around the Y axis to align the waterfall mesh between the two stream planes.

The pivot point of the waterfall is lined up vertically with the top portion of the waterfall, so you can place the waterfall mesh at the exact same height as the top stream plane, and then scale the waterfall mesh so that the bottom portion of the waterfall mesh aligns with the lower stream plane.

Notice that the Sorting Priority parameter in the Advanced Options of the material has been set to -1.  This makes the waterfall meshes draw behind the stream meshes so there isn’t a draw order conflict.

#### Step 5 - Add Rocks
Streams are often filled with rocks that have been pushed by the current. To save memory and reduce draw calls, we’re just using two different rock meshes that both use the same texture set.  The rocks are rotated and scaled to give a variety of appearances. Notice that we’ve created visual variety by creating two different sizes of rocks - large boulders, and smaller rocks. Overall, the rocks break up the shape of the stream and change the pattern of the foam on the water surface.

#### Step 6 - Add Water Decals
We use the Water Wetness and Water Caustics decal to more tightly integrate the stream water with the terrain and rocks. The Wetness decal makes the terrain and other meshes around the stream look like they’re wet, and the Caustics decal imitates the appearance of lighting getting refracted by the surface of the water and getting focused in animated patterns on the bottom of the stream.

For the Wetness decal, it should be created and scaled so that the top of the decal extends around half a meter above the surface of the water. The top of the Caustics decal should be just under the water.

For both decals, the decal volumes should be kept as small as possible in all three dimensions - just large enough to cover their intended use and no larger. You can also save some performance by lowering the Draw Distance parameter on each decal so they are not drawn at a distance.

#### Step 7 - Add Reflection Probes
Reflections are a critical component of realistic-looking water. To improve the appearance of the water reflections, we create a Reflection Probe for each of the stream segments and place it at about head height and in the middle of the stream. If were are objects like rocks and trees nearby, they will be captured in the Reflection Probes and then reflected more accurately in the water.

Especially notice how water to the right of this point is correctly reflecting the high bank behind the signs while water to the left is only reflecting the sky. This additional realism is contributed by the Reflection Probes.

#### Step 8 - Add Terrain Detail Meshes
Our last step is to add detail meshes to the terrain. We have pebble meshes that are added everywhere, including under the water. We have broad-leaf nettle plants that are added around the edges of the water in the dirt areas. We have ferns (3 variations) that are added just above the nettle in the transition between dirt and grass, and we have clover that is added in between the ferns and the grass. For the grass, we have three different meshes that each fade out at a different distance from the camera to soften the fade-out so that it doesn’t happen all at once. The most dense grass is only visible at 10 meters from the camera to improve performance. The three different grass layers are painted somewhat randomly with all three layers being applied where the terrain grass material is most dense and the most sparse grass being painted around the edges. Each grass mesh also has slightly different wind direction and intensity values in the material to give variety to the grass appearance. Only one of the three grass meshes has shadows turned on - which gives the impression of grass shadows without paying the full performance cost.

To save on performance, our terrain is set to fade out the detail meshes at 30 meters. This allows us to achieve a nice density of meshes up close and then get rid of them further away where they’re not as visible.  We hide the transition by dither fading the meshes in the shader before the 30 meter point so there’s not popping.

#### Additional Ideas
We have a pretty nice looking environment here, but there’s a lot more that could be done. You could complete this environment by adding your own trees, stumps and fallen logs.

