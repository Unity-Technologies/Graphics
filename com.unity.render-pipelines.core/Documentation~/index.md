# Scriptable Render Pipeline
![](https://blogs.unity3d.com/wp-content/uploads/2018/01/image5_rs.png)
## What is the Scriptable Render Pipeline

The Scriptable Render Pipeline (SRP) is a feature that gives you full control over Unity's render pipeline and provides the tools you need to create modern, high-fidelity graphics in Unity.

SRP allows you to write C# scripts to control the way Unity renders each frame. Exposing the render pipeline to you in C# makes Unity less of a “black box” when it comes to rendering. Unlike the original built-in render pipeline, SRP allows you to see and control exactly what happens during the rendering process. 

Unity provides you with two prebuilt Scriptable Render Pipelines which you can use in your Project as a base for your own custom SRP:
* The Universal Render Pipeline (URP) offers graphics that scale from mobile platforms to higher-end consoles and PCs.
* The High Definition Render Pipeline (HDRP) utilizes physically-based lighting techniques to offer high-fidelity graphics to target modern, Compute Shader compatible, platforms.

Rather than developing your own SRP from scratch, you can use either of these prebuilt SRPs as a base to modify and adapt to your own requirements.

## What is SRP Core

SRP Core is a collection of API's that exposes many of the rendering internals and configuration to you. This allows you to configure exactly how you want rendering to work in your project.

The SRP API offers a new interface to many familiar Unity constructs which include:

- Lights
- Materials
- Cameras
- Command Buffers

The thing that SRP changes is the *way* that you interact with Unity. For performance reasons, when you write a custom SRP, you work with groups of renderers, not individual items.







