# Custom Lighting in 2D

The default lighting model in 2D renderer is meant for generic use and was design to provide some level flexibility.

However, it is not infinitely flexible and may not be able to meet the needs for more custom or advance effects.

You can now make your own 2D Lighting model.

## Sprite Custom Lit Shader Graph

The new Shader Graph target "Custom Lit Shader Graph" provides a great starting point to create a custom lighting model shader. It does not sample the Light Textures but it does have a Normal pass and a fallback Forward pass for use in non 2D Renderer.

## 2D Light Texture
2D Light Textures are Render Textures created by the 2D Renderer that contain the visible lights in the scene. There are up to 4 textures each representing a blend style in the [2D Renderer Data](2DRendererData_overview.md)

The built in Lit shaders will sample these textures and combined them with the Sprite's textures to create the lighting effect.

## 2D Light Texture Node
To sample the Light Texture use the new "2D Light Texture" node in Shader Graph. The output of the node is the same as the output of a "Texture 2D" and should be fed into a "Texture Sampler".

# Creating the Emissive Effect with Custom Lit Shader
The emissive effect is the perfect example of utilizing the Custom Lit Shader to create a custom effect. By combining the a mask texture to identify areas of the Sprite that should not receive lighting effect.

The "Secondary Texture" feature is a great way to load the emissive mask.
