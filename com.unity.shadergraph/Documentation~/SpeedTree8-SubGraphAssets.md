# SpeedTree 8 Sub Graph Assets

## Prerequisite information
This documentation assumes that you are already familiar with the concepts described in the following pages:
* [SpeedTree](https://docs.unity3d.com/Manual/SpeedTree.html)
* [Sub Graph Nodes](Sub-graph-Node.md)
* [Sub Graph Assets](Sub-graph-Asset.md)
* [Keywords](Keywords.md)

The documentation on [ShaderLab material properties](https://docs.unity3d.com/Manual/SL-Properties.html) might also be contextually helpful.

## Description
[SpeedTree](https://docs.unity3d.com/Manual/SpeedTree.html) is a third-party solution that includes both ready-to-use tree assets, and modeling software for creating your own tree assets.

Shader Graph has three built-in SpeedTree Sub Graph Assets:

* [SpeedTree8ColorAlpha](#SpeedTree8ColorAlpha)
* [SpeedTree8Wind](#SpeedTree8Wind)
* [SpeedTree8Billboard](#SpeedTree8Billboard)

These Sub Graph Assets provide SpeedTree 8 functionality for both the [Universal Render Pipeline (URP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.0/manual/index.html) and [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/manual/index.html), so that you can work with SpeedTree 8 assets and create your own custom SpeedTree 8 Shader Graphs.

**Note:** The URP-specific versions of these SpeedTree 8 Sub Graph Assets use transparent billboard back faces instead of culling billboard back faces. These Sub Graph Assets can only replace their URP equivalents as a default once URP supports per-material culling overrides in Shader Graphs.

## SpeedTree8ColorAlpha <a name="SpeedTree8ColorAlpha"></a>
Each SpeedTree asset has four maps: a basemap (color/albedo), bump map (which provides surface normals), extra map (which provides metallic and ambient occlusion data), and  subsurface map (which provides the subsurface scattering color). The basemap provides input color and alpha data.

This Sub Graph Asset applies all SpeedTree 8 features that modify the basemap's color and alpha data. These features are particularly useful for the following actions:
* Tinting the basemap color
* Varying tree hues
* Crossfading between levels of detail (LODs)
* Hiding geometry seams


### Tinting the basemap color
You can use the SpeedTree8ColorAlpha Sub Graph Asset to apply a tint to the basemap color. This is useful if, for example, you want to adjust tree colors for different seasons of the year.

| Property   | Support   | Purpose           | Behavior                                                        |
|------------|-----------|-------------------|-----------------------------------------------------------------|
| `_ColorTint` | URP, HDRP | Tint the basemap. | Multiplies the `_ColorTint` property value by the basemap color.  |


### Varying tree hues
To improve the visual diversity of SpeedTrees, you can use this Sub Graph Asset to modify the color of each tree instance. Both `_OldHueVarBehavior` and `_HueVariationColor` use the tree’s absolute [world-space position](https://learnopengl.com/Getting-started/Coordinate-Systems) to determine a [pseudorandomized](https://en.wikipedia.org/wiki/Pseudorandomness) hue variation intensity value.

| Property              | Support             | Purpose                                                                 | Behavior                                                                                                                                                                                  |
|-----------------------|---------------------|-------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `_OldHueVarBehavior`    | URP                 | To match the behavior of URP-specific and Built-In SpeedTree 8 shaders. | Uses the pseudorandom hue variation intensity value to [parameterize](https://en.wikipedia.org/wiki/Parametrization_(geometry)) the [linear interpolation](https://en.wikipedia.org/wiki/Linear_interpolation) between the basemap color (t=0) and HueVariation color (t=1) .                               |
| `_HueVariationColor`    | URP, HDRP           | To provide SpeedTrees with more hue diversity.                          | Uses the pseudorandom hue variation intensity value as its opacity and applies that to the basemap color as an [overlay blend](https://docs.unity3d.com/2021.2/Documentation/Manual/SL-BlendOp.html). A more subtle effect than that provided by `_OldHueBehavior`. |
| `EFFECT_HUE_VARIATION`  | N/A                 | N/A                                                                     | This keyword was used in handwritten SpeedTree 8 shaders, but it's not used in these SpeedTree 8 Shader Graphs. This is to ensure compliance with the default shader variant limit.                                                                                    |
|  `_HueVariationKwToggle` | URP, HDRP, Built-In | Only to support upgrade functionality.                                  | See [SpeedTreeImporter.hueVariation](https://docs.unity3d.com/ScriptReference/SpeedTreeImporter-hueVariation.html).

### Crossfading between levels of detail (LODs)
Crossfading dithers between different levels of detail ([LODs](https://docs.unity3d.com/Manual/LevelOfDetail.html)) to minimize [popping](https://en.wikipedia.org/wiki/Popping_(computer_graphics) ) during abrupt transitions. The SpeedTree8ColorAlpha Sub Graph Asset uses a [Custom Function Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@12.0/manual/Custom-Function-Node.html) for that purpose.

This Custom Function Node is not SpeedTree-specific. Enable **Animate Cross-fading** and select an **LOD Fade** setting to use it with any asset that has the LOD Group component. See [Transitioning between LOD levels](https://docs.unity3d.com/Manual/class-LODGroup.html#transitions) for more information.

### Hiding geometry seams
The SpeedTree8ColorAlpha Sub Graph asset applies an alpha gradient to soften the transitions between geometry segments that sample different parts of the basemap.

## SpeedTree8Wind <a name="SpeedTree8Wind"></a>
The SpeedTree8Wind Sub Graph Asset uses a [Custom Function Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@12.0/manual/Custom-Function-Node.html) to deform the vertices of SpeedTree 8 models in response to your application’s wind data. You can use this to make trees appear to bend in the wind.

Unity applies wind data to the SpeedTree8Wind Sub Graph Asset is as follows:
* When a [WindZone](https://docs.unity3d.com/Manual/class-WindZone.html) affects a SpeedTree 8 GameObject that has [Wind](https://docs.unity3d.com/Manual/com.unity.modules.wind.html) enabled, Unity generates SpeedTree 8 wind simulation data.
* Unity populates the SpeedTreeWind Cbuffer with that wind simulation data.
* The SpeedTree8Wind Sub Graph Asset bases its deformation behavior on the data in the SpeedTreeWind Cbuffer.

This asset includes automated LOD vertex interpolation when **LOD Fade** is set to **SpeedTree**. However, it does not support instancing.

## SpeedTree8Billboard <a name="SpeedTree8Billboard"></a>
The SpeedTree8Billboard Sub Graph Asset calculates billboard normals from a SpeedTree 8 model's bump map, geometric tangent, and bitangent data. It includes dithering functionality to improve the appearance of billboards at view angles diagonal to the model.

The keyword toggle associated with this feature is named `EFFECT_BILLBOARD`.This supports backwards compatibility with previous versions of ShaderGraph, which require keywords and their toggling properties to have identical names.

## SpeedTree 8 InterpolatedNormals
All SpeedTree 8 shaders that Unity provides interpolate geometric normals, tangents, and bitangents in the vertex stage, because this results in a better visual appearance than the per-pixel data that Shader Graph nodes provide. You do not need to use this feature if your SpeedTree 8 Shader Graph does not include custom interpolators.

HDRP and URP do not have identical backface normal transformation behavior. This can become a problem when you use [Custom Interpolators](Custom-Interpolators.md) for geometric normal, tangent, and bitangent data. The purpose of the SpeedTree 8 InterpolatedNormals Sub Graph Asset is to allow for that difference. It combines geometric normal data with bump maps in a way that is compatible with the target pipeline's backface normal transformation behavior.
