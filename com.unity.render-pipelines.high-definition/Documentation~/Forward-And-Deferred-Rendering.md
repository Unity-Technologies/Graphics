# Forward and Deferred rendering

When using a Lit Shader In HDRP, you can set each Material to use Forward or Deferred rendering. To do this, enable them in the HDRP Asset using the __Lit Shader Mode__ property. 

![](Images/ForwardAndDeferred1.png)

You can set the __Lit Shader Mode__ to:

* __Forward__: For Materials that use __Forward__ rendering, HDRP calculates the lighting in a single pass when rendering each individual Material. 

* __Deferred__: For Materials that use __Deferred__ rendering, HDRP renders them all into a GBuffer that stores the Material properties visible on the screen. After it renders all deferred GameObjects to the GBuffer, HDRP then processes the lighting for every GameObject in the Scene.

* __Both__: When you select __Both__, you can change between __Forward__ and __Deferred__ rendering mode on a per Camera and Reflection Probe basis at run time by using the [Frame Settings](Frame-Settings.html). For example, you can use Forward mode for a Planar Reflection Probe and then render your main Camera using Deferred mode. However, this increases Project build time (see [Build time](#BuildTime)).

To decide whether to use Forward or Deferred mode, consider the level of quality and performance you want for your Project. Deferred rendering in HDRP is faster in most scenarios, such as a Scene with various Materials and multiple local Lights. Some scenarios, like those with a single Directional Light in a Scene, can be faster in Forward rendering mode. If performance is not so important for your Project, use Forward rendering mode for better quality rendering.

HDRP forces Forward rendering for the following types of Shaders: 

* Fabric

* Hair

* AxF

* StackLit

* Unlit

* Lit shader with a Transparent Surface Type

If you set the __Lit Shader Mode__ to __Deferred__ in your HDRP Asset, HDRP uses deferred rendering to render all Materials that use an Opaque Lit Material.

Forward and Deferred rendering both implement the same features, but the quality can differ between them. This means that HDRP works with all features for whichever __Lit Shader Mode __you select. For example, Screen Space Reflection, Screen Space Ambient Occlusion, Decals, and Contact Shadows work with a Deferred or Forward __Lit Shader Mode__. Although feature parity is core to HDRP, the quality and accuracy of these effects may vary between __Lit Shader Modes__ due to technical restraints.

## Differences between Forward and Deferred rendering in HDRP

### Visual differences

* Normal shadow bias: In Forward mode, HDRP uses the geometric normal (the vertex normal) of the Material for shadow bias. This results in less shadow artifacts compared to the pixel normal that Deferred mode uses.

* Emissive Color: In Deferred mode, due to technical constraints, Ambient Occlusion affects Emissive Color. This is not the Case in Forward mode.

* Ambient Occlusion: In Deferred mode, HDRP applies Ambient Occlusion on indirect diffuse lighting (Ligthmaps and Light Probes) as well as the Screen Space Ambient Occlusion effect. This results in double darkening. In Forward mode, HDRP applies the minimum out of Ambient Occlusion and Screen Space Ambient Occlusion. This results in correct darkening.

* Material Quality: In Deferred mode, HDRP compresses Material properties, such as normals or tangents, in the GBuffer. This results in compression artifacts. In Forward mode, there is no compression, meaning that there are no compression artifacts.

### Technical differences

* For Materials that use Forward mode, HDRP always renders a depth prepass, which outputs a depth and a normal buffer. This is optional in Deferred Mode, if you are not using Decals.

* For Forward mode, HDRP updates normal buffers after the decal DBuffer pass. HDRP uses the normal buffer for Screen Space Reflection and other effects.



<a name="BuildTime"></a>

## Build time

The build time for an HDRP Project may be faster using a specific rendering mode, either Forward or Deferred. The downside of choosing Both is that it increases the built time for the Player substantially because Unity builds two sets of Shaders for each Material, one for Forward and one for Deferred. If you use a specific rendering mode for everything in your Project, you should use that rendering mode instead of Both, to reduce build time. This also reduces the memory size that HDRP allocates for Shaders.

