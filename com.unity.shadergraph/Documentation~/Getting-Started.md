# Getting Started with Shader Graph

## What is a Shader Graph?

A [Shader Graph](Shader-Graph) enables you to build your shaders visually. Instead of hand writing code you create and connect [Node](Node.md) in a graph network. You can do things like:

- Procedurally alter your surface appearance
- Warp and animate UVs
- Modify the look of your objects using familiar image adjustment operations
- Change your object’s surface based on useful information about it, its world location, normals, distance from camera, etc.
- Expose to the material inspector what you think is important to edit for your shader
- Share node networks between multiple graphs and users by creating subgraphs
- Create your own custom shader graph [Nodes](Node.md) through C# and HLSL
- The graph framework gives instant feedback on the changes, and it’s simple enough that new users can become involved in shader creation.

## How do you create Shader Graphs?

To use [Shader Graph](Shader-Graph) you must first create a [Shader Graph Asset](Shader-Graph-Asset). In Unity a [Shader Graph Asset](Shader-Graph-Asset.md) appears as a normal shader. To create a [Shader Graph Asset](Shader-Graph-Asset.md) you click the create menu in the [Project Window](https://docs.unity3d.com/Manual/ProjectView.html) and select **Shader** from the dropdown. From here you can create either a **PBR** or **Unlit** [Shader Graph Asset](Shader-Graph-Asset). This will create a [Shader Graph Asset](Shader-Graph-Asset.md) in the project. You can double click on the [Shader Graph Asset](Shader-Graph-Asset.md) or, with the [Shader Graph Asset](Shader-Graph-Asset.md) selected, select the **Open Shader Editor** button in the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) to bring up the [Shader Graph Window](Shader-Graph-Window.md).

## Editing the Shader Graph

When you open the [Shader Graph Window](Shader-Graph-Window.md) you start with the [Master Node](Master-Node.md). You connect [Nodes](Node.md) into the [Master Node](Master-Node.md) to create the look of your surface. To learn more about the underlying material models check out the existing Unity [Standard Shader](https://docs.unity3d.com/Manual/shader-StandardShader.html) documentation.

You can quickly edit your surface by changing the default values!

But, you know what’s even more exciting? Adding textures and other complex interactions. To add a node simply right click on the workspace in the [Shader Graph Window](Shader-Graph-Window.md) and select **Create Node**.

Each included [Shader Graph](Shader-Graph.md) [Node](Node.md) has a number of input [Ports](Port.md), we’ve included default values that you can customize however you like!

Adding in a **Texture** (or other assets) is also really easy, just create a [Node](Node.md) of that [Data Type](Data-Types.md) and connect it with an [Edge](Edge.md)!

Your [Shader Graph](Shader-Graph.md) shader is just like a normal shader in Unity. Right click create **Material** in the [Project Window](https://docs.unity3d.com/Manual/ProjectView.html) to create a new **Material** you can use on any object in your game. You can create multiple **Materials** from the same shader.

You can expose [Properties](Property-Types.md) in your shader so they can be overwritten in each **Material** you create from your shader. This is easy. In the shader graph right click on any variable node and select **Convert to property node** or add a new [Property](Property-Types.md) using the [Blackboard](Blackboard.md). These exposed [Properties](Property-Types.md) appear in the **Material** [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) for each **Material** you create from your shader.

To see your new [Shader Graph](Shader-Graph.md) changes affect your in game **Materials** click the **Save Asset** button in the [Shader Graph Window](Shader-Graph-Window.md)

## How do I get access to the Shader Graph?

It is recommended for users to access the [Shader Graph](Shader-Graph.md) via the **Package Manager** or via **Templates**. To use the [Shader Graph](Shader-Graph.md) in your project either start a new project using a template that includes [Shader Graph](Shader-Graph.md) or download a **Render Pipeline** package from the **Package Manager**. The [Shader Graph](Shader-Graph.md) will be downloaded automatically for your use in either of these cases. 

Packages that contain [Shader Graph](Shader-Graph.md):
- Lightweight Render Pipeline
- HD Render Pipeline

Templates that contain [Shader Graph](Shader-Graph.md):
- Lightweight 3D Template
- HD 3D Template

### Download from Github

If you wish to download via **Github** you must clone to [SRP repository](https://github.com/Unity-Technologies/ScriptableRenderPipeline) then reference these package versions directly in your project's package manifest.

##  What are the requirements for using Shader Graph

This is a feature for the new [Scriptable Render Pipeline](https://forum.unity.com/threads/feedback-wanted-scriptable-render-pipelines.470095/), available in 2018.1+. It will not work out of the box without an SRP.

We won’t be supporting this feature for the legacy renderer.

## I want more tutorials!

More will be coming (including more examples!) over the coming months. We have an end to end shader creation video [Here](https://www.youtube.com/watch?v=pmAHabxNtqU) as a starter point.