# Lighting Models

Use any of these subgraphs with the Unlit material type to create lighting that is defined in the Shader Graph. To know how to use these subgraphs, refer to the [main page](Shader-Graph-Sample-Custom-Lighting.md#examples).

All of the following subgraphs include the ApplyDecals subgraph to blend decal data, and the Debug Lighting and Debug Materials subgraph nodes to support the debug rendering modes (available in the Rendering Debugger window). They also include a subgraph from the Core Lighting Models category to define the behavior of the lighting itself as described below.

## Lit Basic
The Lit Basic lighting model does very simple lighting and leaves out most lighting features to render as fast as possible. It calculates simple diffuse lighting and a simple form of ambient lighting. It does not support fog, reflections, specular, light cookies, or any other lighting features. But it does render fast and is ideal for low-end mobile devices and XR headsets.

## Lit Colorize
The Colorize lighting model is an example of custom behavior type you can create when you can control the lighting model. The main directional light renders the scene in grayscale with no color. Color is introduced with point lights, which allows you to control where the scene has color based on where you place the point lights in the scene.

## Lit Simple
The Lit Simple lighting model is the same as the URP lighting model, except it uses the Blinn formula for the specular highlights. This makes it slightly cheaper to render than standard URP while looking fairly similar. If you still need all of the lighting features (specular, fog, screen space ambient occlusion, reflections, etc), but you want to make the lighting cheaper, this may be a good choice.

## Lit Toon
The Lit Toon lighting model uses a Posterize operation to break the smooth lighting gradient into distinct bands of shading. It simulates the look of cartoons where lighting is rendered with distinct colors of paint rather than smooth gradients.

## Lit URP
The Lit URP lighting model closely matches the lighting that the Universal Render Pipeline does by default. If you want to start with the URP lighting and then alter it, this is the node to use.

