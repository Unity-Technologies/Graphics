# Component sub graph reference

Explore the Component sub graphs used as building blocks for the [Lighting Model sub graphs](Shader-Graph-Sample-Custom-Lighting-Lighting-Models.md) included in the Custom Lighting sample.

These sub graphs are available in the [**Create Node** menu](Create-Node-Menu.md) under the **Lighting** > **Components** section.

## General

| **Sub graph** | **Description** |
| :--- | :--- |
| **Apply Decals** | Brings in data from projected decals in the scene. If the Decal component has not been added to the renderer, this node does nothing and does not add cost to the shader. |
| **Fog** | Adds fog to the object using the Fog parameters in the Environment tab of the Lighting window. It should only be used with the Unlit material type since the other material types automatically add fog. If you use this with anything but the Unlit Master Stack, you end up with double fog. |
| **Half Angle** | Computes the half angle, which is a vector that is halfway between the View Direction vector and the Light Direction vector. It is often used to calculate specular highlights. |
| **MainLight** | Brings in the Direction, Color, and Shadow Attenuation of the main light source in the scene. |
| **SmoothnessToSpecPower** | Converts a smoothness value into a specular power value, for lighting models that use specular power as an input (such as Blinn and Phong). |

## Additional Lights

The Additional Lights sub graphs compute the lighting contributions of the additional light sources in the scene. As this lighting data computation requires a `For` loop, these calculations must be done in code in the Custom Function node inside each sub graph.

| **Sub graph** | **Description** |
| :--- | :--- |
| **AdditionalLightsBasic** | Computes very simple lighting for the additional light sources. It only includes diffuse light with no specular reflection. |
| **AdditionalLightsColorize** | Outputs the Diffuse, Specular, and Color components, and also outputs the light attenuation separately to allow to use it outside the sub graph to calculate what areas should be colorized and what areas should be black and white. |
| **AdditionalLightsHalfLambert** | Computes the lighting for the additional lights in the scene using the Half Lambert formula which wraps the lighting gradient all the way around to the side of the model opposite the light source. |
| **AdditionalLightsSimple** | Computes lighting using the Blinn formula for specular highlights. This makes the specular highlights cheaper to compute. Blinn specular is generally considered to be less realistic than the default specular that the Universal Render Pipeline uses. |
| **AdditionalLightsURP** | Computes the additional lighting using the same formula used by the Universal Render Pipeline by default. It provides the same type of lighting that you would get when using the standard Lit Master Stack. |

## Ambient

| **Sub graph** | **Description** |
| :--- | :--- |
| **AmbientBasic** | Computes a very simple ambient lighting using only the BakedGI node. It doesn't create any reflections. |
| **AmbientStylized** | Computes ambient lighting using the Sky, Equator, and Ground colors that are defined in the Lighting window on the Environment tab. It also adds some ambient lighting for the sky using the normal up direction and a Fresnel term. |
| **AmbientURP** | Computes ambient light using the same formula as the standard URP lighting mode. |
| **SampleReflectionProbes** | Gets a color sample from the closest reflection probes and blends them together based on the position being rendered. |
| **ScreenSpaceAmbientOcclusion** | Samples the ambient occlusion that is calculated by the Screen Space Ambient Occlusion component on the Renderer Data asset. If the SSAO component is not active on the Renderer, this node simply returns 1. Typically, this value is multiplied by the ambient value so that ambient is darker in occluded areas, but you can use this data as you see fit in your own custom lighting model. |

## Core Models

| **Sub graph** | **Description** |
| :--- | :--- |
| **LightBasic** | Computes very simple lighting and leaves out most lighting features in order to render as fast as possible. It calculates simple diffuse lighting and a simple form of ambient lighting. It does not support fog, reflections, specular, light cookies, or any other lighting features. But it does render fast and is ideal for low-end mobile devices and XR headsets. |
| **LightColorize** | Example of the type of custom behavior you can create when you can control the lighting model. The main directional light renders the scene in grayscale with no color. Color is introduced with point lights so you can control where the scene has color based on where you place the point lights in the scene. |
| **LightCookTorrance** | Combines Lambert diffuse lighting and Cook-Torrance specular highlights with URP ambient lighting, reflections, and fog. It calculates lighting from the main light only. |
| **LightNoir** | Computes simple diffuse lighting from the main and additional lights, plus basic ambient lighting. It then removes all color, increases the contrast, and remaps the result through a gradient to create a high-contrast black-and-white look, similar to film noir. |
| **LightOrenNayar** | Combines Oren-Nayar diffuse lighting with URP ambient lighting, reflections, and fog. It doesn't calculate specular highlights. It calculates lighting from the main light only. |
| **LightSimple** | Same as the URP lighting model, except it uses the Blinn formula for the specular highlights. The Blinn formula makes it slightly cheaper to render than standard URP while looking fairly similar. If you need the full set of lighting features, including specular, fog, screen space ambient occlusion, and reflections, but want cheaper lighting, use this lighting model. |
| **LightStylized** | Combines custom-gradient diffuse lighting, stylized specular highlights, and stylized ambient lighting with fog, for an illustrative look. Use the **Specular** and **Exponent** inputs to control the highlights. It calculates lighting from the main light only. |
| **LightToon** | Uses a Posterize operation to break the smooth lighting gradient into distinct bands of shading. It simulates the look of cartoons where lighting is rendered with distinct colors of paint rather than smooth gradients. |
| **LightURP** | Closely matches the lighting that the Universal Render Pipeline does by default. If you want to start with the URP lighting and then alter it, this is the node to use. |

