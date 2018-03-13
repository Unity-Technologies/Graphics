![](https://blogs.unity3d.com/wp-content/uploads/2018/01/image2.jpg)

The High Definition Render Pipeline (HDRP) is a high-fidelity Scriptable Render Pipeline built by Unity to target modern (Compute Shader compatible) platforms. 

The HDRP utilizes Physically-Based Lighting techniques, linear lighting, HDR lighting and a configurable hybrid Tile/Cluster deferred/Forward lighting architecture and gives you the tools you need to create games, technical demos, animations and more to a high graphical standard. 

NOTE: Projects made using HDRP are not compatible with the Lightweight Render Pipeline. You must decide which render pipeline your project will use before starting development as HDRP features are not cross-compatible between HDRP and Lightweight. 

This section contains the information you need to begin creating applications using HDRP; including information on Lighting, Materials and Shaders, Cameras, debugging and information for advanced users.

HRDP is only supported on the following platforms: 

__Note: HDRP will only work on the following platforms if the device used supports Compute Shaders. I.e: HDRP will only work on iOS if the iPhone model used supports Compute Shaders.__

* Windows and Windows Store, with DirectX 11 or DirectX 12 and Shader Model 5.0
* macOS and iOS using Metal graphics
* Android, Linux and Windows platforms with Vulkan
* Modern consoles (Sony PS4 and Microsoft Xbox One)

__HDRP does not support OpenGL or OpenGL ES devices.__
