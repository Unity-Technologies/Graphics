**Important:** This page is subject to change during the 2019.1 beta cycle.

# Particles Lit Shader

In the Universal Render Pipeline (URP), use this Shader to make particles appear almost photorealistic, for example for camp fire particles, rain drops or torch smoke. This Shader produces lifelike visuals but uses the most computationally heavy [shading model](shading-model.md) in URP, which can impact performance.

## Using the Particles Lit Shader in the Editor

To select and use this Shader:

1. In your Project, create or find the Material you want to use the Shader on.  Select the __Material__. A Material Inspector window opens.
2. Click __Shader__, and select __Universal Render Pipeline__ > __Particles__ > __Lit__.

## UI overview

The Inspector window for this Shader contains these elements:

- __[Surface Options](#surface-options)__
- __[Surface Inputs](#surface-inputs)__
- __[Advanced](#advanced)__

![Inspector for the Particles Lit Shader](Images/Inspectors/Shaders/ParticlesLit.png)

### Surface Options

The __Surface Options__ control how URP renders the Material on a screen.

| Property           | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| __Surface Type__   | Use this drop-down to apply an __Opaque__ or __Transparent__ surface type to the Material. This determines which render pass URP renders the Material in. __Opaque__ surface types are always fully visible, regardless of what’s behind them. URP renders opaque Materials first. __Transparent__ surface types are affected by their background, and they can vary according to which type of transparent surface type you choose. URP renders transparent Materials in a separate pass after opaque Materials.  If you select __Transparent__, the __Blending Mode__ drop-down appears. |
| __Blending Mode__   | Select how Unity calculates the color of each pixel of a transparent Material when it blends the Material with the background.<br/><br/>In the context of Blending Modes, Source refers to the transparent Material where the Blending Mode is set and Destination refers to anything that Material overlaps with. |
|&#160;&#160;&#160;&#160;Alpha | ![Alpha blending mode example](./Images/blend-modes/blend-mode-alpha.png)<br/>*Alpha blending mode.*<br/><br/>__Alpha__ uses the Material's alpha value to change how transparent an object is. 0 is fully transparent. 255 is fully opaque, this is translated to a value of 1 when used with the blending equations. The Material is always rendered in the Transparent render pass regardless of it's alpha value.<br/>This mode lets you use the [Preserve Specular Lighting](#preserve-specular) property.<br/><br/>Alpha equation:<br/>*OutputRGBA* = (*SourceRGB* &#215; *SourceAlpha*) + *DestinationRGB* &#215; (1 &#8722; *SourceAlpha*) |
|&#160;&#160;&#160;&#160;Premultiply | ![Premultiply blending mode example](./Images/blend-modes/blend-mode-premultiply.png)<br/>*Premultiply blending mode.*<br/><br/>__Premultiply__ first multiplies the RGB values of the transparent Material by its alpha value then applies a similar effect to the Material as __Alpha__.<br/>The equation for __Premultiply__ also allows areas of the transparent Material with an alpha value of 0 to have an additive blend effect. This can help reduce artifacts that may appear at the edge of the overlap between opaque and transparent pixels.<br/><br/>Premultiply equation:<br/>*OutputRGBA* = *SourceRGB* + *DestinationRGB* &#215; (1 &#8722; *SourceAlpha*) |
|&#160;&#160;&#160;&#160;Additive | ![Additive blending mode example](./Images/blend-modes/blend-mode-additive.png)<br/>*Additive blending mode.*<br/><br/>__Additive__ adds the color values of the Materials together to create the blend effect. The alpha value determines the strength of the source Material's color before the blend is calculated.<br/>This mode lets you use the [Preserve Specular Lighting](#preserve-specular) property.<br/><br/>Additive equation:<br/>*OutputRGBA* = (*SourceRGB* &#215; *SourceAlpha*) + *DestinationRGB* |
|&#160;&#160;&#160;&#160;Multiply | ![Multiply blending mode example](./Images/blend-modes/blend-mode-multiply.png)<br/>*Multiply blending mode.*<br/><br/>__Multiply__ multiplies the color of the Material with the color behind the surface. This creates a darker effect, like when you look through colored glass.<br/>This mode uses the Material’s alpha value to adjust how much the colors blend. An alpha value of 1 results in unadjusted multiplication of the colors while lower values blend the colors towards white.<br/><br/>Multiply equation:<br/>*OutputRGBA* = *SourceRGB* &#215; *DestinationRGB* <a name="preserve-specular"></a> |
| __Preserve Specular Lighting__ | Indicates whether Unity preserves specular highlights on a GameObject or not. This applies even when your surface is transparent which means that only the reflected light is visible.<br/><br/>This property only appears when the __Surface Type__ is set to Transparent and __Blending Mode__ is set to either Alpha or Additive.<br/><br/>![Material with Preserve Specular Lighting disabled](./Images/blend-modes/preserve-specular-lighting-off.png)<br/>*Material with __Preserve Specular Lighting__ disabled.*<br/><br/>![Material with Preserve Specular Lighting enabled](./Images/blend-modes/preserve-specular-lighting-on.png)<br/>*Material with __Preserve Specular Lighting__ enabled.* |
| __Render Face__    | Use this drop-down to determine which sides of your geometry to render.<br/>__Front Face__ renders the front face of your geometry and [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the back face. This is the default setting. <br/>__Back Face__ renders the front face of your geometry and culls the front face. <br/>__Both__ makes URP render both faces of the geometry. This is good for small, flat objects, like leaves, where you might want both sides visible. |
| __Alpha Clipping__ | Makes your Material act like a [Cutout](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) Shader. Use this to create a transparent effect with hard edges between the opaque and transparent areas. For example, to create blades of grass. To achieve this effect, URP does not render alpha values below the specified __Threshold__, which appears when you enable __Alpha Clipping__.  You can set the __Threshold__ by moving the slider, which accepts values from 0 to 1. All values above your threshold are fully opaque, and all values below your threshold are invisible. For example, a threshold of 0.1 means that URP doesn't render alpha values below 0.1. The default value is 0.5. |
| __Color Mode__     | Use this drop-down to determine how the particle color and the Material color blend together.<br/> __Multiply__ produces a darker final color by multiplying the two colors.<br/>__Additive__ produces a brighter final colour by adding the two colours together.</br/>__Subtractive__ subtracts the particle color from the base color of the Material. This creates an overall dark effect in the pixel itself, with less brightness.<br/>__Overlay__ blends the particle color over the base color of the Material. This creates a brighter color at values over 0.5 and darker colors at values under 0.5.<br/>__Color__ uses the particle color to colorize the Material color, while keeping the value and saturation of the base color of the Material. This is good for adding splashes of color to monochrome Scenes.<br/>__Difference__ returns the difference between both color values. This is good for blending particle and Material colors that are similar to each other. |

### Surface Inputs

The __Surface Inputs__ describe the surface itself. For example, you can use these properties to make your surface look wet, dry, rough, or smooth.


| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| __Base Map__     | Adds color to the surface. To assign a Texture to the __Base Map__ setting, click the object picker next to it. This opens the Asset Browser, where you can select from the Textures in your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). The color next to the setting shows the tint on top of your assigned Texture. To assign another tint, you can click this color swatch. If you select __Transparent__ or __Alpha Clipping__ under __Surface Options__, your Material uses the Texture’s alpha channel or color. The Base Map is also known as a diffuse map. |
| __Metallic Map__ | Shows the map input for the metallic highlights and reflections from direct lighting, for example [Directional, Point, and Spot lights](https://docs.unity3d.com/Manual/Lighting.html). <br/> You can use the __Smoothness__ slider to control the spread of highlights on the surface. 0 gives a wide, rough highlight. 1 gives a small, sharp highlight like glass. Values in between 0 and 1 produce semi-glossy looks. For example, 0.5 produces a plastic-like glossiness. |
| __Normal Map__   | Adds a normal map to the surface. With a [normal map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html), you can add surface details like bumps, scratches and grooves. To add the map, click the object picker next to it. The normal map picks up ambient lighting in the environment.<br/>The float value next to the setting is a multiplier for the effect of the  __Normal Map__. Low values decrease the effect of the normal map. High values create stronger effects. |
| __Emission__     | Makes the surface look like it emits lights. When enabled, the  __Emission Map__ and __Emission Color__ settings appear.<br/>To assign an __Emission Map__, click the object picture next to it. This opens the Asset Browser, where you can select from the textures in your Project.<br/>For __Emission Color__, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html) to assign a tint on top of the color. This can be more than 100% white, which is useful for effects like lava, that shines brighter than white while still being another color.<br/>If you have not assigned an __Emission Map__, the __Emission__ setting only uses the tint you’ve assigned in __Emission Color__.<br/>If you do not enable __Emission__, URP sets the emission to black and does not calculate emission. |



### Advanced

The __Advanced__ settings affect behind-the-scenes rendering. They do not have a visible effect on your surface, but on underlying calculations that impact performance.

| Property               | Description                                                  |
| ---------------------- | ------------------------------------------------------------ |
| __Flip-Book Blending__ | Tick this box to blend flip-book frames together. This is useful in texture sheet animations with limited frames, because it makes animations smoother. If you have performance issues, try turning this off. |
| __Vertex Streams__     | This list shows the vertex streams that this Material requires in order to work properly. If the vertex streams aren’t correctly assigned, the __Fix Now__ button appears. Click this button to apply the correct setup of vertex streams to the Particle System that this Material is assigned to. |
| __Sorting Priority__   | Use this slider to determine the chronological rendering order for a Material. URP renders Materials with lower values first. You can use this to reduce overdraw on devices by making the pipeline render Materials in front of other Materials first, so it doesn't have to render overlapping areas twice. This works similarly to the [render queue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html) in the built-in Unity render pipeline. |


#### Transparent surface type

If you’ve chosen a Transparent surface type under [Surface Options](#surface-options), these options appear:

![Additional Particle options](Images/Inspectors/Shaders/ParticlesExtra.png)

| Property           | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| __Soft Particles__ | Tick this box to make particles fade out when they get close to intersecting with the surface of other geometry written into the [depth buffer](https://docs.unity3d.com/Manual/class-RenderTexture.html).<br/>When you enable this feature, the **Surface Fade** settings appear:<br/>__Near__ sets the distance from the other surface where the particle is completely transparent. This is where the particle appears to fade out completely.<br/>__Far__ sets the distance from the other surface where the particle is completely opaque. The particle appears solid here.<br/>Distances are measured in world units. Only usable for transparent surface types.<br/><br/>**Note:** This setting uses the `CameraDepthTexture` that is created by URP. To use this setting, enable __Depth Texture__ in the [URP Asset](universalrp-asset.md) or for the [Camera](camera-component-reference.md) that is rendering the particles. |
| __Camera Fading__  | Tick this box to make particles fade out when they get close to the camera.<br/>When you enable this feature, the __Distance__ settings appear:<br/>__Near__ sets the distance from the camera where the particle is completely transparent. This is where the particle appears to fade out completely.<br/>__Far__ sets the distance from the camera where the particle is completely opaque. The particle appears solid here.<br/>Distances are measured in world units. <br/><br/>**Note:** This uses the `CameraDepthTexture` that is created by URP. To use this setting, enable __Depth Texture__ in the [URP Asset](universalrp-asset.md) or for the [Camera](camera-component-reference.md) that is rendering the particles. |
| __Distortion__     | Creates a distortion effect by making particles perform refraction with the objects drawn before them. This is useful for creating a heat wave effect or for warping objects behind the particles. <br/>When you enable this feature, these settings appear:<br/>__Strength__ controls how much the Particle distorts the background. Negative values have the opposite effect of positive values. So if something was offset to the right with a positive value, the equal negative value offsets it to the left.<br/>__Blend__ controls how visible the distortion is. At 0, there is no visible distortion. At 1, only the distortion effect is visible.<br/><br/>**Note:** This uses the `CameraOpaqueTexture` that is created by URP. To use this setting, enable __Opaque Texture__ in the [URP Asset](universalrp-asset.md) or for the [Camera](camera-component-reference.md) that is rendering the particles. |
