# Best Practices on UI with High Definition Render Pipeline

Almost any project makes use of UI: for your in-game menu, to indicate a point of interest in your game, or simply display valuable information to your users. In this guide, we will guide you on how to use [Unity UI](https://docs.unity3d.com/Packages/com.unity.ugui@latest) with your HDRP project.

All the Canvas’ Render modes are supported from Screen Space to World Space. However, only Unlit UI shaders are currently supported.

## Working in a Linear Color Space
HDRP projects require working in a Linear color space. When creating your UI, in order to have the proper color - especially on transparent sprites- you need to ensure your textures are in the right color space too. For more information, see [Linear or gamma workflow guide](https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html).

## Multiple Camera Setup
In HDRP, we only support a single Camera setup by default. If you have needs for multi-camera setups, we encourage you to look into those alternatives:


* [Custom Passes](Custom-Pass.md) allow you to inject shader and C# at certain points inside the render loop, giving you the ability to draw objects, do full screen passes,


* [Graphics Compositor](Compositor-Main.md) allows you to render multiple HDRP Cameras to the same Render Target.

## Post-processing effects on UI
UI is rendered before post-processes during the Transparent pass. This setup implies the need of Custom Passes and custom Post Processes to achieve typical visual effects for UI.

For example, it is typical to want a blurred scene background when opening a UI menu, if that is the case, then you should:
Render your menu in a Canvas with its RenderMode set to Screen-Space - Overlay ; so post-processes are not applied on the menu)
Add a Custom Pass injected After Post Process that will blur your scene - see [HDRP Custom Passes](https://github.com/alelievr/HDRP-Custom-Passes) for further examples.

## Known Limitations
Here is a list of known issues between Unity UI and HDRP:
* Only Unlit UI is supported,
* On XR platforms, only World-Space Canvas’ Render Mode is supported.
