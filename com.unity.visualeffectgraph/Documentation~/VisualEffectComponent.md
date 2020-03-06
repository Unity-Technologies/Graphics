<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>
# Visual Effect (Component)

The Visual Effect Component creates an instance of a Visual Effect in the scene, based on a Visual Effect Graph Asset. It controls how the effect plays, renders and let the user customize the instance by editing [Exposed Properties](PropertiesAndBlackboard.md#exposed-properties)

![Visual Effect Component](Images/VisualEffectComponent.png)

## How to create a Visual Effect

In order to create a Visual Effect, you can manually add the component via the Add Component Menu in the Inspector or in the menu : Component / Effects / Visual Effect. 

You can also create a complete Game Object holding a Visual Effect Component by using the GameObject menu under the Category Visual Effects and Selecting Visual Effect.

Finally, When you drag a Visual Effect Graph Asset from the project view to the scene view or hierarchy view. It will create automatically a child Game Object with Visual Effect Component:

* When dropped in the Scene View : At center of screen in front of the camera, 
* When dropped in the Hierarchy under no Parent Game Object : At  the origin of the world 
* When dropped in the Hierarchy under a Parent Game Object : At the parent's transform

## The Visual Effect Inspector

The Visual Effect Inspector helps you configure every instance of a Visual Effect. It displays values only relevant to this particular instance.

| Item               | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| Asset Template     | Object Field that references the Visual Effect Graph being used for this Instance. (Edit Button Opens the Graph and Connects this instance to the Target Game Object panel) |
| Random Seed        | Integer Field that displays the current random seed used for this instance. (Reseed button enables computing a new random seed for this component) |
| Reseed On Play     | Boolean setting that computes a new seed at random every time the Play Event is sent to the Visual Effect |
| Initial Event Name | Enables overriding the Default Event name (string) sent to the component when it becomes enabled. (Default : *OnPlay* ) |

#### Rendering Properties

Rendering properties controls how the visual effect instance will render and receive lighting. These properties are stored per-instance in the scene and do not apply modifications to the Visual Effect Graph.

| Item                  | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| Transparency Priority | **High Definition SRP Only**: Controls the Transparency ordering of the effect. |
| Lighting Layer Mask   | **High Definition SRP Only**: Controls the Lighting Layer Mask, if it is configured in the High Definition SRP Asset. |
| Light Probes          | Controls the Use of Light probes to compute the Ambient Lighting of the Effect. |
| Anchor Override       | (Visible Only using Blend Probes option for Light Probes) : Defines an alternative transform to compute the position of the probe sampling. |
| Proxy Volume Override | (Visible Only using Proxy Volume option for Light Probes) : Defines an alternative Light Probe Proxy volume in order to compute the probe sampling. |

#### Properties

The properties category display any Property that have been defined in the Visual Effect Graph Blackboard as Exposed Property. Every property can be overridden from its default value in order to customize the Visual Effect instance in the scene. Some properties can also be edited using Gizmos directly in the scene.

| Item                 | Description                                                  |
| -------------------- | ------------------------------------------------------------ |
| Show Property Gizmos | Toggles the display of the editing gizmos used to set up some exposed properties (Spheres, Boxes, Cylinders, Transforms, Positions). Each gizmo can then be accessed using its dedicated button next to its property. |
| Properties           | All properties that have been exposed in the Visual Effect Graph Asset.  You can edit these properties for this instance of the Visual Effect. For more information see [Exposed Properties](Blackboard.md#exposed-properties-in-inspector) |

In order to access property values you can edit them using the Inspector, use the [C# API](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect.html) or use Property Binders.

## The Play Controls Window

The Play Controls window displays UI Elements that enable control over the currently selected instance of a Visual Effect. It is displayed in the bottom-right corner of the Scene View, when a Visual Effect Game Object is selected.

![](Images/PlayControls.png)

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

Some Properties can be edited using Gizmos in the scene. In order to enable gizmo editing, click the **Show Property Gizmos** button in the Inspector. Upon enabling property Gizmos, every property that can be edited using Gizmos will display **Edit Gizmo** buttons next to every property that can be edited using gizmos.

![Property Gizmos Inspector](Images/PropertyGizmosInspector.png)