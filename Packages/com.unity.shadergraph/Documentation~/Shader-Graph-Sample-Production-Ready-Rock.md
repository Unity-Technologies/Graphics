# Rock
This is a full-featured, modular rock shader that can be used for everything from small pebbles to boulders up to large cliff faces. It has features that can be turned on and off in the material depending on the application. Each of the features is encapsulated in a subgraph so it’s easy to remove features that you don’t need.  You can also add new features in the chain of modules if you need something else. Each module takes in color and smoothness in one input port and normal and ambient occlusion in a second input. Inside the subgraph, it alters these, and then it outputs the result again in the same format - color and smoothness in the first output port, and normal and AO in the second. Using this input/output port format keeps all of the modules organized and in a nice, neat line.

To help with performance, the shader has an LOD0 boolean parameter exposed to the material. For materials applied to LOD0 of your rocks, this should be true.  For materials applied to the other LODs, this boolean should be false. This feature turns off extra features that are only visible when the rock is close so that non-LOD0 versions of your rocks render faster.

Additionally, this shader branches using the Material Quality built-in enum keyword. This means that the shader is already set up to create a low quality, medium quality, and high quality version of itself depending on project settings. Features will be turned on and off, or different variations of features will be used depending on the project’s Material Quality setting.

#### Base Textures
In order to reduce the total number of texture samples in the shader (sampling textures is the most expensive operation that shaders do, so reducing the number of texture samples can significantly improve shader performance), we’ve used a two-texture format for our base textures instead of 3.  The format is as follows:

* **CS Texture** (BC7 format) - RGB - color, Alpha - smoothness
* **NO Texture** (BC7 format) - RGB - normal, Alpha - ambient occlusion

[!NOTE] 
<The NO texture is NOT saved as a normal map. If you set it to be a normal map, the ambient occlusion data in the alpha channel will be lost.>

#### Macro Detail
The purpose of the Macro Detail module is to add large-scale details to the color, smoothness, normal, and ambient occlusion of the rock’s base material. For rocks that are large, a single texture set is often not high enough resolution and the base textures look blurry or blocky, even from a distance. The Macro Detail module solves this problem. For rocks that are smaller than 1 meter cubed, this feature should be turned off by unchecking Rock Features/MarcoDetail in the rock’s material.

This feature references a texture - Rock_Macro_NOS - which you are welcome to use, or you can create your own.  The texture format is as follows:

* R: normal X
* G: normal Y
* B: ambient occlusion
* A: smoothness/color overlay

This texture needs to be set to Default 2D format with Compression set to High Quality. It is recommended to just use one single macro detail texture for all of the rocks in your project both to save on texture memory and to maintain a consistent visual style.  For this reason, the texture itself is not exposed as a material parameter but is set directly in the shader.
#### Color Projection
For large boulders and cliff faces, you may want to add colored effects to the rocks such as bleaching. The Color Projection module can handle this. It projects color alterations using world space. The nice thing about this effect is that if your rock formation is made up of multiple rocks all jammed together, the color projection will tie them together and make them feel more cohesive - as if they’re one unified formation rather than just a collection of jammed-together rocks.

This effect does use 5 texture samples, so if you don’t need it, or if you’re on a very performance sensitive platform such as a mobile device, you should definitely turn it off in the material to improve performance.
#### Micro Detail
The purpose of the Micro Detail module is to add small-scale details to the color, smoothness, normal, and ambient occlusion of the rock’s base material.  When you get really close to the rocks, sometimes the resolution of the base textures is not high enough and they look blurry or blocky. The Micro Detail module solves this problem by adding very high resolution micro detail to the rock surface.

This feature references a texture - Rock_Micro_NOS - which you are welcome to use, or you can create your own.  The texture format is as follows:

* R: normal X
* G: normal Y
* B: ambient occlusion
* A: smoothness/color overlay

This texture needs to be set to Default 2D format with Compression set to High Quality. It is recommended to just use one single micro detail texture for all of the rocks in your project both to save on texture memory and to maintain a consistent visual style.  For this reason, the texture itself is not exposed as a material parameter but is set directly in the shader.
#### Deposition Moss
The Deposition Moss module applies moss to the tops of the rocks. To define the moss, it uses a Moss_CO texture (color with occlusion in the alpha channel) and a Moss_N texture (normal). The alpha channel of the Moss_CO texture is also used to create smoothness.

It’s also possible to use this module to apply other types of materials to the tops of the rocks - such as sand, ash, snow, etc. To do that, you’d just need to set the Deposition Moss module to use textures for your chosen material instead.  These textures are not exposed to the material, but they are available to be changed on the module.
#### Rain
When the IsRaining parameter is set to 1, the Rain module applies rain effects to the rocks, including animated rain drops on the tops of the rocks, and drips running down the sides of the rocks.  The module also makes the rocks look wet.