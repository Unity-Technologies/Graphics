# Using the Compositor
To open the Compositor window in the Unity Editor, select **Window > Render Pipeline > HD Render Pipeline Compositor** from the menu. From this window, you can control the Compositor configuration. The first time you open the Compositor window, it automatically uses a default *"pass-through"* composition profile that forwards the output of the main [Camera](HDRP-Camera.md) to the final composed frame. You can edit this profile or you can load another one from disk. For information on the properties in the Compositor window, see [Compositor window](Compositor-User-Options.md).


## Layer Types
The HDRP Compositor tool typically handles two types of layers: 
- **Composition Layers**: Which you define in the [Composition Graph](#composition-graph). The Composition Graph defines the number of layers and how to combine them but does not define each layer's content.
- **Sub-layers**: Which you define in the Compositor window, in the [Render Schedule](#render-schedule) section. You use stack Sub-layers to define the content of a Composition Layer.


## Composition Graph
To specify the output image, the Compositor uses a graph of compositing operations. Specifically, the Compositor uses the [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html) with an [Unlit Master Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Unlit-Master-Node.html) as its target. To specify the output image, the Compositor uses the value you connect to the **Color** port. You do not need to connect any other ports on the Master Node.

**Note**: When the output of the composition is to a render target, the Material you create from the Master Node must be double-sided.

When you create a Composition Graph, there are two main types of input property you can expose:
- **Composition Layer**: Any **Texture2D** properties act as Composition Layers which correspond to a layer the graph composites to generate the final frame. These properties appear automatically as Composition Layers in the [Render Schedule](#render-schedule) section of the Compositor window. The **Mode** option for them in Shader Graph corresponds to the default value the Shader uses when you toggle off the visibility of the layer in the Render Schedule list.<br/> **Note**: By default, this value is set to white, but for many compositing operations and behaviors, you may want to set this to black instead.
- **Composition Parameters**: This refers to any exposed property that is not a Texture2D. Composition Parameters can control various aspects of the composition. Examples of Composition Parameters include a Vector1 input to control brightness or a Color input to tint a Texture2D. These properties appear automatically in the [Composition Parameters](#composition-parameters) section of the Compositor window.

The following graph contains examples of the property types described above. The **Logo** property is an example of a Composition Layer and the **Opacity** property is an example of an input property to control an aspect of the composition:

![](Images/Compositor-CompositorSimpleGraph.png)

Unity saves the Compositor settings in a .asset file with the same name as the Composition Graph. When the Compositor loads a Composition Graph, it also loads the settings from the corresponding Asset file if one exists, otherwise, it creates a new Asset with default settings.

## Adding and removing Composition Layer
To add a new Composition Layer, create a new Texture2D input property in the [Composition Graph](#composition-graph). When you next save the Composition Graph, the new layer appears automatically in the [Render Schedule](#render-schedule) section of the Compositor window. From there, you can control the [layer properties](Compositor-User-Options.md#composition-layer-properties) and specify how to [fill the layer with content](#Sub-layers:-adding-content-to-composition-layers). 

Similarly, to delete a Composition Layer, remove the corresponding Texture 2D property from the [Composition Graph](#composition-graph).

## Sub-layers: Adding content to Composition Layers
Each Composition Layer can source its content from one or more Sub-layers. There are three types of Sub-layer:
- **Camera Sub-layer:** The source of the content is a Unity Camera. You can select which Camera to use in the properties of the Sub-layer.
- **Video Sub-layer:** The source of the content is a Unity Video Player. You can select which Video Player to use in the properties of the Sub-layer.
- **Image Sub-layer:** The source of the content is a static image. You can select which image to use in the properties of the Sub-layer.

To add a Sub-layer to a Composition Layer, select the Composition Layer and click the **Add** drop-down button. From the drop-down, you can select the type of Sub-layer.

To remove a Sub-layer, select the Sub-layer and click the **Delete** button.<br/>**Note**: You can only delete Sub-layers this way and not Composition Layers. Instead, to delete a Composition Layer, remove the corresponding Texture2D property from the Composition Graph. 

## Camera Stacking
When you use more than one Sub-layer to specify the content of a Composition Layer, this "stacks" the Sub-layers on top of the same render target. To specify the size and format of the composition, you use the properties of the parent Composition Layer. The Sub-layers inherit the size and format from their parent Composition Layer and you cannot change these properties independently for a particular Sub-layer. This means every stacked Camera/Sub-layer has the same size and format. To change the stacking order, re-arrange the Sub-layers in the [Render Schedule](#render-schedule) section of the Compositor window.

The [Sub-layer Properties](Compositor-User-Options.md#Sub-layer-properties) section controls the type of stacking operation.

## Render Schedule
The Render Schedule is a re-orderable list of Composition Layers and Sub-layers. Sub-layers appear indented below their corresponding parent Composition Layer, which makes it easier to see the hierarchical relationship. When multiple Sub-layers appear below a parent layer, they form a camera stack. Unity renders layers at the top first. To re-order the list, you can click and move both Composition Layers and Sub-layers. You can use this to change the rendering order in a camera stack or move a Sub-layer from one parent Composition Layer to another.

## Composition Parameters
This sections shows every exposed property that is not an input Composition Layer (for example, a Vector1 to control the brightness of the final composition or a Color to tint a Texture2D). In this section, the window allows you to edit each property value outside of the Composition Graph. It is good practice to expose properties from the graph to the Compositor window, instead of hard-coding their values. This helps you to share composition profiles between Projects because those you do not need to open the Composition Graph to edit any values.

