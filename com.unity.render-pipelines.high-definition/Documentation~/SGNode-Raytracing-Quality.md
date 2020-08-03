# Raytracing Quality Node

The Raytracing Quality Node allows you to provide a fast implementation of your Shader Graph to use in GPU-intensive ray-traced effects to tradeoff accuracy for speed. The GPU-intensive effects are:
* [Ray-Traced Reflections](Ray-Traced-Reflections.md).
* [Ray-Traced-Global-Illumination](Ray-Traced-Global-Illumination.md).
* [Ray-Traced-Subsurface-Scattering](Ray-Traced-Subsurface-Scattering.md).

## Setup

The Raytracing Quality Node represents a built-in keyword which means, to create one:
1. In the \[Blackboard](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Blackboard.html), click the plus (\**+**) button.
2. Select **Keyword > Raytracing Quality**. This creates the keyword and makes it visible on the Blackboard.
3. Now, to use the keyword in your graph, drag it from the Blackboard into the workspace.

![](Images/RaytracingQualityNode.png)

## Ports

| Name          | Direction | Type           | Description                                                  |
| :------------ | :-------- | :------------- | :----------------------------------------------------------- |
| **default**   | Input     | Vector4        | The value to use for the normal Shader Graph. This is the default path Unity uses to render this Shader Graph. |
| **optimized** | Input     | Vector4        | The value to use for the fast Shader Graph to use with the GPU-intensive ray-traced effects.|
| **output**    | Output    | Vector4        | Outputs is the value which will be selected based on the context this shader graph is used. |
