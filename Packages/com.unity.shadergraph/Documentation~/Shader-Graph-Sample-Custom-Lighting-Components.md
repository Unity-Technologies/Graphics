# Component Subgraphs

## Components

### Apply Decals
This node brings in data from projected decals in the scene. If the Decal component has not been added to the renderer, this node does nothing and does not add cost to the shader.

### Fog
This node adds fog to the object using the Fog parameters in the Environment tab of the Lighting window. It should only be used with the Unlit material type since the other material types automatically add fog. If you use this with anything but the Unlit Master Stack, you end up with double fog.

### Half Angle
This node computes the half angle, which is a vector that is half way between the View Direction vector and the Light Direction vector. It is often used to calculate specular highlights.

### MainLight
The MainLight node brings in the Direction, Color, and Shadow Attenuation of the main light source in the scene.

### SmoothnessToSpecPower
For lighting models that use specular power as an input (such as Blinn and Phong), this node converts a smoothness value into a specular power value.

## Additional Lights
The additional lights subgraphs compute the lighting contributions of the additional light sources in the scene. As this lighting data computation requires a `For` loop, these calculations must be done in code in the Custom Function node inside each subgraph.

### AdditionalLightsBasic
The AdditionalLightsBasic subgraph computes very simple lighting for the additional light sources. Basically just diffuse light with no specular.

### AdditionalLightsColorize
In addition to outputting the Diffuse, Specular, and Color components like other AdditionalLights nodes, this version also outputs the light attenuation separately so that it can be used outside the subgraph to calculate what areas should be colorized and what areas should be black and white.

### AdditionalLightsHalfLambert
This subgraph node computes the lighting for the additional lights in the scene using the Half Lambert formula which wraps the lighting gradient all the way around to the side of the model opposite the light source. Frequently, stylized lighting models use this type of lighting instead of standard Lambert diffuse.

### AdditionalLightsSimple
This subgraph node computes lighting using the Blinn formula for specular highlights. This makes the specular highlights cheaper to compute. Blinn specular is generally considered to be less realistic than the default specular that the Universal Render Pipeline uses.

### AdditionalLightsURP
This subgraph node computes the additional lighting using the same formula used by the Universal Render Pipeline by default. It provides the same type of lighting that you would get when using the standard Lit Master Stack.

## Ambient
### AmbientBasic
The AmbientBasic node computes a very simple ambient term using only the BakedGI node. It does not create reflections as many of the other ambient subgraphs do.

### AmbientStylized
The AmbientStylized node computes ambient using the Sky, Equator, and Ground colors that are defined in the Lighting window on the Environment tab. It also adds some ambient lighting for the sky using the normal up direction and a Fresnel term.

### AmbientURP
The AmbientURP node computes ambient light using the same formula as the standard URP lighting mode.

### SampleReflectionProbes
The SampleReflectionProbes node gets a color sample from the closest reflection probes and blends them together based on the position being rendered.

### ScreenSpaceAmbientOcclusion
The ScreenSpaceAmbientOcclusion node samples the ambient occlusion that is calculated by the Screen Space Ambient Occlusion component on the Renderer Data asset. If the SSAO component is not active on the Renderer, this node simply returns 1.  Typically, this value is multiplied by the ambient value so that ambient is darker in occluded areas, but you can use this data as you see fit in your own custom lighting model.

## Core Models
### LightBasic
The Lit Basic lighting model does very simple lighting and leaves out most lighting features in order to render as fast as possible. It calculates simple diffuse lighting and a simple form of ambient lighting. It does not support fog, reflections, specular, light cookies, or any other lighting features. But it does render fast and is ideal for low-end mobile devices and XR headsets.

### LightColorize
The Colorize lighting model is an example of the type of custom behavior you can create when you can control the lighting model. The main directional light renders the scene in grayscale with no color. Color is introduced with point lights so you can control where the scene has color based on where you place the point lights in the scene.

### LightSimple
The Lit Simple lighting model is the same as the URP lighting model, except it uses the Blinn formula for the specular highlights. This makes it slightly cheaper to render than standard URP while looking fairly similar. If you still need all of the lighting features (specular, fog, screen space ambient occlusion, reflections, etc), but you want to make the lighting cheaper, this may be a good choice.

### LightToon
The Lit Toon lighting model uses a Posterize operation to break the smooth lighting gradient into distinct bands of shading. It simulates the look of cartoons where lighting is rendered with distinct colors of paint rather than smooth gradients.

### LightURP
The Lit URP lighting model closely matches the lighting that the Universal Render Pipeline does by default. If you want to start with the URP lighting and then alter it, this is the node to use.

## Debug
### DebugLighting
When added to your lighting model (as seen in the existing examples), this subgraph provides support for the following debug lighting modes (available from the Rendering Debugger window):
* Shadow Cascades
* Lighting Without Normal Maps
* Lighting With Normal Maps
* Reflections
* Reflection With Smoothness
* Global Illumination

### DebugMaterials
When added to your lighting model (as seen in the existing examples), this subgraph provides support for the following debug material modes (available from the Rendering Debugger window):
* Albedo
* Specular
* Alpha
* Smoothness
* Ambient Occlusion
* Emission
* Normal World Space
* Normal Tangent Space
* Light Complexity
* Metallic
* Sprite Mask
* Rendering Layer Masks

## Diffuse
### DiffuseCustomGradient
Instead of a dot product calculation to generate the lighting gradient, this subgraph uses an input texture gradient. This allows you to paint the lighting as you see fit.

### DiffuseHalfLambert
The Half Lambert formula that this node uses creates a lighting gradient that goes from 0 to 1 from the dark side of the model to the bright side. Because the gradient wraps all the way around the model instead of just half way (as realistic lighting does), it has a softer, more stylized look.

### DiffuseLambert
This subgraph creates standard diffuse lighting using the dot product between the surface normal and the light vector. If the two vectors are pointing in the same direction, the surface has a diffuse value of 1. If the two vectors are perpendicular, the surface has a value of 0. The dark side of the model (facing away from the light) is clamped so that the result is also 0.

### DiffuseOrenNayar
This subgraph uses the Oren Nayar lighting formula which simulates the lighting response of a rough surface like clay or plaster. The math used by this formula is quite expensive when you compare it with the subtle results that it provides, so you may find it sufficient to just use DiffuseLambert instead.

## Reflectance
### ReflectancePBR
A subgraph that calculates reflectance using a standard physically-based model.

### ReflectanceURP
A subgraph that calculates reflectance using the same formula that URP uses.

## Specular
### SpecularBlinn
This subgraph calculates specular highlights using the Blinn formula. It’s cheaper than a more modern/realistic formula.

### SpecularCookTorrance
This subgraph calculates specular highlights using the Cook Torrance formula. This method creates specular that works well for brushed metal surfaces.

### SpecularPBR
This subgraph calculates specular highlights using the GGX formula, which is popular in modern PBR lighting models.

### SpecularStylized
This subgraph calculates specular highlights in a less realistic, more stylized way. This specular works better for an illustrative style.

### SpecularURP
This subgraph calculates specular highlights using the same formula as the lighting that the Universal Render Pipeline uses.
