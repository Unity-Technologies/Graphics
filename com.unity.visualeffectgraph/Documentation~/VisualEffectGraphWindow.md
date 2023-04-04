# The Visual Effect Graph window

The Visual Effect Graph window is the main window for Visual Effect Graph. This is where you can edit Visual Effect Graph Assets, and Subgraph Assets. The window displays a workspace that consists of the Systems, Contexts, and Operators that a  [Visual Effect Graph Asset](VisualEffectGraphAsset.md) contains.

![VisualEffectGraph-Window](Images/vfx-graph-window.png)

## Opening the Visual Effect Graph window

To open the Visual Effect Graph window, you can use any of the following methods:

* In the Project window, double-Click a [Visual Effect Graph](VisualEffectGraphAsset.md) Asset or [SubGraph](Subgraph.md) Asset. You can also click the **Open** button in the Inspector for the respective Asset. This connects the Asset that you open to the window.
* In the Inspector for a [Visual Effect component](VisualEffectComponent.md#the-visual-effect-inspector), click the **Edit** button next to the **Asset Template** property. This connects the Asset assigned to **Asset Template** to the window.
* In the menu, select **Window > Visual Effects > Visual Effect Graph**. This opens an empty Visual Effect Graph window so you need to open a Visual Effect Graph Asset to use the editor.

## The Visual Effect Graph window layout

Inside the Visual Effect Graph window, there are multiple zones and panels.

![VisualEffectGraphWindow](Images/vfx-graph-window-details.png)

* **[Toolbar](#Toolbar)** (Red) : This bar contains controls that affect the Visual Effect Graph globally. This includes controls that specify when Unity compiles the Visual Effect Graph as well as controls that let you display/hide certain panels.
* **[Node Workspace](#NodeWorkspace)** (Green) : This is where you can view and edit the Visual Effect Graph.
* **[Blackboard](#Blackboard)** (Blue) : This panel displays the properties that the Visual Effect Graph uses.
* **[VFX Control](#TargetGameObject)** (Purple) : This panel displays controls for the GameObject currently attached.

<a name="Toolbar"></a>
### Toolbar

The Visual Effect Graph window Toolbar contains functionality to operate on a Visual Effect Graph Asset.

![Toolbar](Images/vfx-toolbar.png)

| Item                  | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Save** <br/> ![](Images/save.png)             | **Action** : Use this button to save the Visual Effect Graph that is currently open and its subgraphs.<br/>**Dropdown**:<br/><br/>&#8226; **Save as…**: Saves the Visual Effect Graph under a specified name and/or location.<br/>&#8226; **Show in Inspector**: Focuses the Visual Effect Graph's Asset in the Inspector.|
| **Compile** <br/> ![](Images/compile.png)             | **Action** : Recompiles the open Visual Effect Graph.<br/>**Dropdown**:<br/><br/>&#8226; **Auto Compile**: Automatically compiles the Visual Effect Graph.<br/>&#8226; **Auto Reinit**: Automatically reinitializes an attached component when a value changes in the **Spawner** or **Init** contexts.<br/>&#8226; **Prewarm Time**: Specifies the duration of the prewarm used with **Auto Reinit**. If the VFX already has a runtime prewarm, it ignores this setting.<br/>&#8226;**Runtime Mode**: Forces optimized compilation, even when the editor is open.<br/>&#8226; **Shader Debug Symbols**: Forces shader debug symbols generation when Unity compiles the authored VFX asset.<br/>&#8226; **Shader Validation**: Forces shader compilation when the effect recompiles, even if no visual effect is visible. This displays the Shader errors in the Scene.|
| **Auto Attach** <br/> ![](Images/auto-attach.png)             | **Toggle**: Toggles the visibility of the Auto Attachment panel. The **Auto Attachment** panel allows you to attach the open Visual Effect Graph to a GameObject by selecting it in the Hierarchy. Once you have attached the visual effect to a GameObject, it enables the Visual Effect controls in the VFX Control panel and allows you to tweak gizmos in the Scene View.|
| **Lock**           | **Toggle**: Toggles lock/unlock for auto attachments. If you set the toggle to unlocked - you can attach the open Visual Effect Graph to a GameObject by selecting items in the Hierarchy. If you set the toggle to locked - The GameObject that is currently attached becomes locked and auto attachments are disabled. The Visual Effect Graph then can not be attached by selecting items in the Hierarchy. You can manually keep the lock and change the attachment in the object picker of the Auto Attach panel.|
| **Blackboard** <br/> ![](Images/blackboard.png)             | **Toggle**: Toggles the visibility of the **Blackboard Panel**.|
| **VFX Control** <br/> ![](Images/vfx-control.png)             | **Toggle**: Toggles the visibility of the VFX Control.|
| **Help** <br/> ![](Images/help.png)             | **Toggle**: Opens the Visual Effect Graph [manual](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/).<br/><br/>**Dropdown**:<br/><br/>**VFX Graph Additions**: Installs pre-made visual effects and utility operators made with Visual Effect Graph. These include Bonfire, Lightning, Smoke and Sparks effect examples. There are also various operators and Subgraph Blocks:<br/><br/>&#8226; **Get Strip Progress**: A subgraph that calculates the progress of a particle in the strip in the range 0 to 1. You can use this to sample curves and gradients to modify the strip based on its progress.<br/><br/>&#8226; **Encompass (Point)**: A subgraph that grows the bounds of an AABox to encompass a point.<br/><br/>&#8226; **Degrees to Radians and Radians to Degrees**: Subgraphs that help you to convert between radians and degrees within your graph.<br/><br/>**Output Event Helpers**: This version of the Visual Effect Graph introduces new helper scripts to the OutputEvent Helpers sample to help you to set up OutputEvents:<br/><br/>&#8226; **Cinemachine Camera Shake**: An Output Event Handler script that triggers a Camera shake through a [Cinemachine Impulse Source](https://docs.unity3d.com/Packages/com.unity.cinemachine@latest/index.html?subfolder=/manual/CinemachineImpulseSourceOverview.html), on a given output event.<br/><br/>&#8226; **Play audio**:  An Output Event Handler script that plays a single AudioSource on a given output event.<br/><br/>&#8226; **Spawn a Prefab**:  An Output Event Handler script that spawns Prefabs (managed from a pool) on a given output event. It uses position, angle, scale, and lifetime to position the Prefab and disable it after a delay. To synchronize other values, you can use other scripts inside the Prefab:<br/><br/>&#8226; **Change Prefab Light**: An example that demonstrates how to synchronize a light with your effect.<br/><br/>&#8226; **Change Prefab RigidBody Velocity**: An example that demonstrates how to synchronize changing the velocity of a RigidBody with your effect.<br/><br/>&#8226; **RigidBody**: An Output Event Handler script that applies a force or velocity change to a RigidBody on a given output event.<br/><br/>&#8226; **Unity Event**: An Output Event Handler that raises a UnityEvent on a given output event.<br/><br/>&#8226; **[VFX Graph Home](https://unity.com/visual-effect-graph)**: Opens the Visual Effect Graph home page.<br/><br/>&#8226; **[Forum](https://forum.unity.com/forums/visual-effect-graph.428/)**: Opens a Visual Effect Graph sub-forum.<br/><br/>&#8226; **[Github] [Spaceship Demo](https://github.com/Unity-Technologies/SpaceshipDemo)**: Opens a repository of AAA Playable First person demo showcasing effects made with Visual Effect Graph and rendered with the High Definition Render Pipeline.<br/><br/>&#8226; **[Github] [VFX Graph Samples](https://github.com/Unity-Technologies/VisualEffectGraph-Samples)**: Opens a repository that contains sample scenes and visual effects made with Visual Effect Graph.|
| **Version Control** <br/> ![](Images/version-control.png)             | **Action**: When you enable [Version Control](https://docs.unity3d.com/Manual/Versioncontrolintegration.html), these buttons become available. Click the main button to check out the changes you made in the asset file.<br/><br/>**Dropdown**:<br/><br/>&#8226; **Get Latest**: Updates the asset file with latest changes from the repository.<br/>&#8226; **Submit**: Submits the current state of the asset to the Version Control System.<br/>&#8226; **Revert**: Discards the changes you made to the asset.|

<a name="NodeWorkspace"></a>
### Node Workspace

The Node workspace is the area below the toolbar. Here you can navigate and edit the graph. The Node workspace also holds the **Blackboard** and **Target VisualEffect GameObject** panels.

<a name="Blackboard"></a>
### Blackboard

The **Blackboard** is a panel that allows you to manage properties that the Visual Effect Graph uses. It is a floating panel that is independent of the zoom and position of the current Workspace view. The window always displays this panel on top of Nodes in the **Node Workspace**.

To resize this panel, click on any edge or corner and drag. To reposition this panel, click on the header of the panel and drag.

For more information, see [Blackboard](Blackboard.md).

<a name="TargetGameObject"></a>
### VFX Control

The VFX Control panel displays the controls for the GameObject it is currently attached to. It enables you to:

* Control playback options
* Trigger Events
* Use Debug Modes
* Record the bounds of the visual effect. For more information about bounds recording, see [Visual effect bounds](visual-effect-bounds.md).

It is a floating panel that is independent of the zoom and position of the current Workspace view. The window always displays this panel on top of Nodes in the **Node Workspace**.

To resize this panel, click on any edge or corner and drag. To reposition this panel, click on the header of the panel and drag.

## Using the Node Workspace

### Navigating the Workspace

The navigation controls for the Node Workspace are similar to those that other graph-based Unity features use:

#### Move around the graph:

* Middle click and drag.
* Hold the **Alt** key, click and drag.

#### Zoom in and out using :

* To zoom in, scroll the Mouse Wheel up.
* To zoom out, scroll the Mouse Wheel down.

#### Select elements:

* To select elements individually, click on them.
  * To add to/remove an element from the current selection, hold the **Ctrl** key and click on it.
* To create a selection rectangle, click in empty space and drag. This selects every element that the rectangle touches.
  * You can use a selection rectangle to add to/remove elements from the current selection. To do this, hold the **Ctrl** key and use the method described above to create a new selection rectangle.
* To create a selection marquee, hold the **Shift** key, click in empty space, and drag to create a path. This selects every element that the path/marquee touches.
* To clear the current selection, click in empty space.

#### Focus

*  To focus on a specific Node/group of Nodes, select the Node/Nodes and press the **F** key.
*  To focus on the entire graph, clear the current selection and press the **F** key.

#### Copy, Cut and Paste, and Duplicate elements:

* Right click on an element, or group of elements, to open a menu that displays relevant commands.
* Keyboard Shortcuts:
  * **Copy**: Ctrl+C.
  * **Cut**: Ctrl+X.
  * **Paste**: Ctrl+V.
  * **Duplicate**: Ctrl+D.
  * **Duplicate with edges**: Ctrl+Alt+D.

### Adding graph elements

To add graph elements, you can use any of the following methods:

* **Right-click Menu** : Right-click to open the menu, select **Add Node**, then select the **Node** you want to add from the menu. This action is context-sensitive, based on the element that is below your cursor, and only provides you with graph elements that are compatible.
* **Spacebar Menu** : This shortcut is the equivalent of making a right-click, then selecting **Add Node**.
* Interactive Connections : When creating an edge from a port (either property or workflow), drag the edge around and release the click into empty space to display the Node Menu. This action is context-sensitive, based on the source port's type, and only provides you with compatible graph elements that you can connect to.

### Manipulating graph elements

You can manipulate graph elements in the workspace :

#### Moving elements

* To move an element around the workspace, left click on the element's header, drag the element to a new position, and release the mouse button.
* To move [Blocks](Blocks.md) inside a Context, or move them to another Context, click on the Block's header, drag the Block to a new position, and release the mouse button.

#### Resizing elements

Some elements, such as Sticky Notes, support resizing. To do this, click on any edge or corner, drag until you reach the desired element size, and release the mouse button.
