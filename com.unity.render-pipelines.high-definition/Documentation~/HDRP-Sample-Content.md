# HDRP Sample Content

The High Definition Render Pipeline (HDRP) comes with a set of Samples to help you getting started.

A Sample is a set of assets that you can import in your project and use as a base to build your project or learn how a feature can be used.

To find these samples, go **to Windows > Package Manager**, and **select High Definition RP** in the list. On the right side of the window you will see the Package's details, and a **Samples** category. In order to import a Sample in your project, simply click the **Import into Project** button. This will create a Samples folder in your project in which all the samples will get imported.

## Additional Post-Processing Data

Additional Post-Processing Data gives you access to textures you can use with the Post-Processing Effects :

- Lens Dirt Textures (designed to be used in Bloom)
- Spectral Look-up Textures (designed to be used in Chromatic Aberrations)
- Look-Up Textures

## Shader Graph Samples

These Shader Graph Samples show how one can use the advanced master nodes: Fabric Master Node and the Hair Master Node. Those requires various work from the artists inside the shader graph and the provided example are a good start to play with them.

 There is also an example of Decal Master Node.

## Procedural Sky

The Procedural Sky is a deprecated Sky from older version of HDRP which can be use for compatibility. It will be remove in future version. It have various wrong behavior with the physically based light unit of HDRP. It is also an example showing how to create a custom sky in your project so that it's compatible with the Volume framework.

## Particle System Shader Samples

The Paricle System Shader Samples show examples of various lit and unlit particle effects.

## Material Samples

![Material Samples](Images/MaterialSamples.png)

The Materials Samples bring into your project examples of materials based on the lit shader using effects such as subsurface scattering, displacement, anisotropy and more. The MaterialSamples scene requires Text Mesh Pro to display text explanations.

