# About the Lightweight Render Pipeline

![Lightweight Render Pipeline in action](Images/AssetShots/Beauty/Overview.png)

The Lightweight Render Pipeline (LWRP) is a prebuilt Scriptable Render Pipeline, made by Unity. The technology offers graphics that are scalable to mobile platforms, and you can also use it for higher-end consoles and PCs. You’re able to achieve quick rendering at a high quality without needing compute shader technology. LWRP uses simplified, physically based Lighting and Materials.

The LWRP uses single-pass forward rendering. Use this pipeline to get optimized real-time performance on several platforms. 

The LWRP is supported on the following platforms:
* Windows and UWP
* Mac and iOS
* Android
* XBox One
* PlayStation4
* Nintendo Switch
* All current VR platforms

The Lightweight Render Pipeline is available via two templates: LWRP and LWRP-VR. The  LWRP-VR comes with pre-enabled settings specifically for VR. The documentation for both render pipelines is the same. For any questions regarding LWRP-VR, see the LWRP documentation.

**Note:**  Built-in and custom Lit Shaders do not work with the Lightweight Render Pipeline. Instead, LWRP has a new set of standard shaders. If you upgrade a current Project to LWRP, you can upgrade built-in shaders to the new ones.

**Note:** Projects made using LWRP are not compatible with the High Definition Render Pipeline or the built-in Unity rendering pipeline. Before you start development, you must decide which render pipeline to use in your Project. 
