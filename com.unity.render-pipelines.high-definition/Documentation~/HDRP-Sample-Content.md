# HDRP Sample Content

The High Definition Render Pipeline (HDRP) comes with a set of Samples to help you get started.

A Sample is a set of Assets that you can import into your Project and use as a base to build upon or learn how to use a feature.

To find these Samples:

1. Go to **Windows > Package Manager**, and select **High Definition RP** from the package list.
2. In the main window that shows the package's details, find the **Samples** section.
3. To import a Sample into your Project, click the **Import into Project** button. This creates a **Samples** folder in your Project and imports the Sample you selected into it. This is also where Unity imports any future Samples into.

## Additional Post-Processing Data

Additional Post-Processing Data gives you access to Textures you can use with post-processing effects. It provides:

- Lens Dirt Textures (designed for use in [Bloom](Post-Processing-Bloom.html)).
- Spectral Look-up Textures (designed for use in [Chromatic Aberrations](Post-Processing-Chromatic-Aberration.html)).
- Look-Up Textures.

## Shader Graph Samples

This Sample includes example Shader Graphs that show you how to use the following Master Nodes: 

* [Fabric Master Node](Master-Node-Fabric.html).
* [Hair Master Node](Master-Node-Hair.html).
* [Decal Master Node](Master-Node-Decal.html).

The Fabric and Hair Master Nodes usually require various work from artists inside the Shader Graph and the Samples are a good head start.

## Procedural Sky

The [Procedural Sky](Override-Procedural-Sky.html) is a deprecated sky type from older versions of HDRP which you can use for compatibility. This Sample also includes an example of how to create a custom sky in your Project that is compatible with HDRP's [Volume framework](Volumes.html). HDRP will remove the Procedural Sky in a future version because it behaves incorrectly with HDRP's physically based light units.

## Particle System Shader Samples

This Sample includes various examples of lit and unlit particle effects.

## Material Samples

![Material Samples](Images/MaterialSamples.png)

This Sample includes various examples of Materials that use the [Lit Shader](Lit-Shader.html). The included Materials use effects such as subsurface scattering, displacement, and anisotropy. The **MaterialSamples** Scene requires Text Mesh Pro to display the text explanations.

