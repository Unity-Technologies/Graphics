# Forest Stream Construction Tutorial
This tutorial shows you, step by step, how to use the assets included in this sample to construct a forest stream environment.

1. [Sculpt Terrain](#step-1)
2. [Paint Terrain Materials](#step-2)
3. [Add Water Planes](#step-3)
4. [Add Waterfall Meshes](#step-4)
5. [Add Rocks](#step-5)
6. [Add Water Decals](#step-6)
7. [Add Reflection Probes](#step-7)
8. [Add Terrain Detail Meshes](#step-8)
[Additional Ideas](#additional-ideas)


## <a name="step-1">Step 1 - Sculpt Terrain</a>
1. Start by blocking out the main shapes of the terrain. Use the **Set Height** brush to create a sloping terrain by creating a series of terraces and then use the **Smooth** brush to smooth out the hard edges between the terraces.

2. Cut in the stream channel with the **Set Height** brush in several different tiers heading down the slope. After you cut in the stream, smooth out the hard edges with the **Smooth** brush.

3. Add polish to finalize the terrain shape. Use the **Raise/Lower Height** brush and the **Smooth** brush to add touch-ups and variety.  In this process, start out with large brushes and end with the small ones.

When this step is done, you can revisit the terrain shape occasionally to add additional touch ups, especially after adding in the water meshes in steps 3 and 4, to ensure that the water meshes and terrain shape work together.


## <a name="step-2">Step 2 - Paint Terrain Materials</a>
Next, it’s time to add materials to our terrain.  We have four material layers - cobblestone rocks for our stream bed, dry dirt, rocky moss, and mossy grass. To apply the materials, we begin by establishing guidelines.  The stones material goes in the stream bed.  The dirt material goes along the banks of the stream. As a transition between the first and the grass, we use the rocky moss material. And finally, we use the grass material for the background.

1. First block in the materials according to the guidelines with large, hard-edged brushes. 
2. Then we go back and blend the materials together using smaller brushes. Paint one material over the other using brushes with a low opacity value to blend the two materials together.

Even though our terrain materials exhibit tiling artifacts by themselves, we’re able to hide the tiling by giving each material a different tiling frequency. When the materials are blended, they break up each others tiling artifacts.  We also cover the terrain with detail meshes (step 7) which further hides the tiling.

## <a name="step-3">Step 3 - Add Water Planes</a>
The stream itself is constructed from simple planes that are added to the scene.
1. Right-click in the Hierarchy panel and select **3D Object**>**Plane**. 
2. Then apply the WaterStream material.  
3. Place the planes in the stream channel that’s cut into the terrain.
4. Scale the planes along the Z axis to stretch them along the length of the stream. Water flows in the local -Z direction of the planes. Planes are scaled as long as they need to be in order to reach from one stream height drop to the next.

Notice that the edges of the stream mesh are transparent at the start and at the end.  This is to allow the stream meshes to blend together correctly with the waterfall meshes that link the stream planes together.

## <a name="step-4">Step 4 - Add Waterfall Meshes</a>
The waterfall meshes are designed to connect one level of stream plane to the next lower level. 
1. Place the waterfall meshes at the end of a stream plane. They slope down to connect to the next stream plane. 
2. Rotate the waterfall meshes around the Y axis to align the waterfall mesh between the two stream planes.
3. Scale the waterfall mesh on the Y axis so that the bottom portion of the waterfall mesh aligns with the lower stream plane. The pivot point of the waterfall is lined up vertically with the top portion of the waterfall, so you can place the waterfall mesh at the exact same height as the top stream plane, and then scale to meet the lower stream plane.

Notice that the Sorting Priority parameter in the Advanced Options of the material has been set to -1.  This makes the waterfall meshes draw behind the stream meshes so there isn’t a draw order conflict.

## <a name="step-5">Step 5 - Add Rocks</a>
Streams are often filled with rocks that have been pushed by the current. To save memory and reduce draw calls, we’re just using two different rock meshes that both use the same texture set.
1. Place rocks at random intervals along the length of the stream.
2. Rotate and scale the rocks to give a variety of appearances. 

Notice that we’ve created visual variety by creating two different sizes of rocks - large boulders, and smaller rocks. Overall, the rocks break up the shape of the stream and change the pattern of the foam on the water surface.

## <a name="step-6">Step 6 - Add Water Decals</a>
We use the Water Wetness and Water Caustics decal to more tightly integrate the stream water with the terrain and rocks. The Wetness decal makes the terrain and other meshes around the stream look like they’re wet, and the Caustics decal imitates the appearance of lighting getting refracted by the surface of the water and getting focused in animated patterns on the bottom of the stream.

1. Create and scale the Wetness decals so that the top of the decal extends around half a meter above the surface of the water. The top of the Caustics decal should be just under the water.
2. Create and scale the caustics decals so that the caustic patterns are only projected under the water planes.

For both decals, the decal volumes should be kept as small as possible in all three dimensions - just large enough to cover their intended use and no larger. You can also save some performance by lowering the Draw Distance parameter on each decal so they are not drawn at a distance.

## <a name="step-7">Step 7 - Add Reflection Probes</a>
Reflections are a critical component of realistic-looking water. 
1. To improve the appearance of the water reflections, create a Reflection Probe for each of the stream segments and place it at about head height and in the middle of the stream. If there are objects like rocks and trees nearby, they will be captured in the Reflection Probes and then reflected more accurately in the water.

Especially notice how water to the right of this point correctly reflects the high bank behind the signs while water to the left only reflects the sky. The Reflection Probes contribute this additional realism.

## <a name="step-8">Step 8 - Add Terrain Detail Meshes</a>
Our last step is to add detail meshes to the terrain. 
1. Add pebble meshes everywhere, including under the water. 
2. Add broad-leaf nettle plants around the edges of the water in the dirt areas. 
3. Add ferns (3 variations) just above the nettle in the transition between dirt and grass.
4. Add clover in between the ferns and the grass. 
5. For the grass, add the three different meshes. Each of them fade out at a different distance from the camera to soften the fade-out so that it doesn’t happen all at once. The most dense grass is only visible at 10 meters from the camera to improve performance. Paint the three different grass layers somewhat randomly with all three layers being applied where the terrain grass material is most dense and the most sparse grass being painted around the edges. Each grass mesh also has slightly different wind direction and intensity values in the material to give variety to the grass appearance. Only one of the three grass meshes has shadows turned on - which gives the impression of grass shadows without paying the full performance cost.

To save on performance, our terrain is set to fade out the detail meshes at 30 meters. This allows us to achieve a nice density of meshes up close and then get rid of them further away where they’re not as visible.  We hide the transition by dither fading the meshes in the shader before the 30 meter point so there’s no popping.

## <a name="additional-ideas">Additional Ideas</a>
We have a pretty nice looking environment here, but there’s a lot more that could be done. You could complete this environment by adding your own trees, stumps and fallen logs.

