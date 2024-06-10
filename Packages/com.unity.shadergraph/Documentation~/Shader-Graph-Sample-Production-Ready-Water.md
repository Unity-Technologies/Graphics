# Water
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