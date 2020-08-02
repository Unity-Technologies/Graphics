# Raytracing Quality Node

The Raytracing Quality Node allows you to provide a fast implementation of your Shader Graph to be use in GPU heavy raytraced effect to tradeoff accuracy for speed: [Ray-Traced Reflections](Ray-Traced-Reflections.md), [Ray-Traced-Global-Illumination](Ray-Traced-Global-Illumination.md) and [Ray-Traced-Subsurface-Scattering](Ray-Traced-Subsurface-Scattering.md).

## Setup

The Raytracing Quality Node is a builtin keyword and thus need to be created in the Dashboard **Keyword -> Raytracing Quality** of the shader graph then drag and dropped in the working area.

![](Images/RaytracingQualityNode.png)

## Ports

| Name          | Direction | Type           | Description                                                  |
| :------------ | :-------- | :------------- | :----------------------------------------------------------- |
| **default**   | Input     | Vector4        | Sets the value to use for the normal shader graph. This is the default path use to render this shader graph. |
| **optimized** | Input     | Vector4        | Sets the value to use for the fast shader graph to use with the GPU heavy raytraced effect.|
| **output**    | Output    | Vector4        | Outputs is the value which will be selected based on the context this shader graph is used. |
