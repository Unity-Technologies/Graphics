# Texture Packing Schemes

Texture sampling is one of the most expensive operations that shaders do, in terms of performance. If you manage to reduce the number of texture samples performed in the shader, you can significantly boost the [shader’s overall performance](Shader-Graph-Sample-Terrain-Performance.md). One way of reducing texture samples is to pack multiple types of data together into a single texture.  Here are the various ways that is done in this sample content.

**CNM** - this is the texture packing scheme that's most commonly used in Unity. It uses three textures to define a complete material - color, normal, and masks.  The mask texture is packed as follows: 
* Red: metal
* Green: occlusion
* Blue: height
* Alpha: smoothness (MOHS).

This is the standard texture packing scheme most commonly used by Unity.

**CSNOH** - a texture packing scheme that uses two textures to define a complete material - CS, and NOH.  The CS texture is color with smoothness in the alpha channel.  The NOH texture is packed as follows:
* Red: normal X
* Green: normal Y
* Blue: occlusion
* Alpha: height

This packing scheme does not support a metallic mask as metal is very rarely needed for terrain materials. Using two textures as a material definition instead of three significantly reduces the performance cost of the terrain shaders. With CNM, a typical terrain shader with four layers does 12 texture samples, but with CSNOH, we cut that sample count down to 8. And using CSNOH is generally about 30% faster (in our testing) than using CNM because of the reduced sample counts.

**NOS** - a texture packing scheme designed specifically for detail textures - which add additional, close-up detail to existing materials. The data is packed as follows:
* Red: normal X
* Green: normal Y
* Blue: occlusion
* Alpha: smoothness

The smoothness data is centered at 0.5 and blended as an overlay so that values above 0.5 increase the base smoothness and values below 0.5 reduce it. This packing scheme is intended to optimize cost-effectiveness for a single texture sample, to provide detail additions to color, normal, smoothness, and occlusion from a single texture.

**CHNOSArray** - this packing scheme is designed to maximize texture sample reduction by packing the data from multiple materials into a single array texture. For each terrain material, the color and height data are stored in individual CH textures (color in RGB, height in the alpha channel). But the normal, occlusion, and smoothness for all of the materials on the terrain are packed into a single array texture.  With CNM, a typical terrain shader with four layers does 12 texture samples, but with CHNOSArray, we cut that sample count down to just 5 (4 for the color, and 1 for everything else). Also, it gets even more efficient the more layers you add. For example, for 8 layers, it would cost 9 samples for CHNOSArray, as opposed to 24 samples for CNM, a saving of 63%. The main issue with using array textures is that the borders between material types are binary with no blending. We fix this using dithering, which means you may see some pixelation on the borders between materials, but this is better than hard edges.