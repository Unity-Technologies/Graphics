# About the Universal Render Pipeline

![Universal Render Pipeline in action](Images/AssetShots/Beauty/Overview.png)

The Universal Render Pipeline (UniversalRP) is a prebuilt Scriptable Render Pipeline, made by Unity. The technology offers graphics that are scalable to mobile platforms, and you can also use it for higher-end consoles and PCs. You’re able to achieve quick rendering at a high quality without needing compute shader technology. UniversalRP uses simplified, physically based Lighting and Materials.

The UniversalRP uses single-pass forward rendering. Use this pipeline to get optimized real-time performance on several platforms. 

The UniversalRP is supported on the following platforms:
* Windows and UWP
* Mac and iOS
* Android
* XBox One
* PlayStation4
* Nintendo Switch
* All current VR platforms

The Universal Render Pipeline is available via two templates: UniversalRP and UniversalRP-VR. The  UniversalRP-VR comes with pre-enabled settings specifically for VR. The documentation for both render pipelines is the same. For any questions regarding UniversalRP-VR, see the UniversalRP documentation.

**Note:**  Built-in and custom Lit Shaders do not work with the Universal Render Pipeline. Instead, UniversalRP has a new set of standard shaders. If you upgrade a current Project to UniversalRP, you can upgrade built-in shaders to the new ones.

**Note:** Projects made using UniversalRP are not compatible with the High Definition Render Pipeline or the built-in Unity rendering pipeline. Before you start development, you must decide which render pipeline to use in your Project. 
