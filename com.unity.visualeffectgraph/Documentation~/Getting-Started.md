# Getting Started

## Assets and Scene Objects

* Visual Effects are stored in assets called **Visual Effect Graphs** , they can be created in the project view using the Create Menu (or right-click/create), and reside under the *Visual Effects* category.

* Visual Effects are instantiated in the scene using **Visual Effect** components that reference a Visual Effect Graph asset.

![](Images/vfx-asset-component.png)

## Editing Assets

You can edit Visual Effect Graph assets using various methods:

* By Opening the Visual Effect Graph Window (Window > Visual Effects > Visual Effect Graph) then double clicking a Visual Effect Graph
* By Clicking the Open button in the inspector header of a Visual Effect Graph asset
* By clicking the **Edit** Button, next to the Asset Template field of a Visual Effect Component. This will also connect the Target GameObject panel in the Visual Effect Graph window.

## Visual Effect Graph window

The visual effect graph window holds the logical graph for one or many systems. The canvas can be panned using the middle mouse or alt+drag, be zoomed in and out using the mouse wheel, and can focus nodes using the F key.

![](Images\graphWindow.PNG)

To bring the add node menu, you can `right click > add node / create block`

> Note: the menu is contextual and will only provide what can be created under the cursor.

![](Images\add-node-block.gif)

Nodes /Blocks can also be created:

* Using the spacebar (shortcut to bring the menu)
* By making connections then releasing the click in empty space

## Adding a template system

Systems can be created by using the System category in the add node menu. There are a few templates available depending on the needs (empty, simple, swarm with thousands of particles, or simple static mesh)

![](Images/create-system.gif)

## Graph Logic

### Systems, Contexts and Blocks

When creating a system, a series of **contexts** is vertically connected, and executed from top to bottom.

* Spawn handles how many particles will be spawned
* Initialize will be executed for every new particle
* Update will run for every particle, every frame
* Render will give shape to every particle every frame.

You can add **Blocks** to a context in order to define its behavior. Blocks are executed for each context from top to bottom too.

![](Images\execution-order-attribute.gif)

More information in the [Systems, Contexts and Blocks](Systems-Contexts-and-Blocks)

### Operators

You can use operators to perform mathematical computations to set up slots in blocks or other operators. You can perform math operations, sample curves or textures, access exposed parameters or particle attributes using these operators.

![](Images\operators.png)

More information in the [Operators](Operators) page

## Parameters and Events

#### Parameters

You can customize your Effect instances using a Parameter Interface. These parameters are created in the Blackboard Window that can be accessed via the Toolbar. Parameters can be created and set into categories.

![](Images\blackboard.PNG)

More info in [Parameters and Events](Parameters-and-Events)

#### Events

Events can be created using an **Event Context**. Events are the flow logic that will trigger spawn contexts.

By default, Spawn contexts are triggered by implicit `OnPlay` and `OnStop` events but you can customize this behavior by creating custom named events. 

These events can be triggered externally on the component by using the C# API or Timeline.

![](Images\implicit-events-spawner.PNG)

## Getting Templates

You can find a template project at this address : https://github.com/Unity-Technologies/VisualEffectGraph-Samples

This project contains a set of effects and scenes to learn from.