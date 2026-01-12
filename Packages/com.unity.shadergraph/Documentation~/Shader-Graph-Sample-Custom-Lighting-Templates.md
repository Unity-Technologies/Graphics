# URP Custom Lighting template reference

Explore the prebuilt URP Custom Lighting shader graph templates included in the Custom Lighting sample.

> [!NOTE]
> These templates are available in the [template browser](template-browser.md) under the **URP Custom Lighting** section. Don't edit the corresponding assets in the **Assets** > **Samples** > **Shader Graph** folder of your project.

Each Custom Lighting template includes a specific [Lighting Model sub graph](Shader-Graph-Sample-Custom-Lighting-Lighting-Models.md).

## Custom Lighting Basic

This template uses a basic lighting model that calculates diffuse lighting for multiple light sources. To reduce the cost of calculations, this shader does not support fog, specular, reflections, light cookies, and SSAO, but you can reintegrate any of these features if needed. This model makes the lighting as cheap as possible by only including the basic requirements. This type of lighting model is ideal for XR or low-end mobile devices where performance is critical.

## Custom Lighting Simple

This template uses a lighting model designed to be similar to the main lighting used by URP. It uses a slightly cheaper Blinn model for specular highlights, which reduces the quality but speeds up the rendering in most cases.

## Custom Lighting Toon

This template uses a lighting model that posterizes the lighting to give it the appearance of a cartoon. This shows you how you can alter lighting in the graph to achieve stylized effects that give your project a unique appearance.

## Custom Lighting URP

This template uses the same lighting formula as the Lit URP model. You can use it as a starting point if you want to begin with URP as is and then make changes.

## Additional resources

* [Introduction to lighting model customization](Shader-Graph-Sample-Custom-Lighting-Introduction.md)
* [Get started with the Custom Lighting sample](Shader-Graph-Sample-Custom-Lighting-Get-Started.md)
