# Layers

Layers control the application and behavior of individual material types to the terrain. In HDRP, you can use up to 8 layers with painted layer masks. In URP, you can use more, but each set of 4 requires an additional render pass. The main limitation for how many layers your terrain uses is performance.

Layers mainly control the type of tile break-up used, but can also contain effects such as parallax mapping, detail mapping, triplanar projection, etc.

Note that most of the samples in this example use the same type of layer for all of the layers in the shader.  This is not required, You can use a different layer type for each layer in the shader. It's most efficient to use a layer that specifically suits the needs of the texture set you're using and only use the more expensive layer types when needed.

**Layer Hex** - this terrain layer type uses a grid of tiled hexagons where each hex tile has a unique scale and offset applied.  This method allows the terrain textures to be tiled infinitely without any repetition.  It costs a bit more than some of the other techniques because each texture needs to be sampled three times, but it is the most effective method for removing terrain tiling artifacts.
- Requires **9** texture samples

**Layer Hex CSNOH** - this terrain layer type uses a grid of tiled hexagons where each hex tile has a unique scale and offset applied.  This method allows the terrain textures to be tiled infinitely without any repetition.  It costs a bit more than some of the other techniques (although this cost is somewhat reduced because we’re using CSNOH instead of CNM) because each texture needs to be sampled three times, but it is the most effective method for removing terrain tiling artifacts.
- Requires **6** texture samples

**Layer Hex Detail** - this terrain layer type is the same as Layer Hex, but also adds a detail texture so the base material has tighter detail blended on top. This is good for situations where the base textures need to be low resolution. The Detail Meters Per Tile value can be used to control the scale of the detail texture.
- Requires **10** texture samples

**Layer Hex Detail CSNOH** - this terrain layer type is the same as Layer Hex CSNOH, but also adds a detail texture so the base material has tighter detail blended on top. This is good for situations where the base textures need to be low resolution. The Detail Meters Per Tile value can be used to control the scale of the detail texture.
- Requires **7** texture samples

**Layer Hex Parallax CSNOH** - this terrain layer type is the same as Layer Hex CSNOH, but also adds parallax mapping. This makes the layer good at breaking up tiling and also gives a really nice sense of depth/height to the layer. However, the combination of both parallax and hex tile break-up increases the texture sample count by a LOT and makes this the most expensive layer type in the list. We recommend against using this, or if you need to use it, only use it for one layer and make the other layers  use cheaper options. (Depending on the Graphics Quality setting - High, Medium, or Low, this layer will use Parallax Occlusion Mapping, Parallax Mapping, or just normal mapping.)
- Uses a variable number of texture samples depending on the camera distance from the terrain and the angle between the camera vector and the terrain normal.

**Layer Parallax CSNOH** - this terrain layer type does parallax mapping, a technique for faking geometry tessellation and displacement with ray casting and multiple samples of each texture. Since we’re doing many texture samples, this technique can be quite expensive.  This node attempts to keep the number of samples required under control by using distance and angle masks - to do more samples when viewing terrain close up and at a shallow angle, but it can still be quite costly.  We recommend only using this type of layer for terrain materials that have large, bumpy shapes, and only on more powerful platforms. Note that we only include parallax mapping using the CSNOH texture packing scheme, and that it’s not offered using the standard CNM scheme.  This is because using parallax mapping with CNM would require so many texture samples that it’s not considered practical. (Depending on the Graphics Quality setting - High, Medium, or Low, this layer will use Parallax Occlusion Mapping, Parallax Mapping, or just normal mapping.)
- Uses a variable number of texture samples depending on the camera distance from the terrain and the angle between the camera vector and the terrain normal.

**Layer Rotation** - this terrain layer type breaks up tiling repetition artifacts by sampling each texture twice.  The second sample is rotated 90 degrees.  The two samples are then blended together using a mask texture. This method of tiling break-up is not as effective as the hex tile or tile break-up method, but it is cheaper since fewer texture samples are required.
- Requires **7** texture samples

**Layer Rotation CSNOH** - this terrain layer type breaks up tiling repetition artifacts by sampling each texture twice.  The second sample is rotated 90 degrees.  The two samples are then blended together using a mask texture. This method of tiling break-up is not as effective as the hex tile  or tile break-up method, but it is cheaper since fewer texture samples are required.
- Requires **5** texture samples

**Layer Side Project CSNOH** - this terrain layer type samples the textures from the sides, and front/back instead of from the top, like all of the other layer types. This makes it great for applying textures to the vertical surfaces of the terrain that would typically see stretching artifacts.
- Requires **4** texture samples

**Layer Simple** - this terrain layer type is intended to be as cheap as possible.  All it does is sample the three textures (color, normal, and mask), and unpack the results. This layer type is the best choice when performance is critical or when you don’t need tile break-up or any other additional features.
- Requires **3** texture samples

**Layer Simple CSNOH** - this terrain layer type is intended to be as cheap as possible.  All it does is sample the two textures (CS and NOH), and pass out the results. This layer type is the best choice when performance is critical or when you don’t need tile break-up or any other additional features.
- Requires **2** texture samples

**Layer Procedural** - this terrain layer type does tile break-up by sampling the textures twice and blending between them using a procedurally-generated pattern. It is a more expensive method than Rotation, but less expensive than hex tiling.
- Requires **6** texture samples

**Layer Procedural CSNOH** - this terrain layer type does tile break-up by sampling the textures twice and blending between them using a procedurally-generated pattern. It is a more expensive method than Rotation, but less expensive than hex tiling.
- Requires **4** texture samples

**Layer Triplanar** - this terrain layer type projects the terrain textures from the top, front, and side. It’s intended to be used on steep areas of terrain.  All of the other layer types use UV projection which is applied from the top down.  This means that there are stretching artifacts on steep areas of the terrain. This layer type overcomes that by projecting from the front and side in areas where the terrain is steep.   Each texture is sampled three times (top, front, and side) so it costs more than some of the other layer types. It should only be used for cliffs or other materials intended to be applied to steep portions of the terrain.  Using this layer type on relatively flat terrain would be a waste.
- Requires **9** texture samples


