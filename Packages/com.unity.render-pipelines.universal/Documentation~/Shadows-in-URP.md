# Shadows in the Universal Render Pipeline

The Universal Render Pipeline’s [Lights](light-component.md) can cast shadows from one GameObject onto another. They emphasize the position and scale of GameObjects, which adds a degree of depth and realism to a Scene that could otherwise look flat.

## Shadow map resolution

The resolution of a Light’s shadow map determines the size of its shadow maps. The larger the shadow map, the more precise the shadows can be, and the better the Universal Render Pipeline can capture small details in the shadow casting geometry. Rendering shadow maps at higher resolutions make them look sharper.

The number of shadow maps Universal RP renders per Light depends on the **Type** of the Light:

- A Spot Light renders one shadow map.
- A Point Light renders six shadow maps (the number of faces in a cubemap).
- A Directional Light renders one shadow map per cascade. Set the cascade count of Directional Lights from the [Universal Render Pipeline Asset](universalrp-asset.md) of your project.

Universal RP will try to use the best resolution according to the number of shadow maps that are needed in the scene, and the size of the shadow atlases.

## Shadow atlases

Universal RP renders all real-time shadows for a frame using one common shadow map atlas for all punctual light shadows (i.e shadows for Spot Lights and Point Lights), and an other shadow map atlas for Directional Light shadows.

Set the size of these atlases in your Unity Project’s [Universal Render Pipeline Asset](universalrp-asset.md). The atlas size determines the maximum resolution of shadows in your Scene.

For example, an atlas of size 1024 x 1024 can fit:

- Four shadow maps of 512 x 512 pixels.
- Sixteen shadow maps of 256 x 256 pixels.

### Matching shadow atlas resolution to Built-In RP settings

In projects that used the **Built-In Render Pipeline**, you controlled shadow maps resolution by selecting a shadow resolution level ("Low", "Medium", "High", "Very High") in your project's Quality Settings.
For each shadow map, Unity then decided which resolution to actually use, based on the algorithm explained in the [Built-In RP Manual Page about Shadow Mapping](https://docs.unity3d.com/Manual/shadow-mapping.html).
You could then inspect in the [Frame Debugger](https://docs.unity3d.com/Manual/FrameDebugger.html) the resolution actually used for a specific shadow map.

In **Universal Render Pipeline**, you specify the resolution of the Shadow Atlases. Therefore you can control the amount of video memory your application will allocate for shadows.

If you want to make sure that the resolution Universal RP uses for a specific punctual light shadow in your project, will not go under a specific value: Consider the number of shadow maps required in the scene, and select a big enough shadow atlas resolution.

For example: if your scene has four Spot Lights and one Point light ; and you want each shadow map resolution to be at least 256x256.
Your scene needs to render ten shadow maps (one for each Spot Light, and six for the Point Light), each with resolution 256x256.
Using a shadow atlas of size 512x512 would not be enough, because it can contain only four maps of size 256x256. Therefore, you should use a shadow atlas of size 1024x1024, that can contain up to sixteen maps of size 256x256.




## Shadow Bias

Shadow maps are essentially textures projected from the point of view of the Light. Universal RP uses a bias in the projection so that the shadow casting geometry does not self-shadow itself.

In Universal RP, each individual Light component controls its own shadow biasing using the following parameters:

- **Depth Bias**
- **Normal Bias**
- **Near Plane**

Find these settings under the **Shadows** section. If properties are not visible, change the Bias setting from "Use Pipeline Settings" to "Custom" to expose them.

Using high shadow bias values may result in light "leaking" through Meshes. This is where there is a visible gap between the shadow and its caster, and leads to shadow shapes that do not accurately represent their casters.


## Performance

Here are some example of frame times (observed with SRP package revision 78d514f756c and Unity 2020.2.0b):
- Impact of adding a Point Light with hard shadows at position (2, 1, 1) to [Universal RP Project Template](creating-a-new-project-with-urp) scene on Galaxy S20+:                        +1.9ms (total frame time 22.2ms)
- Impact of adding a Point Light with hard shadows at position (2, 1, 1) to Universal RP Project Template scene on PC - GeForce RTX 2080 Ti (Full HD): +0.1ms (total frame time 16.6ms)
