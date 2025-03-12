# Visual Effect (Component)

The Visual Effect Component creates an instance of a Visual Effect in the scene, based on a Visual Effect Graph Asset. It controls how the effect plays, renders and let the user customize the instance by editing [Exposed Properties](Blackboard.md).

## How to create a Visual Effect

To create a Visual Effect:

1. Add the Visual Effect component using the **Add Component** menu in the Inspector or  navigate toi **Component > Effects > Visual Effect**
2. Click the **New** Button, next to the Asset Template property Field.
3. Save the new Visual Effect Graph asset.

The visual effect graph window then opens the newly created asset.

To create a complete Game Object that contains a Visual Effect Component, navigate to the GameObject menu, click **Visual Effects** and select **Visual Effect**.

When you drag a Visual Effect Graph Asset from the project view to the scene view or hierarchy view it automatically creates a child Game Object with Visual Effect Component:

* When dropped in the Scene View : In the center of screen in front of the camera.
* When dropped in the Hierarchy under no Parent Game Object : At  the origin of the world.
* When dropped in the Hierarchy under a Parent Game Object : At the parent's transform.

## The Visual Effect Inspector

The Visual Effect Inspector helps you configure every instance of a Visual Effect. It displays values only relevant to this particular instance.

| Item               | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| Asset Template     | Object Field that references the Visual Effect Graph Unity uses for this Instance.<br/><br/>The **New/Edit** button allows you to create a new visual effect graph asset or edit the current one. When you click **New/Edit**, Unity opens the Visual Effect Graph asset and connects this scene instance to the Target Game Object panel. |
| Random Seed        | An Integer Field that displays the current random seed used for this instance. The **Reseed** button generates a new random seed for this component. |
| Reseed On Play     | Boolean setting that computes a new seed at random every time Unity sends the Play Event to the Visual Effect. |
| Initial Event Name | Allows Unity to override the Default Event name (string) sent to the component when it is enabled. (Default : *OnPlay* ). |

#### Rendering properties

Rendering properties control how the visual effect instance will render and receive lighting. These properties are stored per-instance in the scene and do not apply modifications to the Visual Effect Graph.

| Item                  | Description   |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Priority              | Controls the Transparency ordering of the effect.  This property only appears if the Project uses the High Definition Render Pipeline.                                                                                                                                                      |
| Rendering Layer Mask  | This property functions differently depending on which render pipeline your Project uses. <br/>&#8226; **High Definition Render Pipeline**: Controls the Lighting Layer Mask if it is configured in the HDRP Asset. <br />&#8226; **Universal Render Pipeline**: Determines which rendering layer this Renderer exists on. |
| Reflection Probes     | Specifies how reflections in the Scene affect the Renderer.  This property only appears if the Project uses the Universal Render Pipeline.                                                                                                                                                  |
| Light Probes          | Controls the Use of Light probes to compute the Ambient Lighting of the Effect.                                                                                                                                                                                                             |
| Anchor Override       | (Visible Only using Blend Probes option for Light Probes) : Defines an alternative transform to compute the position of the probe sampling.                                                                                                                   |
| Proxy Volume Override | (Visible Only using Proxy Volume option for Light Probes) : Defines an alternative Light Probe Proxy volume in order to compute the probe sampling.                                                                                                                                         |
| Sorting Layer         | Specifies the Renderer's group among other [SpriteRenderer](https://docs.unity3d.com/ScriptReference/SpriteRenderer.html) components.                                                                           |
| Order in Layer        | Specifies the Renderer's order with a sorting layer relative to other [SpriteRenderer](https://docs.unity3d.com/ScriptReference/SpriteRenderer.html) components. See also [Renderer.sortingOrder](https://docs.unity3d.com/ScriptReference/Renderer-sortingOrder.html).     |

#### Instancing properties

Instancing properties control how the visual effect instance is used by the [Instancing](Instancing.md) feature.

| Item               | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| Allow instancing   | Allow the Instancing feature to group this instance with others as a batch, to improve performance. Defaults to *true*. |

#### Properties

The properties category display any Property that is defined in the Visual Effect Graph Blackboard as an **Exposed Property**. Every property can be overridden from its default value in order to customize the Visual Effect instance in the scene. Some properties can also be edited using Gizmos directly in the scene.

| Item                 | Description                                                  |
| -------------------- | ------------------------------------------------------------ |
| Show Property Gizmos | Toggles the display of the editing gizmos used to set up some exposed properties (Spheres, Boxes, Cylinders, Transforms, Positions). You can access each gizmo using its dedicated button next to its property. |
| Properties           | All properties that have been exposed in the Visual Effect Graph Asset.  You can edit these properties for this instance of the Visual Effect. For more information see [Exposed Properties](Blackboard.md#exposed-properties-in-inspector) |

To access property values, edit them using the Inspector, use the [C# API](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect.html), or use Property Binders.

## The Play Controls Window

The Play Controls window displays UI Elements that give you control over the currently selected instance of a Visual Effect. It is displayed in the bottom-right corner of the Scene View, when a Visual Effect Game Object is selected.

The play Controls Window displays the following controls:

* Stop (Button) : Resets the effect and set its state to paused.
* Play / Pause (Button) : Toggles the paused state of the effect.
* Step (Button) : Pauses the effect and simulates one frame.
* Restart (Button) : Un-pauses the effect, resets it, and sends the default Play Event.
* Rate (Int Slider) : Sets the play rate of the effect (in percent)
* Set (Popup) : Sets a custom play rate of the effect from the menu.
* Show Bounds (Toggle) : Toggles visibility of the bounds of the effect
* Show Event Tester (Toggle) : Shows Event Tester Utility Window
* Play() and Stop() Buttons : Sends the default OnPlay and OnStop event to the component.
* (Optional) Gizmos (Popup) : Toggles the visibility of property gizmos.

## Editing Properties with Gizmos

You can edit some properties using Gizmos in the scene. In order to enable gizmo editing, click the **Show Property Gizmos** button in the Inspector. Upon enabling property Gizmos, every property that can be edited using Gizmos will display **Edit Gizmo** buttons next to every property that can be edited using gizmos.

