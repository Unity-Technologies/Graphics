Test starting with 81__ are HDRP_ShaderGraph_TestScene(s), which means that it will make two image comparisons. One of Shader Graph objects, one of HDRP/Lit objects that the Shader Graph objects match. The reason is to ensure parity between SG & HDRP.

In these scenes, you can switch between Shader Graph & HDRP/Lit comparison objects to see the difference faster. To do so start the 81__ scene, and press any key to have the instructions come up. (Space to switch between objects).

8101_Opaque is a condensed scene, it will test for:

- All Surface Types
	> Standard
	> Specular Color
	> Iridescence
	> Subsurface Scattering
	> Translucent
	> Anistropy
- Decals
- Double Sided
- Geometric Specular AA

8102_Transparent is a condensed scene, it will test for:

- Blend Modes
- Fog
- Refraction
- Back Then Front Rendering
- Distortion (Temporarily broken / removed)