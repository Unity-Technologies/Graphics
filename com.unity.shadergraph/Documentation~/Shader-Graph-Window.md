# The Shader Graph window

The Shader Graph window contains your workspace for creating shaders using Shader Graph. It opens when you [open a Shader Graph Asset](Open-Graph-Edit.md) from your project.

![](images/)
<!-- Add an image that shows the Graph window -->

The Shader Graph window contains the following windows:

- The [Blackboard](Blackboard.md)
- The [Graph-Inspector](Graph-Inspector.md)
- The [Main Preview](Main-Preview.md)

The Shader Graph window also contains the [Graph toolbar](#the-graph-toolbar) and the [Graph Editor](#the-graph-editor).

## The Graph toolbar

The Graph toolbar lets you display or hide the Blackboard, Graph Inspector, and Main Preview windows. It also contains some additional options and tools for working with a Shader Graph Asset.

![](images/)
<!-- Add an image that shows the Graph toolbar -->

<table>
<thead>
<tr>
<th><strong>Toolbar Item</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Save Asset</strong></td>
<td>Save the Shader Graph Asset that's currently open in the Shader Graph window to its existing location in the project. For more information on saving a Shader Graph, see <a href="Save-Graph-Asset.md">Save your Shader Graph Asset</a>.</td>
</tr>
<tr>
<td><strong>Save As</strong></td>
<td>Open a system file dialog to change the file name or save location of the Shader Graph Asset that's currently open in the Shader Graph window. For more information on saving a Shader Graph, see <a href="Save-Graph-Asset.md">Save your Shader Graph Asset</a>.</td>
</tr>
<tr>
<td><strong>Show In Project</strong></td>
<td>Opens the <a href="https://docs.unity3d.com/Documentation/Manual/ProjectView.html">Project window</a> and highlights the Shader Graph Asset that's currently open in the Shader Graph window.</td>
</tr>
<tr>
<td><strong>Check Out</strong></td>
<td>If you're using version control with your project, select <strong>Check Out</strong> to check out the Shader Graph Asset that's currently open in the Shader Graph window. For more information on using version control in the Unity Editor, see <a href="https://docs.unity3d.com/Documentation/Manual/VersionControl.html">the Version Control section in the Unity User manual</a>.</td>
</tr>
<tr>
<td><strong>Color Mode</strong></td>
<td>Select a Color Mode to change how Shader Graph nodes display in your graph:
<ul>
<li><strong>None</strong>: Shader Graph doesn't add colors to your nodes.</li>
<li><strong>Category</strong>: Shader Graph adds colors to your nodes based on their category in the <a href="Create-Node-Menu.md">Create Node menu</a>.</li>
<li><strong>Precision</strong>: Shader Graph adds colors to your nodes based on the <a href="Precision-Modes.md">Precision Mode</a> set on your graph or on each individual node.</li>
<li><strong>User Defined</strong>: Shader Graph adds colors to your nodes based on the color you assign to each node.</li>
</ul>
For more information about Color Modes in Shader Graph, see <a href="Color-Modes.md">Color Modes</a>.
</td>
</tr>
<tr>
<td><strong>Blackboard</strong></td>
<td>Select to display or hide the <a href="Blackboard.md">Blackboard</a>.</td>
</tr>
<tr>
<td><strong>Graph Inspector</strong></td>
<td>Select to display or hide the <a href="Graph-Inspector.md">Graph Inspector</a>.</td>
</tr>
<tr>
<td><strong>Main Preview</strong></td>
<td>Select to display or hide the <a href="Main-Preview.md">Main Preview</a>.</td>
</tr>
</tbody>
</table>

## The Graph Editor

The Graph Editor is the center editing area of the Shader Graph window. Using the Graph Editor, you can connect nodes and create your Shader Graph.

![](images/)
<!-- Add an image that shows the Graph Editor -->

You can use the following shortcuts to navigate and change your view in the Graph Editor:

<table>
<thead>
<tr>
<th><strong>Action</strong></th>
<th><strong>Shortcut</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Pan</strong></td>
<td> Alt + click and drag <br/> <br/>Middle-click and drag</td>
<td>Hold the Alt key while clicking and dragging, or click and drag with the scroll wheel to move your viewable area inside the Graph Editor.</td>
</tr>
<tr>
<td><strong>Zoom</strong></td>
<td>Scroll wheel</td>
<td>Use the scroll wheel to zoom in and out on the viewable area inside the Graph Editor.</td>
</tr>
<tr>
<td><strong>Focus</strong></td>
<td>F</td>
<td>Press F to center the viewable area inside the Graph Editor on your current selection.</td>
</tr>
</tbody>
</table>

You can find additional actions in the context menu. You can open the context menu by right-clicking on an empty space in the Graph Editor.

![](images/)
<!-- Add an image that shows the Context Menu -->

> [!NOTE]
> If you right-click on a node or another item in your graph, or right-click with a node selected, the context menu will include different options from the ones in the following table.

<table>
<thead>
<tr>
<th><strong>Menu Item</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Create Node</strong></td>
<td>Select to open the <a href="Create-Node-Menu.md">Create Node Menu</a>. For more information on creating nodes, see <a href="Create-New-Node.md">Create a new Shader Graph node</a>. <br/> You can also open the Create Node menu by pressing Spacebar.</td>
</tr>
<tr>
<td><strong>Create Sticky Note</strong></td>
<td>Select to create a new <a href="Sticky-Notes.md">Sticky Note</a> in your graph at your current cursor location.</td>
</tr>
<tr>
<td><strong>Collapse All Previews</strong></td>
<td>Some nodes display previews of their results in the Shader Graph window. Select <strong>Collapse All Previews</strong> to hide all previews on any nodes in your graph.</td>
</tr>
<tr>
<td><strong>Cut</strong></td>
<td>Select to move any nodes in your current selection from your graph to your clipboard. <br/> You can also cut your current selection by pressing Ctrl + X (macOS: Cmd + X).</td>
</tr>
<tr>
<td><strong>Copy</strong></td>
<td>Select to copy any nodes from your current selection to your clipboard. <br/> You can also copy your current selection by pressing Ctrl + C (macOS: Cmd + C).</td>
</tr>
<tr>
<td><strong>Paste</strong></td>
<td>Select to paste any nodes from your clipboard to your current cursor location. <br/> You can also paste your current selection by pressing Ctrl + V (macOS: Cmd + V).</td>
</tr>
<tr>
<td><strong>Delete</strong></td>
<td>Select to delete any items in your current selection from your graph. <br/> You can also delete your current selection by pressing Delete/DEL.</td>
</tr>
<tr>
<td><strong>Duplicate</strong></td>
<td>Select to duplicate any nodes in your current selection and place the new copies in your graph. <br/> You can also duplicate your current selection by pressing Ctrl + D (macOS: Cmd + D).</td>
</tr>
<tr>
<td><strong>Select</strong> &gt; <strong>Unused Nodes</strong></td>
<td>Select to select all nodes in your current Shader Graph that aren't connected to your graph's Master Stack and don't contribute to your final shader output.</td>
</tr>
<tr>
<td><strong>View</strong> &gt; <strong>Collapse Ports</strong></td>
<td>Select to hide all ports that don't have a valid connection, or <a href="Edge.md">Edge</a>, on all nodes in your current selection.</td>
</tr>
<tr>
<td><strong>View</strong> &gt; <strong>Expand Ports</strong></td>
<td>If ports have been collapsed, select to display all ports on all nodes in your current selection.</td>
</tr>
<tr>
<td><strong>View</strong> &gt; <strong>Collapse Previews</strong></td>
<td>Some nodes display previews of their results in the Shader Graph window. Select <strong>Collapse Previews</strong> to hide all previews on all nodes in your current selection.</td>
</tr>
<tr>
<td><strong>View</strong> &gt; <strong>Expand Previews</strong></td>
<td>Some nodes display previews of their results in the Shader Graph window. If previews have been collapsed, select <strong>Expand Previews</strong> to display all previews on all nodes in your current selection.</td>
</tr>
<tr>
<td><strong>Precision</strong> &gt; <strong>Inherit</strong></td>
<td>Select to set the Precision Mode for all nodes in your current selection to <strong>Inherit</strong>. For more information on Precision Modes, see <a href="Precision-Modes.md">Precision Modes</a>.</td>
</tr>
<tr>
<td><strong>Precision</strong> &gt; <strong>Single</strong></td>
<td>Select to set the Precision Mode for all nodes in your current selection to <strong>Single</strong>. For more information on Precision Modes, see <a href="Precision-Modes.md">Precision Modes</a>.</td>
</tr>
<tr>
<td><strong>Precision</strong> &gt; <strong>Half</strong></td>
<td>Select to set the Precision Mode for all nodes in your current selection to <strong>Half</strong>. For more information on Precision Modes, see <a href="Precision-Modes.md">Precision Modes</a>.</td>
</tr>
</tbody>
</table>
