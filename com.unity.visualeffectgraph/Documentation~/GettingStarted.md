# Getting started with Visual Effect Graph

This page shows you how  to install Visual Effect Graph, and gives a brief overview of how to [create](#creating-visual-effect-graphs), [edit](#editing-a-visual-effect-graph), and [preview](#previewing-a-graph-s-effects) effects with Visual Effect Graph. For an overview of how the graph works, see [Graph Logic and Philosophy](GraphLogicAndPhilosophy.md).
Visual Effect Graph is a Unity package that uses a [Scriptable Render Pipeline](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html) to render visual effects. Visual Effect graph uses on compute Shaders to simulate effects.

## Requirements
* Unity 2018.3 or newer. Verified packages start at Unity 2019.3.
* A [Scriptable Render Pipeline](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html) package:
  * [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html)(2019.3 or newer) 
  * [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?preview=1) (2018.3 or newer. Verified packages start at Unity 2019.3.) 

**Note:** When you download the High Definition Render Pipeline package from Package Manager, Unity automatically installs the Visual Effect Graph package.
* A target device that can use compute Shaders, such as aWindows PC, Playstation 4, XBox One, or Mac running MacOS.

## Installing Visual Effect Graph

To install the Visual Effect Graph package:


1. In the Unity Editor, go to __Window__ &gt; __Package Manager__. In the top navigation bar, make sure __All packages__ is selected. 
2. Note: On version prior to Unity 2019.3, you will have to check the "Show preview packages" "Advanced" option for the Visual Effect Graph to appear in the list.
3. Select the __Visual Effect Graph__ package.
4. In the bottom right corner, click __Install__.

![](Images/InstallVisualEffectGraph.png)


### Using the correct version of Visual Effect Graph
Every Visual Effect Graph package works with a Scriptable Render Pipeline package of the same version. If you want to upgrade the Visual Effect Graph package, you must also upgrade the render pipeline package that you’re using.

For example, the Visual Effect Graph package version 6.5.3-preview in Package Manager works with the High Definition RP package
version 6.5.3-preview. 

## Creating Visual Effect Graphs
To use Visual Effect Graph, you must first create a [Visual Effect Graph Asset](VisualEffectGraphAsset.md) . 

To create a Visual Effect Graph Asset:

1. In Unity, click __Assets__ &gt; __Create__ &gt; __Visual Effects__ &gt; __Visual Effect Graph__. 

To make a copy of a Visual Effect Graph Asset:

1. In the Project window, select the Visual Effect Asset you want to make a copy of.
2. In the top navigation bar, select __Edit__ &gt; __Duplicate__.  You’ve now created a copy.

## Using Visual Effect Graphs in Scenes
To use a Visual Effect Graph, you must add a [Visual Effect](#Creating-Visual-Effect-Graphs) to the Scene. 

To do so, you can:

* Drag and drop a Visual Effect Graph Asset from the Project Window into the Hierarchy Window. <br />When you drop the Asset on an existing GameObject, this adds a new child GameObject with a Visual Effect Component, and assigns the graph to it. <br />When you drop the Asset on an empty space, Unity creates a new Visual Effect GameObject and assigns the graph to it.
* Drag and drop a Visual Effect Graph Asset from the Project Window to the Scene View Window. This makes the graph appear in front of the Camera.

When you’ve added the Visual Effect Graph Asset to you Hierarchy, Unity attaches the Asset to a [Visual Effect Component](VisualEffectComponent.md), which references the Asset. 

## Editing a Visual Effect Graph
To edit Visual Effect Graph Assets in the  [Visual Effect Graph window](VisualEffectGraphWindow.md) :

* Open the Visual Effect Graph window (menu: __Window___ &gt; __Visual Effects__) with an empty graph. This prompts you to open a Visual Effect Graph Asset.
* Select an existing Visual Effect Graph Asset, and click the __Edit__ button in the Inspector. This opens the Visual Effect Graph window with the graph contained in this Asset.
* Select the Visual Effect component (menu: next to the Asset template, click __Edit__). This opens the Visual Effect Graph window and with the graph contained in the referenced Asset.

## Previewing a graph’s effect
To preview an effect, you can:

* Select a Visual Effect Graph Asset and use the Inspector Preview window. 

* Place your effect directly in the Scene as a Visual Effect GameObject. 

This lets you edit parameters directly in the Scene, see the lighting on your effect, and use the [Target GameObject Panel](VisualEffectGraphWindow.md#target-visual-effect-gameobject) features for the specific target instance of your effect.

## Manipulating Graph Elements
When you open an Asset inside the Visual Effect Graph window, you can see and edit the graph for that specific Asset.

A Visual Effect Graph contains [Operator Nodes](Operators.md) and [Blocks](Blocks.md). Each Node is in charge of processing its input properties. You can link Nodes together to perform a series of calculations. All Nodes end up connecting into a Block (or a context) : A Block defines an operation on an effect, based on its input properties. 

When you link several Blocks together, these form a context. For more information about Nodes, Blocks, and contexts in the Visual Effect Graph, see [Graph Logic](GraphLogicAndPhilosophy.md). 

Every change you make to a graph has immediate consequences on the behavior of your effect, and you can preview the changes in real time. Every time you add, remove, or connect a Node, the graph recompiles all the elements that have changed, and restarts the effect. However, changing values (for example, editing a curve) does not make Unity recompile anything and affects the simulation in real time.
To add Nodes, you can either:

* Right-click in the graph, and select __Create Node__.
* Press the spacebar on your keyboard.
* Click and drag an edge from an existing port, and release the click in an empty space.

When you do any of the above actions, the __Create Node__ menu appears. Here, you can see the Nodes, Blocks, and contexts that are compatible with that specific location in the graph.