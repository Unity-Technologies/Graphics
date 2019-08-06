# About Shader Graph

## Description

Shader Graph enables you to build shaders visually. Instead of writing code, you create and connect nodes in a graph framework. Shader Graph gives instant feedback that reflects your changes, and it’s simple enough for users who are new to shader creation.

For an introduction to Shader Graph, see [Getting Started](Getting-Started.md).

Shader Graph is available through the Package Manger window in Unity versions 2018.1 and higher. If you install a prebuilt Scriptable Render Pipeline (SRP) such as the [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.lightweight@latest) (URP) or the [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest) (HDRP), Unity automatically installs Shader Graph in your project.

Avoid installing or updating Shader Graph independently of the prebuilt SRP packages. Shader Graph builds shaders that are compatible with the URP and HDRP, but they are not compatible with the built-in renderer.

Shader Graph package versions on Unity Engine 2018.x are *Preview* versions, which do not receive bug fixes and feature maintenance. To work with an actively supported version of Shader Graph, use Unity Engine 2019.1 or higher.