When opening a Visual Effect Asset, the Visual Effect Graph window opens and displays the contents of the asset. This window enables editing the default behaviour of this template. Expose parameters and events, and also attach to instances in scene to debug and preview the effect.

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/graphWindow.PNG)



## The Graph Area

The graph area is where you can add node elements and connect them to define behaviours. This area is the wide panel that takes the most entirety of the window.

#### Navigation:

* You can pan around the graph using the Middle Click button or the Alt+Drag Left Click button. 
* You can Zoom in and out using the Mouse Wheel.\
* You can focus on one specific element using the F Key
* You can frame all elements using the A Key

#### Adding Elements

* You can right-click into an empty space, or inside a Context container and select `Create new Node` to prompt the contextual create node menu.
* You can also use the Spacebar to prompt the contextual create node menu at cursor position.
* When making connections, if you drop a non-connected connection into an empty space, the contextual create node menu will appear. Any node created this manner will also be connected

#### Selection / Dragging

* Clicking an element selects it
* Ctrl+Click an element add/removes it from selection
* Dragging an element around with Left click drags it
* You can select using a selection rectangle by starting in any empty area
* You can freehand select elements by using the Shift key before dragging around. Freehand selection works based on collision between nodes and the freehand line. 

#### Copy, Paste, Duplicate, etc.

* Ctrl+C / Ctrl+V copies and pastes
* Ctrl+Z undoes
* Ctrl+D duplicates
* After starting dragging an element, using the Ctrl Key, then dropping the element will create a copy of it.

#### Group Nodes

* Group nodes can be created by selecting any node/group of nodes and selecting Group Nodes in the contextual Menu
* Group nodes adapt their size to their contents
* You can add elements to a group by dropping the node onto it
* You can remove elements from a group by using the shift key , and dragging the element out of It.

#### Sticky Notes

* You can add sticky notes by right clicking an empty space then choosing Add Sticky Note
* Sticky notes have a title and a body that can be edited by double clicking.
* Sticky notes can be resized
* You can change the theme, and the font size by using the right-click menu

## Toolbar

On top of the graph area is a toolbar where you can access various actions:

* **Refresh** : Refreshes the graph (WIP)
* **Blackboard** : Toggles the Blackboard Panel
* **Component board** : Toggles the Component Board Panel
* **Auto Compile** : Toggles the auto compilation of the asset
* **Compile** : Compiles the asset

## Blackboard Panel

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/blackboard.PNG)

The blackboard panel enables the creation and the setup of the parameter interface. Every parameter can be created here, setup with default values, reordered and categorized using this panel.

* Use the + button to add a category or a new entry of a specific type.
* Use the exposed checkbox to make this parameter visible to to the component
* Delete key deletes a parameter
* You can reorder parameters and move them to Categories by dragging them around.
* To create a parameter operator, simply drag the parameter label to the graph to create it.

## Component Board Panel

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/componentboard.PNG)

The Component board is used to attach the current graph to a component in the scene so the preview can be made on this particular instance.

#### Attachment to Running instance

To attach the component board to a particular instance in scene, just select the effect in the hierarchy, then click the Attach button of the component board. The instance will attach and you will be able to access play controls, event preview and editing widgets.

* Play controls are the same as the component play controls:
  * Stop sends the Stop() event to trigger the shutdown of the effect, while terminating simulation. 
  * Play/Pause toggles the simulation and spawn
  * Frame advance lets you advance only one frame of simulation
  * Reset clears out buffers and sends the Play() event
  * Preview Play Rate can also be adjusted from 1% to 4000%
* Events can be sent to this instance
  * OnPlay and OnStop (corresponding to `Play()` and `Stop()` from the API)
  * Any custom named event can also be sent, and renamed using this panel.

