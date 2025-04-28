# Understand Fullscreen Materials

Use the Fullscreen shader to create a custom effect that appears over the entire screen view. For example, you could use this shader to make the screen turn red when a character takes damage, or make droplets of water appear on the screen. You can see some example Fullscreen shaders in the [Fullscreen shader samples](create-a-fullscreen-material.md#fullscreen-samples).

You can then use the Fullscreen shader in the following ways: 

- To create a [Custom Pass effect](custom-pass-create-gameobject.md#material-from-fullscreen-shadergraph).
- To create a [Custom Post Process effect](custom-post-processing-use-full-screen-shader.md).
- In a C# script with the [`HDUtils.DrawFullscreen`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@15.0/api/UnityEngine.Rendering.HighDefinition.HDUtils.html) or `Graphics.Blit()` functions. To use `Graphics.Blit()`see [Make a Fullscreen material Blit compatible](create-a-fullscreen-material#make-a-full-screen-shader-graph-blit-compatible).

![A scene of 3D shapes, with a full-screen shader that applies a raindrop effect to the screen.](Images/Fullscreen-shader-rain.png)

A full-screen shader that applies a raindrop effect to the screen.

Refer to [Create a Fullscreen Material](create-a-fullscreen-material.md) for more information.
