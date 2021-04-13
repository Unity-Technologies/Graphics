# What's new in HDRP version 11 / Unity 2021.1

This page contains an overview of new features, improvements, and issues resolved in version 11 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.1.

## Features

The following is a list of features Unity added to version 11 of the High Definition Render Pipeline, embedded in Unity 2021.1. Each entry includes a summary of the feature and a link to any relevant documentation.

### SRP packages are part of the core

With the release of Unity 2021.1, graphics packages are relocating to the core of Unity. This move simplifies the experience of working with new Unity graphics features, as well as ensuring that your projects are always running on the latest verified graphics code.

For each release of Unity (alpha / beta / patch release) the graphics code is embedded within the main Unity installer. When you install the latest release of Unity, you also get the latest URP, HDRP, Shader Graph, VFX Graph, and more.

Tying graphics packages more closely to the main Unity release allows better testing to ensure that the graphics packages you use have been tested extensively with the version of Unity you have downloaded.

You can also use a local copy or a custom version of the graphics packages by overriding them in the manifest file.

For more information, see the following post on the forum: [SRP v11 beta is available now](https://forum.unity.com/threads/srp-v11-beta-is-available-now.1046539/).

### Mixed cached shadow maps

From HDRP 11.0, it is possible to cache only a portion of non-directional shadow maps. With this setup, HDRP renders shadows for static shadow casters into the shadow map based on the Light's Update Mode, but it renders dynamic shadow casters into their respective shadow maps each frame.

This can result in significant performance improvements for projects that have lights that don't move or move not often, but need dynamic shadows being cast from them.

For more information about the future, see the [Shadow](Shadows-in-HDRP.md) section of the documentation.

### Cubemap fields in Volume components

Cubemap fields now accept both [RenderTextures](https://docs.unity3d.com/Manual/class-RenderTexture.html) and [CustomRenderTextures](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html) if they use the cubemap mode / dimension. This change affects the `HDRI Sky` and `Physically Based Sky` components and allows you to animate both skies.

For more information, see the [HDRI Sky](Override-HDRI-Sky.md) and [Physically Based Sky](Override-Physically-Based-Sky) sections of the documentation.
### Volume System API

#### Nested Volume Component Parameters

The volume system will now search for volume parameters declared inside nested classes.

```cs
public class ExampleComponent: VolumeComponent
{
    [Serializable]
    public class NestedClass
    {
        public FloatParameter number = new FloatParameter(0.0f);
    }

    public NestedClass nested = new NestedClass();
}
```

#### Volume Component Init Callback

From HDRP 11.0, the Volume Components support an Init callback which can be used to initialize static resources, for example to have a default value on a texture parameter.
It is executed only once at runtime and at every script reload inside the editor.
```cs
public class ExampleComponent : VolumeComponent
{
    static Texture s_DefaultTexture = null;
    public TextureParameter texture = new TextureParameter(s_DefaultTexture);
    static void Init()
    {
        s_DefaultTexture = //...
    }
}
```

### Density Volume Improvements

Density Volumes masks now support using 3D RenderTextures as masks. 3D mask textures now also use all four RGBA channel which allows volumetric fog to have different colors and density based on the 3D Texture.

The size limit of 32x32x32 for the mask textures has also been replaced by a setting in the HDRP asset called "Max Density Volume Resolution", under the Lighting > Volumetrics section. The upper limit for mask textures is now 256x256x256, an info box below the field tells you how much memory is allocated to store these textures. Note that increasing the resolution of the mask texture doesn't necessarily improve the quality of the volumetric, what's important is to have a good balance between the **Volumetrics** quality and the density volume resolution.

There is a new field to change the falloff HDRP applies when it blends the volume using the Blend Distance property. You can choose either Linear which is the default and previous technique, or Exponential which is more realistic.

Finally, the minimal value of the **Fog Distance** parameter was lowered to 0.05 instead of 1 and now allows thicker fog effects to be created.

### Cloud System

![](Images/HDRPFeatures-CloudLayer.png)

From HDRP 11.0, HDRP introduces a cloud system, which can be controlled through the volume framework in a similar way to the sky system.

HDRP includes a Cloud Layer volume override which renders a cloud texture on top of the sky. For more information, see the [Cloud Layer](Override-Cloud-Layer.md) documentation.

For detailed steps on how to create your custom cloud solution, see the documentation about [creating custom clouds](Creating-Custom-Clouds.md).

## Issues resolved

For information on issues resolved in version 11 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@11.0/changelog/CHANGELOG.html).
