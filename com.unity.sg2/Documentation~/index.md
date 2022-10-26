# About Shader Graph

## Description

Shader Graph enables you to build shaders visually. Instead of writing code, you create and connect nodes in a graph framework. Shader Graph gives instant feedback that reflects your changes, and it’s simple enough for users who are new to shader creation.

For an introduction to Shader Graph, see [Getting Started](Getting-Started.md).

Shader Graph is available through the Package Manager window in supported versions of the Unity Editor. If you install a Scriptable Render Pipeline (SRP) such as the [Universal Render Pipeline (URP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest) or the [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest), Unity automatically installs Shader Graph in your project.

Shader Graph package versions on Unity Engine 2018.x are *Preview* versions, which do not receive bug fixes and feature maintenance. To work with an actively supported version of Shader Graph, use Unity Engine 2019.1 or higher.

### SRP packages are part of the core

With the release of Unity 2021.1, graphics packages are relocating to the core of Unity. This move simplifies the experience of working with new Unity graphics features, as well as ensuring that your projects are always running on the latest verified graphics code.

For each release of Unity (alpha / beta / patch release) graphics packages are embedded within the main Unity installer. When you install the latest release of Unity, you also get the latest [Universal Render Pipeline (URP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest), [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest), Shader Graph, [Visual Effect (VFX) Graph](https://docs.unity3d.com/Packages.com.unity.visualeffectgraph@latest) packages, among others.

Tying graphics packages to the main Unity release allows better testing to ensure that the graphics packages you use have been tested extensively with the version of Unity you have downloaded.

You can also use a local copy or a custom version of the graphics packages with an override in the manifest file.

For more information, see the following post on the forum: [SRP v11 beta is available now](https://forum.unity.com/threads/srp-v11-beta-is-available-now.1046539/).
