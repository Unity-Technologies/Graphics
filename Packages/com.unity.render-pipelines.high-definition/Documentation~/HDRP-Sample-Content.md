# HDRP Sample Content

The High Definition Render Pipeline (HDRP) comes with a set of Samples to help you get started.

A Sample is a set of Assets that you can import into your Project and use as a base to build upon or learn how to use a feature.

To find these Samples:

1. Go to **Window** > **Package Manager**, and select **High Definition RP** from the package list.
2. In the main window that shows the package's details, find the **Samples** section.
3. To import a Sample into your Project, click the **Import into Project** button. This creates a **Samples** folder in your Project and imports the Sample you selected into it. This is also where Unity imports any future Samples into.

## Additional Post-Processing Data

Additional Post-Processing Data gives you access to Textures you can use with post-processing effects. It provides:

- Lens Dirt Textures (designed for use in [Bloom](Post-Processing-Bloom.md)).
- Spectral Look-up Textures (designed for use in [Chromatic Aberrations](Post-Processing-Chromatic-Aberration.md)).
- [Look-Up Textures](Authoring-LUTs.md).

## Procedural Sky

The [Procedural Sky](Override-Procedural-Sky.md) is a deprecated sky type from older versions of HDRP which you can use for compatibility. This Sample also includes an example of how to create a custom sky in your Project that is compatible with HDRP's [Volume framework](Volumes.md). HDRP will remove the Procedural Sky in a future version because it behaves incorrectly with HDRP's physically based light units.

## Particle System shader samples

This Sample includes various examples of lit and unlit particle effects.

## Material samples

![Material Samples](Images/MaterialSamples.png)

This Sample includes various examples of Materials. It includes Materials that use the [Lit Shader](Lit-Shader.md), [Fabric Master Stack](master-stack-fabric.md), [Hair Master Stack](master-stack-hair.md), [Eye Shader](eye-shader.md) and [Decal Master Stack](master-stack-decal.md). The included Materials use effects such as subsurface scattering, displacement, and anisotropy. The **MaterialSamples** Scene requires Text Mesh Pro to display the text explanations.

The Fabric, Hair and Eye Master Nodes usually require various work from artists inside the Shader Graph and the Samples are a good head start.

In the **Eye** Scene, the eye examples use a carefully designed mesh with a particular UV setup at a specific import scale factor. If you want to produce eyes of similar quality, open the eye mesh in 3D modelling software to see how the mesh is constructed and the UVs are setup.

In the Transparency scenes, the examples contain information on how to setup properly transparents in your projects using different rendering methods (Rasterization, Ray Tracing, Path Tracing).
To take advantage of all the content of this section, a GPU that supports [Ray Tracing](Ray-Tracing-Getting-Started.md) is needed.

## Lens Flare samples

![Lens Flare Samples](Images/LensFlareSamples.png)

The [Lens Flare](shared/lens-flare/lens-flare-component.md) samples include the following examples that you can use in your project:
- Lens Flare Assets.
- Lens Flare Textures.
- A scene you can use to preview Lens Flare Assets.
- A scene to showcases the use of Lens Flares with interior lighting.
- A scene to showcases the use of Lens Flares with a directional light.

## Volumetric samples

![Volumetric Samples](Images/VolumetricSamples.png)
The volumetric samples include a scene that contains multiple examples of [volumetric fog](Local-Volumetric-Fog.md). This scene includes the following:

- 3D textures.
- Procedural 3D noise subgraphs.
- Fog Volume Shader Graph examples.

## Fullscreen samples
![Fullscreen Samples](Images/FullscreenSamples.png)

This sample includes examples on how to create a [Fullscreen Shader](fullscreen-shader.md) and use it with a Custom Pass, Custom Post Process and Custom Render Target. The sample scene includes prefabs for the following effects:

- Custom Pass : Edge Detection, Sobel Filter, Object Highlight, Night Vision, Speed Lines.
- Custom Render Targets : Dynamic Custom HDRi for Night Sky, Animated Water Droplets.
- Custom Post Process : Colorblindness Filter.

## Environment samples

![](Images/Water_samples.png)The Environment samples contain the following scenes you can use to test HDRP's [Water](WaterSystem.md) features: 

- Pool: Demonstrates ripples and buoyancy. 
- River: Demonstrates current, water deformers, floating objects, and a water mask.
- Ocean: Demonstrates waves, foam, and the water excluder.