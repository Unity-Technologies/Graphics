# What's new in HDRP version 15 / Unity 2023.1

This page contains an overview of new features, improvements, and issues resolved in version 15 of the High Definition Render Pipeline (HDRP), embedded in Unity 2023.1.

## Added

### Temporal Anti-Aliasing Sharpening Mode
![](Images/TAA-Sharpening-header.png)

Starting from HDRP 15, two new options are available to perform sharpening with Temporal Anti-aliasing. The first new option is a post-process pass that will offer higher quality sharpening, control on how much sharpening will happen and an option to reduce possible ringing artifacts. The second option is to run Contrast Adaptive Sharpening from AMD FidelityFX.

### Specular Fade

Specular light can now completely be faded when using a specular color workflow using the **HDRP/Lit** and **HDRP/StackLit** shaders by toggling a new option that can be found in the HDRP Global Settings under **Miscellaneous/Specular Fade**.

|        With Specular         |        Faded Specular        |
|:----------------------------:|:----------------------------:|
| ![](Images/WithSpecular.png) | ![](Images/KillSpecular.png) |

## Updated

### Rendering Layers

HDRP 15.0 introduces updates to the managment of Rendering Layers. Light Layers and Decal Layers will now share the first 16 Rendering Layers, instead of using 8 separate bits each.
Additionally, an option was added in the HDRP Asset to allow access to a fullscreen buffer containing the Rendering Layers Masks of rendered Objects. That buffer can be sampled from the ShaderGraph through the __HD Sample Buffer__ node, and be used to implement custom effects, like outlining objects on a specific rendering layer.
