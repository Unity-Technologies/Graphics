# What's new in HDRP version 16 / Unity 2023.2

This page contains an overview of new features, improvements, and issues resolved in version 16 of the High Definition Render Pipeline (HDRP), embedded in Unity 2023.2.

## Added

## Updated

### Ray-Traced Reflections ReBLUR denoiser
![](Images/WhatsNew16_ReBLUR_Denoiser.png)

Starting from HDRP 16, the Ray-Traced Reflections denoiser has been updated based on the ReBLUR implementation. This new algorithm now includes an anti-flickering setting that improves temporal stability and renders more coherent results from rough to smooth objects.  
This new denoiser is used in Raytracing Mode and Mixed Mode and completely replace the previous version. 

### Decals

HDRP 16.0 adds support for [Decal Master Stack](master-stack-decal.md) to affect transparent objects. The result of the shader graph is stored within the same decal atlas as [Decal Shader](Decal-Shader.md) materials.
In addition through the **Transparent Dynamic Update** setting within the decal shader graphs it is now possible to animate the decal within the atlas.