## Debug

| **Sub graph** | **Description** |
| :--- | :--- |
| **DebugLighting** | Provides support for the following lighting debug modes (available from the [Rendering Debugger window](xref:urp-rendering-debugger-reference)):<ul><li>Shadow Cascades</li><li>Lighting Without Normal Maps</li><li>Lighting With Normal Maps</li><li>Reflections</li><li>Reflection With Smoothness</li><li>Global Illumination</li></ul> |
| **DebugMaterials** | Provides support for the following material debug modes (available from the [Rendering Debugger window](xref:urp-rendering-debugger-reference)):<ul><li>Albedo</li><li>Specular</li><li>Alpha</li><li>Smoothness</li><li>Ambient Occlusion</li><li>Emission</li><li>Normal World Space</li><li>Normal Tangent Space</li><li>Light Complexity</li><li>Metallic</li><li>Sprite Mask</li><li>Rendering Layer Masks</li></ul> |

## Diffuse

| **Sub graph** | **Description** |
| :--- | :--- |
| **DiffuseCustomGradient** | Uses an input texture gradient instead of a dot product calculation to generate the lighting gradient. This allows you to paint the lighting as you see fit. |
| **DiffuseHalfLambert** | Uses a Half Lambert formula that creates a lighting gradient that goes from 0 to 1 from the dark side to the bright side of the model. Because the gradient wraps all the way around the model instead of just halfway (as realistic lighting does), it has a softer, more stylized look. |
| **DiffuseLambert** | Creates standard diffuse lighting using the dot product between the surface normal and the light vector. If the two vectors are pointing in the same direction, the surface has a diffuse value of 1. If the two vectors are perpendicular, the surface has a value of 0. The dark side of the model (facing away from the light) is clamped so that the result is also 0. |
| **DiffuseOrenNayar** | Uses the Oren-Nayar lighting formula which simulates the lighting response of a rough surface like clay or plaster. The math used by this formula is quite expensive when you compare it with the subtle results that it provides, so you may find it sufficient to just use DiffuseLambert instead. |

## Reflectance

| **Sub graph** | **Description** |
| :--- | :--- |
| **ReflectancePBR** | Calculates reflectance using a standard physically-based model. |
| **ReflectanceURP** | Calculates reflectance using the same formula that URP uses. |

## Specular

| **Sub graph** | **Description** |
| :--- | :--- |
| **SpecularBlinn** | Calculates specular highlights using the Blinn formula. It’s cheaper than a more modern/realistic formula. |
| **SpecularCookTorrence** | Calculates specular highlights using the Cook-Torrance formula. This method creates specular highlights that work well for brushed metal surfaces. |
| **SpecularPBR** | Calculates specular highlights using the GGX formula, which is popular in modern PBR lighting models. |
| **SpecularStylized** | Calculates specular highlights in a less realistic, more stylized way. This specular works better for an illustrative style. |
| **SpecularURP** | Calculates specular highlights using the same formula as the lighting that the Universal Render Pipeline uses. |

## Specular terms

These sub graphs calculate the individual terms of the specular model that the **SpecularPBR** sub graph uses. They're available in the **Create Node** menu under **Lighting** > **Components** > **Specular** > **PBR**.

| **Sub graph** | **Description** |
| :--- | :--- |
| **NormalDistributionTrowReitz** | Calculates the normal distribution term using the Trowbridge-Reitz (GGX) formula. The normal distribution term is the proportion of the surface's microfacets (the tiny details that make up a surface) that face the right way to reflect light toward the viewer. It takes the **roughSqr** (roughness squared) and **NdotH** (dot product of the normal and the half angle) inputs. |
| **SchlickFresnelReflectance** | Calculates the Fresnel term using Schlick's approximation. The Fresnel term is the percentage of incoming light that the surface reflects, which increases when you view the surface at a shallow angle. It takes the **viewDir**, **halfAngle**, and **Reflectance** inputs. The default **Reflectance** value is 0.04. |
| **SmithGGXGeometryTerm** | Calculates the geometry term using the Smith GGX formula. The geometry term measures how much the surface's microfacets shadow and mask each other. It takes the **roughSqr** (roughness squared), **nDotL** (dot product of the normal and the light direction), and **nDotV** (dot product of the normal and the view direction) inputs. |

## Additional resources

* [Introduction to lighting model customization](Shader-Graph-Sample-Custom-Lighting-Introduction.md)
* [Get started with the Custom Lighting sample](Shader-Graph-Sample-Custom-Lighting-Get-Started.md)
* [Lighting Model sub graph reference](Shader-Graph-Sample-Custom-Lighting-Lighting-Models.md)
