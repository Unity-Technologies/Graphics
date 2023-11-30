# Best practices for UI in the High Definition Render Pipeline

Almost every Project uses some form of user interface (UI). This could be:

* In a menu system
* To specify a point of interest in your application
* To display valuable information to users.

This guide explains how to use [Unity UI](https://docs.unity3d.com/Packages/com.unity.ugui@latest) in your High Definition Render Pipeline (HDRP) Project and provide best practice information.

HDRP supports all the Canvas **Render Modes**, however, it only supports Unlit UI shaders for Canvas UI.

## Working in a linear color space
HDRP Projects require you to work in linear [color space](https://docs.unity3d.com/Manual/LinearLighting.html). When you create UI in the Editor, ensure the sprite textures are in linear color space to make sure the UI renders correctly. This is especially important for transparent sprites. For more information, see [Linear or gamma workflow](https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html) and [Working with linear Textures](https://docs.unity3d.com/Manual/LinearRendering-LinearTextures.html).

## Multiple Camera setup
HDRP only supports a single Camera setup by default. If you need to use more than one camera at the same time, use one the following alternatives:


* [Custom Passes](Custom-Pass.md): Allows you to inject shader code and C# at certain points inside the render loop. This gives you the ability to draw GameObjects and process full-screen passes.


* [Graphics Compositor](graphics-compositor.md): Allows you to render multiple cameras to the same render target.

## Post-processing effects on UI
HDRP renders UI during the transparent pass, before post-processing. This means that you need to create Custom Passes and custom Post Processes to achieve typical visual effects for UI.

For example, if you want to blur the scene background when a UI menu is active:

1. Render your menu in a Canvas with its **Render Mode** set to **Screen Space - Overlay**. Using this render mode means HDRP doesn't apply post-processing to the canvas.
2. Add a [Custom Pass](Custom-Pass.md), injected **After Post Process**, to blur your scene. For examples on how to create custom passes, see [HDRP Custom Passes](https://github.com/alelievr/HDRP-Custom-Passes).

## Known limitations
Here is a list of known issues between Unity UI and HDRP:

* HDRP only supports Unlit UI.
* On VR platforms, HDRP only supports the **World Space** Canvas **Render Mode**.
