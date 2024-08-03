# Shader Graph Window

## Description

The **Shader Graph Window** contains the workspace for creating shaders with the **Shader Graph** system. To open the **Shader Graph Window**, you must first create a [Shader Graph Asset](index.md). For more information, refer to the [Getting Started](Getting-Started.md) section.

The **Shader Graph** window contains various individual elements such as the [Blackboard](Blackboard.md), [Graph Inspector](Internal-Inspector.md), and [Main Preview](Main-Preview.md). You can move these elements around inside the workspace. They automatically anchor to the nearest corner when scaling the **Shader Graph Window**.

## Toolbar

The toolbar at the top of the **Shader Graph Window** contains the following commands.

| Icon                | Item                | Description |
|:--------------------|:--------------------|:------------|
| ![Image](images/sg-save-icon.png) | **Save Asset**      | Save the graph to update the [Shader Graph Asset](index.md). |
| ![Image](images/sg-dropdown-icon.png) | **Save As**         | Save the [Shader Graph Asset](index.md) under a new name. |
| | **Show In Project** | Highlight the [Shader Graph Asset](index.md) in the [Project Window](https://docs.unity3d.com/Manual/ProjectView.html). |
| | **Check Out**       | If version control is enabled, check out the [Shader Graph Asset](index.md) from the source control provider. |
| ![Image](images/sg-color-mode-selector.png) | **Color Mode Selector**      | Select a [Color Mode](Color-Modes.md) for the graph. |
| ![Image](images/sg-blackboard-icon.png) | **Blackboard**      | Toggle the visibility of the [Blackboard](Blackboard.md). |
| ![Image](images/sg-graph-inspector-icon.png) | **Graph Inspector** | Toggle the visibility of the [Graph Inspector](Internal-Inspector.md). |
| ![Image](images/sg-main-preview-icon.png) | **Main Preview**    | Toggle the visibility of the [Main Preview](Main-Preview.md). |
| ![Image](images/sg-help_icon.png) | **Help**     | Open the Shader Graph documentation in the browser. |
| ![Image](images/sg-dropdown-icon.png) | **Resources** | Contains links to Shader Graph resources (like samples and User forums). |

## Workspace

The workspace is where you create [Node](Node.md) networks.
To navigate the workspace, do the following: 
- Press and hold the Alt key and drag with the left mouse button to pan. 
- Use the mouse scroll wheel to zoom in and out.

You can hold the left mouse button and drag to select multiple [Nodes](Node.md) with a marquee. There are also various [shortcut keys](Keyboard-shortcuts.md) you can use for better workflow.


## Context Menu

Right-click within the workspace to open a context menu. However, if you right-click on an item within the workspace, such as a [Node](Node.md), the context menu for that item opens. The workspace context menu provides the following options.

| Item                         | Description |
|:-----------------------------|:------------|
| **Create Node**              | Opens the [Create Node Menu](Create-Node-Menu.md). |
| **Create Sticky Note**       | Creates a new [Sticky Note](Sticky-Notes.md) on the Graph. |
| **Collapse All Previews**    | Collapses previews on all [Nodes](Node.md). |
| **Cut**                      | Removes the selected [Nodes](Node.md) from the graph and places them in the clipboard. |
| **Copy**                     | Copies the selected [Nodes](Node.md) to the clipboard. |
| **Paste**                    | Pastes the [Nodes](Node.md) from the clipboard. |
| **Delete**                   | Deletes the selected [Nodes](Node.md). |
| **Duplicate**                | Duplicates the selected [Nodes](Node.md). |
| **Select / Unused Nodes**    | Selects all nodes on the graph that are not contributing to the final shader output from the [Master Stack](Master-Stack.md). |
| **View / Collapse Ports**    | Collapses unused ports on all selected [Nodes](Node.md). |
| **View / Expand Ports**      | Expands unused ports on all selected [Nodes](Node.md). |
| **View / Collapse Previews** | Collapses previews on all selected [Nodes](Node.md). |
| **View / Expand Previews**   | Expands previews on all selected [Nodes](Node.md). |
| **Precision / Inherit**      | Sets the precision of all selected Nodes to Inherit. |
| **Precision / Float**        | Sets the precision of all selected nodes to Float. |
| **Precision / Half**         | Sets the precision of all selected nodes to Half. |

## Additional resources

- [Color Modes](Color-Modes.md)
- [Create Node Menu](Create-Node-Menu.md)
- [Keyboard shortcuts](Keyboard-shortcuts.md)
- [Master Stack](Master-Stack.md) 
- [Nodes](Node.md)
- [Sticky Notes](Sticky-Notes.md)