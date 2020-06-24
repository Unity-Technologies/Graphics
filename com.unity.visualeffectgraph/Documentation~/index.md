# Visual Effect Graph

![A visual effect made with the Visual Effect Graph.](Images/VisualEffectGraph.png)

The Visual Effect Graph enables you to author visual effects using Node-based visual logic. You can use it for simple effects as well as very complex simulations.
Unity stores Visual Effect Graphs in Visual Effect Assets that you can use on the [Visual Effect Component](VisualEffectComponent.md). You can use a Visual Effect Asset multiple times in your Scene.

## Using a Visual Effect Graph
Use a Visual Effect Graph to:
* Create one or multiple Particle Systems.
* Add static meshes and control Shader properties.
* Create properties to customize the instances you use in the Scene.
* Create events to turn parts of your effect on and off. You can then send these events from the Scene via C# or [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html).
* Extend the library of features by creating sub-graphs of the Nodes that you commonly use.
* Use a Visual Effect Graph in another Visual Effect Graph. For example, you can reuse and customize a simple but configurable explosion in more complex graphs.
* Previews changes immediately, so you can simulate effects at various rates and perform step-by-step simulation.
For instructions on how to install the Visual Effect Graph, see [Getting started with Visual Effect Graph](GettingStarted.md).
