# Components and Blends

### LayerBlends
**Blend Layers Alpha** - Blends two material layers using alpha blending. It can be used on its own to blend two layers by passing in the layer mask directly, or can be used in combination with the HeightMask node to create height blending.

### Components
**Debug UVs** - this node can be used to visualize the UVs that your shader is generating. If the DebugUVs boolean is true, it outputs the input UV coordinates as a red-green gradient instead of the input texture data.  If DebugUVs is false, it outputs the texture data instead. Because the DebugUVs boolean is a static value, the compiler should ignore these operations at runtime and only compile one branch or the other.

**Detail** - this node takes in material values from a regular terrain layer and combines them with a detail texture that uses the NOS [packing scheme](Shader-Graph-Sample-Terrain-Packing.md). It adds close-up, high frequency detail to a material and can be used in cases where large, individual pixels are visible in the base terrain because texture resolution isn’t high enough for the viewing distance.

**Detail CSNOH** - this subgraph node takes in material values from a CSNOH terrain layer and combines them with a detail texture that uses the NOS [packing scheme](Shader-Graph-Sample-Terrain-Packing.md). It adds close-up, high frequency detail to a material and can be used in cases where large, individual pixels are visible in the base terrain because texture resolution isn’t high enough for the viewing distance.

**Distance Fade** - this node is intended to smooth the transition from the full resolution terrain material to the more efficient low resolution composite terrain material. It fades out the hard lines that are visible in the Normal and Occlusion channels so that no seam is visible between the high and low resolution versions of the terrain material. With this node smoothing the transition, the Base Map Dist. can be kept lower - improving performance while erasing the seam at the same time.

**Terrain Angle Mask** - creates a black to white gradient based on the facing angle of the surface - whether it’s horizontal or vertical. The mask is white where the terrain is facing straight up (flat) and black where it’s steep.  The gradient can be adjusted using the MaskCenter and MaskContrast values.  The MaskCenter value moves the black point of the gradient and the MaskContrast smooths or sharpens the mask.

### Hex Tile Blending Components

**Blend With Triangle Weights** - this subgraph takes three texture values and combines them together using the weights input value. The R, G, and B input values are intended to use the R, G,  and B output values of either the Sample Triangle Texture2D subgraph or the Sample Triangle Normal2D subgraph, and the weights are intended to come from the Get Hex Grid Triangle weights output.

**Get Hex Grid Triangle** - used for hex grid tiling, this node produces a set of three hex grid UV values that can be plugged into the Grid Random3 node.  It also produces a set of red, green, and blue hex grid weights that can be passed into the Sampled Blended Triangle Texture2D subgraph or the Sample Blended Triangle Texture Normal2D and used to blend the three samples together. The HexSize input controls the size of the hex grid tiles and the Edge Contrast controls the focus or sharpness of the transition between the tiles. Lower values create larger, smoother transitions, and higher values create tighter, more focused transitions.

**Grid Random3** - used for hex grid tiling, this node takes in the three sets of hex uv coordinates produced by the Get Hex Grid Triangle node and uses them to generate three random vec3 hash values.  These values can then be passed into the random input of the Random Scale Offset UV node to create uv coordinates with random scale and offset values.

**Random Scale Offset Rotate UV** - takes in a set of UV coordinates and applies random scale, offset, and rotation transforms to them based on the random input value. The amount of scale and rotation can be controlled with the Scale Range and Rotation Range Degrees values.

**Random Scale OffsetUV** - takes in a set of UV coordinates and applies random scale and offset transforms to them based on the random input value. Random rotation can also be applied if the Use Rotation input is true.  The amount of scale and rotation can be controlled with the Scale Range and Rotation Range values.

**Rotate Normal CSNOH** - this subgraph node takes an NOH sample, or other texture sample that stores the normal X and Y in the red and green channels and rotates the normal using the given rotation value.  When UVs are randomly rotated, such as with hex grid tiling, the Normals sampled using those UVs must be “unrotated” so that lighting continues to work correctly.  This node does that unrotation of the normals.  The data in the blue and alpha channels is left unchanged.

**Rotate Normals** - this subgraph node takes a normal vector and rotates it using the given rotation value.  When UVs are randomly rotated, such as with hex grid tiling, the Normals sampled using those UVs must be “unrotated” so that lighting continues to work correctly.  This node does that unrotation of the normals.  The data in the blue and alpha channels is left unchanged.

**Sample Blended Triangle Normal2D** - used for hex grid tiling, this subgraph uses Sample Triangle Texture Normal2D to sample a normal map three times and then combines the samples using the Blend With Triangle Weights subgraph. The texture input should be a normal map asset from your terrain. The 3 uv inputs should be generated by three separate Random Scale Offset UV nodes, and the weights should come from the Get Hex Grid Triangle weights output.

**Sampled Blended Triangle Texture2D** - used for hex grid tiling, this subgraph uses Sample Triangle Texture2D to sample a texture map three times and then combines the samples using the Blend With Triangle Weights subgraph. The texture input should be a texture asset from your terrain - but not a normal map. The 3 uv inputs should be generated by three separate Random Scale Offset UV nodes, and the weights should come from the Get Hex Grid Triangle weights output.

**Sample Blended Triangle Texture2D Rotation** - this subgraph node does three samples of the texture using the Sample Triangle Texture2D node, unrotates the normals using the Rotate Normal CSNOH node, and then blends the samples together using the Blend With Triangle Weights node.

**SampleBlendedTriangleTextureRotation2DArray** - this subgraph node is designed to allow hex grid tiling technique to be used with array textures.  It does three samples of the array texture using the Sample Triangle Texture2D Array node, unrotates the normals using the Rotate Normal CSNOH node, and then blends the samples together using the Blend With Triangle Weights node.  This node is used by the CHNOS Array Hex shader.

**Sample Triangle Normal2D** - used for hex grid tiling, this subgraph samples a normal map three times given three different sets of UV coordinates.

**Sample Triangle Texture2D** - used for hex grid tiling, this subgraph samples a texture map three times given three different sets of UV coordinates.

**Sample Triangle Texture2D Array** - this subgraph node is used by the Sample Blended Triangle Texture2D Rotation Array node to do three samples of a texture array given three sets of UV coordinates. It is used in the process of hex grid tiling.  It is similar to Sample Triangle Texture2D - but has the addition of the ArrayIndex input which controls which slice of the texture array is sampled.

### Packing Components

**Pack Material** - combines the individual material components (Color, Normal, Metallic, Smoothness, Occlusion, and Height) into a single 4x4 matrix. While this is not strictly required, it does make it easier to pass around a full material definition with just a single wire.

**Unpack CSNOH** - unpacks the data in the CSNOH format into Color, Normal, Smoothness, Occlusion, and Height.  In order to save on performance, the normal’s Z is reconstructed by simply using a static value of 0.8 instead of doing the square root math.  The results are not strictly accurate, but are generally “good enough” for terrain textures and provide a decent performance improvement.  If you’d rather use the accurate method for reconstructing the normal, feel free to connect the included Normal Reconstruct Z node to the Normal output instead.

**Unpack Material** - receives a material definition 4x4 matrix (created with the Pack Material subgraph) and unpacks it to its individual material components (Color, Normal, Metallic, Smoothness, Occlusion, and Height).

**Unpack NOS** - this subgraph node is used to unpack texture data stored in the NOS (normal, occlusion, smoothness) format that is commonly used for detail maps.
