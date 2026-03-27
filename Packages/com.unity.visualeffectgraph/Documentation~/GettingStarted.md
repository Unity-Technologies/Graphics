# Getting started with Visual Effect Graph

This page shows you how  to install Visual Effect Graph, and gives a brief overview of how to [create](#creating-visual-effect-graphs), [edit](#editing-a-visual-effect-graph), and [preview](#previewing-a-graph-s-effects) effects with Visual Effect Graph. For an overview of how the graph works, see [Graph Logic and Philosophy](GraphLogicAndPhilosophy.md).
Visual Effect Graph is a Unity package that uses a [Scriptable Render Pipeline](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html) to render visual effects, and uses compute Shaders to simulate effects.

## Requirements
For information on system requirements for the Visual Effect Graph, see [Requirements and compatibility](System-Requirements.md).

## Installing Visual Effect Graph

To install the Visual Effect Graph package:

1. Open a Unity project.
1. Open the **Package Manager** window (**Window** > **Package Manager**).
1. In the Package Manager window, in the **Packages** field, select **Unity Registry**.
1. Select **Visual Effect Graph** from the list of packages.
1. In the bottom right corner of the Package Manager window, select **Install**. Unity installs Visual Effect Graph into your Project.

**Note:** When using [HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/index.html), VFX Graph is included with  [HDRP Package](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/install-hdrp.html#install-the-high-definition-rp-package) and doesn't need to be manually installed

### Using the correct version of Visual Effect Graph
Every Visual Effect Graph package works with a Scriptable Render Pipeline package of the same version. If you want to upgrade the Visual Effect Graph package, you must also upgrade the render pipeline package that you’re using.

For example, the Visual Effect Graph package version 6.5.3-preview in Package Manager works with the High Definition RP package
version 6.5.3-preview.

## Creating Visual Effect Graphs

To use Visual Effect Graph, you must first create a [Visual Effect Graph asset](VisualEffectGraphAsset.md).

### From the Editor menu

1. From the main menu, select **Assets** > **Create** > **Visual Effects** > **Visual Effect Graph**.
1. Select a template as a starting point for your new visual effect.
1. Select **Create**.

### From a GameObject

1. Select the GameObject and add a [Visual Effect component](VisualEffectComponent.md).
1. Next to the **Asset Template** property, Select **New**.
1. Select a template as a starting point for your new visual effect.
1. Select **Create**.

### From the Visual Effect Graph window

1. From the main menu, select **Window** > **Visual Effect** > **Visual Effect Graph** to open the Visual Effect Graph window.
1. Select the **Create New Visual Effect Graph** button. 
1. Select a template as a starting point for your new visual effect.
1. Select **Create**.

## Using Visual Effect Graphs in Scenes

To add a Visual Effect in a scene, you can:

* Drag and drop a Visual Effect Graph Asset from the Project Window into the Hierarchy Window. <br />When you drop the Asset on an existing GameObject, this adds a new child GameObject with a Visual Effect Component, and assigns the graph to it. <br />When you drop the Asset on an empty space, Unity creates a new Visual Effect GameObject and assigns the graph to it.
* Drag and drop a Visual Effect Graph Asset from the Project Window to the Scene View Window. This makes the graph appear in front of the Camera.

When you’ve added the Visual Effect Graph Asset to the Hierarchy, Unity attaches the Asset to a [Visual Effect Component](VisualEffectComponent.md), which references the Asset.

If you created a Visual Effect Asset directly from the Visual Effect Component in the scene, as described in the previous section, everything is already set up and ready to use.

## Editing a Visual Effect Graph
To edit Visual Effect Graph Assets in the  [Visual Effect Graph window](VisualEffectGraphWindow.md) :

* Open the Visual Effect Graph window (menu: **Window** > **Visual Effects**) with an empty graph. This prompts you to open a Visual Effect Graph Asset.
* Select an existing Visual Effect Graph Asset, and click the **Edit** button in the Inspector. This opens the Visual Effect Graph window with the graph contained in this Asset.
* Select the Visual Effect component (menu: next to the Asset template, click **Edit**). This opens the Visual Effect Graph window and with the graph contained in the referenced Asset.

## Previewing a graph’s effect
To preview an effect, you can:

* Select a Visual Effect Graph Asset and use the Inspector Preview window.

* Place your effect directly in the Scene as a Visual Effect GameObject.


This lets you edit parameters directly in the Scene and see the lighting on your effect. It also allows you to attach an effect from the scene to an opened graph.


## Attaching a Visual Effect from the scene to the current graph
When you attach a Visual Effect in your scene to the current graph, you can use the [Control Panel](VisualEffectGraphWindow.md#TargetGameObject) and [Debug Panel](performance-debug-panel.md) features for the specific target instance of your effect. 
This also allows Unity to display the correct gizmos in the scene, which makes some aspects of your effect easier to edit. 

To attach a Visual Effect to the opened graph, you can either select the GameObject in the hierarchy, or follow these steps:
1. In the matching graph, open the **Auto Attach Panel** from the [Toolbar](VisualEffectGraphWindow.md#Toolbar). 
1. Click on the**Select a target** field to select a compatible GameObject that exists in the current open scene.

## Manipulating graph elements
When you open an Asset inside the Visual Effect Graph window, you can see and edit the graph for that specific Asset.

A Visual Effect Graph contains [Operator Nodes](Operators.md) and [Blocks](Blocks.md). Each Node is in charge of processing its input properties. You can link Nodes together to perform a series of calculations. All Nodes end up connecting into a Block (or a Context) : A Block defines an operation on an effect, based on its input properties.

When you link several Blocks together, these form a Context. For more information about Nodes, Blocks, and Contexts in the Visual Effect Graph, see [Graph Logic](GraphLogicAndPhilosophy.md).

Every change you make to a graph has immediate consequences on the behavior of your effect, and you can preview the changes in real time. Every time you add, remove, or connect a Node, the graph recompiles all the elements that have changed, and restarts the effect. However, changing values (for example, editing a curve) does not make Unity recompile anything and affects the simulation in real time.
To add Nodes, you can either:

* Right-click in the graph, and select **Create Node**.
* Press the spacebar on your keyboard.
* Click and drag an edge from an existing port, and release the click in an empty space.
* Drag and drop an element from the [Blackboard](Blackboard.md) into the graph.

When you do any of the above actions, the **Create Node** menu appears. Here, you can see the Nodes, Blocks, and Contexts that are compatible with that specific location in the graph.

## Additional resources

* [VFX Graph template window reference](Templates-window.md)
* [Visual Effect Graph window reference](VisualEffectGraphWindow.md)
* [Visual Effect Graph Asset reference](VisualEffectGraphAsset.md)
