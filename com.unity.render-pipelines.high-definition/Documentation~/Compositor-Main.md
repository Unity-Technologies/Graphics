# Compositor (Preview)

The Compositor allows for real-time compositing operations between Unity's High Definition Render Pipeline (HDRP) and external media sources, such as videos or images. Depending on the requirements for your application, the Compositor provides multiple composition techniques. You can use each technique independently or use more than one at the same time to create a combination of different types of composition operations. The techniques that the Compositor includes are:
- **Camera Stacking:** Allows you to render multiple [HDRP Cameras](HDRP-Camera.md) to the same render target.
- **Graph-Based Composition:** Allows you to use arbitrary mathematical operations to combine multiple Composition Layers to generate the final frame.
- **3D Composition:** Allows you to use Composition Layers as 3D surfaces in a Unity Scene. This means that, for example, Unity can calculate reflections and refractions between different Composition Layers and GameObjects.

The following table provides a high level overview of the advantages and disadvantages of each compositing technique:

| **Technique** | **Performance** | **Memory Overhead** | **Flexibility** | **Feature Coverage [*]** |
| ------------- | ------------- |------------- | ------------- | ------------- |
| **Camera Stacking** | High | Low | Low | High |
| **Graph-Based Composition** | Low | High | High| Low |
| **3D Composition** | Low | High | Low | High |

[*] *Feature Coverage* indicates whether features such as [screen-space reflections](Override-Screen-Space-Reflection.md), transparencies or refractions can work between layers.

Furthermore, the Compositor includes functionality such as *"localized post-processing"*, where a Post-Processing Volume only affects certain GameObjects in the Scene. 

For a high level overview of the Compositor's functionality please refer to the [User Guide](Compositor-User-Guide) section. For a description on specific options in the user interface, please refer to the [User Options](Compositor-User-Options) section.

## Composition example

The following example uses the Compositor to render a watermark on top of a Unity Scene.
![](Images/Compositor-HDRPTemplateWithLogo.png)

*The result.*


![](Images/Compositor-CompositorSimpleGraph.png)

*The composition graph for the watermark example:*