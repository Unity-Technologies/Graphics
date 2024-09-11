---
uid: um-srp-custom-getting-started
---

# Create a custom render pipeline

This page contains information on how to get started with creating your own custom render pipeline based on the Scriptable Render Pipeline (SRP).

<a name="creating-custom-srp"></a>
## Creating a new project and installing the packages needed for a custom render pipeline

These instructions show you how to create a custom render pipeline using the SRP Core package. SRP Core is a package made by Unity that contains a reusable code to help you make your own render pipeline, including boilerplate code for working with platform-specific graphics APIs, utility functions for common rendering operations, and the shader library that URP and HDRP use.

1. Create a new Unity Project.
2. Use Git to create a clone of the [SRP source code repository](https://github.com/Unity-Technologies/Graphics). You can place the SRP source code in any  location on your disk, as long as it is not in one of the [reserved Project sub-folders](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html#PkgLocation).
3. Use Git to update your copy of the SRP source code to a branch that is compatible with your version of the Unity Editor. Read [Using the latest version](https://github.com/Unity-Technologies/Graphics#branches-and-package-releases) in the SRP repository documentation for information on branches and versions.
4. Open your Project in Unity, and install the following packages from the SRP source code folder on your disk, in the following order. For information on installing packages from disk, see [Installing a package from a local folder](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html).
    * _com.unity.render-pipelines.core_. 
    * Optional: _com.unity.render-pipelines.shadergraph_. Install this package if you intend to use Shader Graph or modify the Shader Graph source code as part of your custom SRP.
    * Optional: _com.unity.render-pipelines.visualeffectgraph_. Install this package if you intend to use Visual Effect Graph or modify the Visual Effect Graph source code as part of your custom SRP.

You can now debug and modify the scripts in your copy of the SRP source code, and see the results of your changes in your Unity Project.

## Creating a custom version of URP or HDRP

The Universal Render Pipeline (URP) and the High Definition Render Pipeline (HDRP) offer extensive customization options to help you achieve the graphics and performance you need. However, if you want even more control, you can create a custom version of one of these render pipelines, and modify the source code.

Follow steps 1-3 in the section above, **Creating a new Project and installing the packages needed for a custom SRP**. When you reach step 4, install the following packages in the following order:

**URP:**

* _com.unity.render-pipelines.core_
* _com.unity.render-pipelines.shadergraph_
* _com.unity.render-pipelines.universal_

**HDRP:**

* _com.unity.render-pipelines.core_
* _com.unity.render-pipelines.shadergraph_
* _com.unity.render-pipelines.high-defintion_