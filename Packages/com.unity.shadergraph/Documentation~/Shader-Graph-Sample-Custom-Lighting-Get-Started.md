# Get started with the Custom Lighting sample

Use the templates and sub graphs included in the **Custom Lighting** sample to get started with lighting model customization in Shader Graph.

> [!NOTE]
> To use the **Custom Lighting** templates and sub graphs, you first need to [import the sample in your project](ShaderGraph-Samples.md#add-samples).

## Start from a shader graph template

To use one of the available Custom Lighting templates as a starting point to customize lighting in a prebuilt shader graph asset, follow these steps:

1. [Create a shader graph asset from a template](create-shader-graph.md).

1. In the [template browser](template-browser.md), select one of the templates from the **URP Custom Lighting** section.

1. Select **Create**.

1. Open the created shader graph asset in the Shader Graph window if the window doesn't open automatically.

## Start from an empty shader graph

To use one of the available Lighting Model sub graphs as a starting point to customize lighting in an empty shader graph asset, follow these steps:

1. [Create a shader graph asset with a preset target](create-shader-graph.md#create-a-shader-graph-with-a-preset-target) and select **URP** > **Unlit Shader Graph**.

1. Open the created shader graph asset in the Shader Graph window if the window doesn't open automatically.

1. Go to the [**Create Node** menu](Create-Node-Menu.md) and add one of the sub graphs from the **Custom Lighting** > **Lighting Models** section.

1. Connect the **Lit** output port of the added sub graph to the **Base Color** input port of the Master Stack.

1. To allow the Unlit target to correctly support custom lighting models, set the [Graph Settings](Graph-Settings-Tab.md) as follows:
   * Enable **Keep Lighting Variants**.
   * Disable **Default Decal Blending** and **Default SSAO**.

## Additional resources

* [Create a shader graph asset](Create-Shader-Graph.md)
* [Add and connect nodes](Create-Node-Menu.md)
