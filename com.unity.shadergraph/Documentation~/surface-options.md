# Modify surface options without changing your graph

## Description

Enable **Allow Material Override** to modify a specific set of properties for Universal Render Pipeline Lit and Unlit Shader Graphs and for Built-In Render Pipeline Shader Graphs in the Material Inspector.

This functionality makes it possible for you to override the following Universal Render Pipeline Lit shader properties:

* Workflow Mode
* Surface Type
* Render Face
* Depth Write
* Depth Test
* Alpha Clipping
* Cast Shadows
* Receive Shadows

These are the Universal Render Pipeline Unlit shader properties you can override:

* Surface Type
* Render Face
* Depth Write
* Depth Test
* Alpha Clipping
* Cast Shadows

The Universal Render Pipeline documentation for [Unlit](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/unlit-shader.html) and [Lit](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/lit-shader.html) shaders explains these properties, which correspond to specific [ShaderLab](https://docs.unity3d.com/Manual/SL-Reference.html) tags and commands that are also applicable to the Built-In Render Pipeline.

## How to use

To use the Material Override feature:
1. Create a new graph in Shader Graph.
2. Save this graph.
3. Open the [Graph Inspector](Internal-Inspector.md).
4. Set **Active Targets** to **Universal** or **Built In**.
5. In the Graph Inspector’s **Universal** or **Built In** section, enable **Allow Material Override**.
6. Create or select a Material or GameObject which uses your Shader Graph.
7. In the Material Inspector, modify **Surface Options** for the target Material or GameObject.
