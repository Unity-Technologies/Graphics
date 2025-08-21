# Problems and Solutions

#### My terrain textures look too repetitive. I want to break up the texture tiling.  What shaders or layer types can I use?
This sample content showcases three types of tiling break-up: Rotation, Procedural, and Hex Grid Tiling.  Rotation samples the terrain textures twice.  The second set of samples is rotated 90 degrees and blended with the first using a large mask texture. Procedural is similar, but it generates the blending mask procedurally instead of using a texture mask.  For Hex Grid Tiling, we procedurally generate a grid of hexagons.  For each hexagonal tile, we randomly offset, rotate (optional), and scale the UVs and then use them to sample the terrain textures. Hex Grid Tiling requires 9 texture samples per layer using CNM and 6 samples per layer using CSNOH.  So Hex Grid Tiling is more expensive than Rotation or Tile Break, but it’s also significantly more effective at tile-breakup.

If you need to stick with the standard CNM texture packing scheme, take a look at the CNM Rotation Height shader or the CNM Hex Height shader.  If you want to make your own shader, try using Layer Hex or Layer Rotation as your layer types.

If you’re willing to try an alternate texture packing scheme, you can get cheaper results by using CSNOH.  Take a look at CSNOHRotHeight or CSNOHHexHeight.  And if you want to make your own shader, use LayerRotCSNOH or LayerHexCSNOH.

#### I want to break up texture tiling, but the tile break-up methods included in this sample are too expensive for my performance budget. Is there anything else I can do to break up tiling?
Here are some other ideas for tile breakup if you can't afford the performance cost of doing it in the shader:

- Scale the material larger so that it repeats less.

- Edit the textures to remove unique features that make repetition more obvious.

- Make each terrain layer tile at a different rate and then paint the layers such that the blending itself breaks up the tiling.

- Hide the tiling with other details on the terrain like rocks, grass, trees, and other details.

In general, large stretches of empty terrain make the tiling more obvious so anything you can do to interrupt that will reduce the repetitiveness.

#### I want to add additional close-up detail to a layer that is tiled very large.  What layer types can I use?
The Detail and Detail CSNOH components are made for this exact purpose.  In order to keep performance costs low, they sample a single detail texture and then unpack it to add additional, close-up detail to color, normal, smoothness, and occlusion.

If you’re building your own terrain shader, check out the Layer Hex Detail layer type to see how detail can be added. And if you’re using CSNOH, take a look at Layer Hex Detail CSNOH.

#### I want to hide the seam between the foreground terrain and the distant baselayer terrain. How can I do that?
The seam between the foreground shader and the basemap background shader is caused by a hard line between the ambient occlusion and normal maps of the foreground and the background - which just uses solid white for AO and flat blue for a normal.  These hard lines can be blended out using the DistanceFade node.  Just insert it into your terrain shader right at the end and then give it proper DistanceFadeStart and DistanceFadeLength values - depending on your terrain’s “Base Map Dist.” value.  DistanceFadeStart and DistanceFadeLength should add up to the Base Map Dist. value.  For example, if your Base Map Dist. value is 100, you could use 60 for DistanceFadeStart and 40 for DistanceFadeLength in order for the fading to match the terrain.

By the way, the background basemap version of the terrain is the absolute cheapest version of the terrain to render, so the lower you can set your Base Map Dist value to, the cheaper your terrain will be to render.  Hiding the hard seam can allow you to pull that distance in much closer than otherwise and get better performance.

#### The steep slopes of my terrain look very stretched.  What terrain layer type should I use for slopes?
The terrain’s UV coordinates are projected from the top down, so they work best on flat terrain.  The steeper the slopes are on your terrain, the more stretching you’ll get.  If you have terrain with a lot of steep surfaces, you should probably try projecting the textures in the X and Z directions instead.  Take a look at LayerTriplanar or LayerSideProjCSNOH for an example of how to do this.

#### I want to make my terrain render as fast as possible.  What techniques can I use?
Faster terrain rendering can be achieved using multiple techniques.  Try all or some of these to help improve performance:
- Reduce the number of layers.  Each terrain layer requires additional texture samples and makes the overall texture sample count higher.  Remove layers that aren’t used or used very little.
- Increase the Pixel Error setting on the terrain.  This will make the terrain’s dynamic adaptation system drop small triangles closer to the camera and reduce the overall-triangle count required to render the terrain. This value can vary quite a bit depending on your camera angle, distance from the terrain, etc, so it’s not possible to provide one value that’s perfect for everyone. Try to find a sweet spot where the visible popping isn’t too bad, but where you can still hit your triangle budget. 
- If the terrain shader is using Trilinear as the Filter type in the Sampler State for the textures, switch it to Linear instead. This will make the textures look a bit more blurry, but will also save performance. If you lose too much texture quality with this setting, you could also try just setting key textures to Trilinear and the rest to Linear. Note that for these settings to work correctly, you need to set the **Anisotropic Textures** setting in **Project Settings** > **Quality** to **Per Texture**.
- Lower the terrain’s **Base Map Dist.** setting.  This will make more of the distant terrain draw using the cheaper basemap version of the shader. It’s a good strategy to have a really nice looking shader up-close to the camera (that you create with Shader Graph) and then bring in the **Base Map Dist.** as close as you can so you’re not rendering your expensive shader where you don’t need it. The Distance Fade subgraph can be used to blend out your foreground shader making the line between foreground and background less obvious, which should enable you to bring the Base Map distance closer.
- Replace expensive layer nodes with cheaper ones in your custom shader.  If you’re using hex grid tiling (Layer Hex, or Layer Hex CSNOH) and it’s too expensive, try using Rotation instead (Layer Rotation or Layer Rotation CSNOH).  If even that is too much, you can set some or all of your layers to Layer Simple or Layer Simple CSNOH.  You won’t get tile break-up with those, but they are a lot cheaper.
- Use a texture packing scheme that requires fewer texture samples.  The standard Unity packing scheme is CNM - color, normal, masks - and it requires 3 texture samples per layer - minimum. If you switch to CSNOH (color/smoothness and normal, occlusion, and height), you’ll only be doing 2 samples per layer instead and this should get you around a 30% performance savings.
- For an even more extreme reduction in texture samples, you could use a texture array to pack all texture data (except color and height) into a single array texture. Take a look at the description for the CHNOS Array packing scheme or at the CHNOS Array example shader for more details.
- As a last resort, you could make a terrain shader that only uses color maps and does not use normals, smoothness, occlusion, or anything else. This wouldn’t be pretty, but it would only require 1 texture sample per layer.  We don’t recommend this, however, because for just one additional texture sample, you can have normals, smoothness, and occlusion if you use the CHNOS Array from the previous solution.

#### I want to apply a shader to a large terrain and have materials automatically added in logical locations without needing to hand-paint things. How can I do that?
This type of terrain shader is sometimes called an auto-material.  This sample content includes a couple of examples of auto-materials that use the terrain’s slope angle and elevation/altitude to automatically apply materials to the terrain.  Take a look at CSNOH Auto Material Simple, CSNOH Auto Material Rotation, and CSNOH Auto Material Hex.  They blend between materials using the Altitude Mask node and the Terrain Angle Mask node.




