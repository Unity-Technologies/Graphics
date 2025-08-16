# Shaders

The names of these shaders indicate the [texture packing scheme](Shader-Graph-Sample-Terrain-Packing.md) they use, the type of layers they’re using, and the technique used to blend the layers together.

You can either use these terrain shaders as provided in the sample, or keep them as reference to see how each layer works and then build your own terrain shader using the existing layer type nodes blended together with your choice of layer blend nodes.

Each of the example shaders uses four layers, but you can use fewer or more depending on the needs of your project.  Just remember that each new layer makes the shader more expensive.

**CHNOS Array Dither** - this shader uses a technique that reduces texture samples as much as possible.  The texture packing scheme packs the data from multiple materials into a single array texture. For each terrain material, the color and height data are stored in individual CH textures (color in RGB, height in the alpha channel). But the normal, occlusion, and smoothness for all of the materials on the terrain are packed into a single array texture.  Because of this packing scheme, this 4-layer shader is able to do just 6 texture samples total, as opposed to the CNMSimpleBlend shader which requires 13. A dither pattern is used to blend out the hard edges that would otherwise appear when sampling an array texture.

**CHNOS Array Hex** - this shader uses the same packing scheme as the one above, but also includes hex grid tiling to break up texture repetition.  This four-layer shader is doing just 15 texture samples, as opposed to the CNMHexHeight shader, which is doing 36. A dither pattern is used to blend out the hard edges that would otherwise appear when sampling an array texture.

**CNM BuiltIn Blend** - This shader is the most basic example of a terrain shader and matches the functionality of the existing terrain shader written in code. It uses the Built-In Terrain Layer nodes for each layer and uses the Blend2LayersBasic nodes to blend all of the layers together.  It doesn’t do any tile break-up or fading between the near and far terrain materials. It is a good example of how the Terrain Layer Mask and Terrain Layer nodes can be used together to create a very simple and cheap terrain shader.

**CNM Hex Height** - This shader uses the LayerHex layer nodes (including one LayerHexDetail layer) - so it’s doing the hex tile method of repetition artifact break-up.  It’s more expensive than the other shaders, but is the most effective at breaking up repetition. This shader also uses height blending for more realistic transitions between layers.

**CNM Rotation Height** - This shader uses LayerRot layer type - so it’s using the rotation method for tile break-up.  It is not as effective as the hex tile method, but it’s cheaper.  Layers are blended using height-based blending.

**CNM Simple Blend** - This shader is designed to be as simple as possible while still using a custom layer type - the LayerSimple type.  It’s using simple lerp blending.  In my testing this shader and CNMBiultInBlend performed the same - both of them very cheap.

**CNM Simple Height** - This shader is the same as CNMSimpleBlend, but it’s using height-based blending instead of lerp blending for a more realistic look.

**CNM Procedural Height** - This shader uses the Procedural method of breaking up the repetition. It’s similar to the Rotation method in that it samples the material textures twice, but the mask that blends between them is generated procedurally instead of using a mask texture. It’s more expensive than the Rotation method, but also more effective at tile break-up.

**CSNOH Auto Material Hex** - this example shader is similar to CSNOH Auto Material Simple, but it uses the hex grid tiling technique to break up the texture tiling.

**CSNOH Auto Material Rotation** - this example shader is similar to the one above, but it uses the rotation technique to break up texture tiling repetition.

**CSNOH Auto Material Simple** - this is an example of a shader that can be created to automatically apply materials to surfaces without requiring any manual painting or pre-generated splat maps. The shader uses the angle of the terrain along with the altitude to apply grass, dirt, rocks, cliff faces, and even snow in appropriate locations. At lower altitudes, the shader blends between grass on flat surfaces and dirt on more angled surfaces.  At higher altitudes, the shader blends between rocky soil on flat surfaces and cliff faces on slopes.  Then it blends the two together and adds snow on top.

**CSNOH Hex Blend** - This shader uses the Layer Hex CSNOH layer nodes - so it’s doing the hex tile method of repetition artifact break-up. It’s using standard alpha blending between the layers instead of height-based blending.  It’s more expensive than CSNOH Simple Blend or CSNOH Rotation Height, but is the most effective at breaking up repetition.

**CSNOH Hex Height** - This shader uses the Layer Hex CSNOH layer nodes - so it’s doing the hex tile method of repetition artifact break-up. Layers are blended using height-based blending. It’s more expensive than CSNOH Simple Blend or CSNOH Rotation Height, but is the most effective at breaking up repetition.

**CSNOH Hex Parallax Height** - this shader is using both parallax mapping and hex grid tile break-up for every layer.  It is extremely expensive because of the high number of texture samples required by all of the layers.  We DO NOT recommend using a shader like this in production (where every layer is using the most expensive layer type).  Instead, you should mix and match layer types based on the requirements of the materials you’re using. This example is included for performance and visual comparison purposes.

**CSNOH Parallax Height** - this shader uses parallax mapping - a form of ray marching, to create the illusion of tessellation and displacement on the surface of the terrain.  Since we’re doing many texture samples, this technique can be quite expensive.  Unlike this example which uses Layer Parallax CSNOH layers for every layer, we recommend using only one or two Layer Parallax CSNOH (for the materials that really need them) in your shaders to save on performance.

**CSNOH Rotation Height** - This shader uses Layer Rotation CSNOH layer type - so it’s using the rotation method for tile break-up.  It is not as effective as the hex tile method, but it’s cheaper.  Layers are blended using height-based blending.

**CSNOH Simple Blend** - This shader is designed to be as simple as possible while still using a custom layer type - the Layer Simple CSNOH type.  It’s using simple alpha blending.  In our testing this shader and CSNOH Simple Height performed the same - both of them very cheap (much cheaper than their CNM counterparts).

**CSNOH Simple Height** - This shader is the same as CSNOH Simple Blend, but it’s using height-based blending instead of lerp blending for a more realistic look.

**CSNOH Procedural Height** - This shader uses the Procedural method of breaking up the repetition. It’s similar to the Rotation method in that it samples the material textures twice, but the mask that blends between them is generated procedurally instead of using a mask texture. It’s more expensive than the Rotation method, but also more effective at tile break-up.

## Beyond these shaders
While each of these sample shaders uses primarily one layer type and one type of blend node, you can mix and match them (and even create your own layer types and blend nodes) to get the specific features that you need for each individual layer. Maybe you have one layer with especially bad tiling artifacts.  You could use the LayerHex layer type for that and then blend that into a Layer Triplanar layer that’s specifically for cliffs. The point is that you’re not locked into just one type of layer, so you can use some really cheap layers to save on performance and some fancier layers where you need more advanced features.


