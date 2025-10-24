# Create a shader graph asset

You can create a new shader graph asset in different ways according to your current workflow.



## Create a shader graph with a preset target

To start from a default configuration with a preset master stack according to a specific render pipeline and material type, follow these steps:

1. In the **Project** window, right-click and select **Create** > **Shader Graph**, and then the target render pipeline and the desired shader type.

   The types of shader graphs available depend on the render pipelines present in your project (for example, **URP** > **Lit Shader Graph**). For a full list of provided options, refer to the [Universal Render Pipeline](https://docs.unity3d.com/Manual/urp/urp-introduction.html) and [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest) documentation.

   Unity creates a new shader graph asset in your project.

1. Name the shader graph asset.

You can now open the asset and edit the graph in the [Shader Graph window](Shader-Graph-Window.md).


## Create an empty shader graph

To create an empty shader graph asset and build your shader graph from scratch in the Shader Graph window:

1. In the **Project** window, right-click and select **Create** > **Shader Graph** > **Blank Shader Graph**.

   Unity creates a new shader graph asset in your project.

1. Name the shader graph asset.

You can now open the asset and edit the graph in the [Shader Graph window](Shader-Graph-Window.md).

> [!NOTE]
> To make such a blank shader graph functional, you have to define a [Target](Graph-Target.md) in the [Graph settings tab](Graph-Settings-Tab.md) of the Graph Inspector.

## Additional resources

* [Shader Graph window](Shader-Graph-Window.md)
