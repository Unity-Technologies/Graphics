# Shader Graph Window

## Description

The **Shader Graph Window** contains the workspace for creating shaders using the **Shader Graph** system. To open the **Shader Graph Window** you must first create a [Shader Graph Asset](index.md). For more information see the [Getting Started](Getting-Started.md) section.

The **Shader Graph** window contains various individual elements such as the [Blackboard](Blackboard.md), [Graph Inspector](Internal-Inspector.md), and [Main Preview](Master-Preview.md). These elements can be moved inside the workspace. They will automatically anchor to the nearest corner when scaling the **Shader Graph Window**.

## Title Bar

The title bar at the top of the **Shader Graph Window** contains actions that can be performed on the **Graph**.

| Item        | Description |
|:------------|:------------|
| Save Asset        | Saves the graph to update the [Shader Graph Asset](index.md) |
| Save As           | Opens a file dialog that allows the user to save out the [Shader Graph Asset](index.md) under a new name. |
| Show In Project   | Highlights the [Shader Graph Asset](index.md) in the [Project Window](https://docs.unity3d.com/Manual/ProjectView.html) |
| Check Out         | If version control is enabled, this will check out the [Shader Graph Asset](index.md) from the source control provider. |
| Color Mode        | Provides the drop down menu to select a [Color Mode](Color-Modes.md) for the graph. |
| Blackboard        | Toggles visibility of the [Blackboard](Blackboard.md). |
| Graph Inspector   | Toggles visibility of the [Graph Inspector](Internal-Inspector.md). |
| Master Preview    | Toggles visbility of the [Master Preview](Master-Preview.md). |

## Workspace

The workspace is where you create [Node](Node.md) networks. 
You can navigate the workspace by holding Alt and left mouse button to pan and zoom with the scroll wheel.

You can hold left mouse button and drag to select multiple [Nodes](Node.md) with a marquee. There are also various shortcut keys to use for better workflow.

| Hotkey      | Windows     | OSX         | Description |
|:------------|:------------|:------------|:------------|
| Cut | Ctrl + X | Command + X | Cuts selected [Nodes](Node.md) to the clipboard
| Copy | Ctrl + C | Command + C | Copies selected [Nodes](Node.md) to the clipboard
| Paste | Ctrl + V | Command + V | Pastes [Nodes](Node.md) in the clipboard
| Focus | F | F | Focus the workspace on all or selected [Nodes](Node.md)
| Create Node | Spacebar | Spacebar | Opens the [Create Node Menu](Create-Node-Menu.md)

## Context Menu

Right clicking within the workspace will open a context menu. Note that right clicking on an item within the workspace, such as a [Node](Node.md), will open the context menu for that item and not the workspace.

| Item        | Description |
|:------------|:------------|
| Create Node | Opens the [Create Node Menu](Create-Node-Menu.md) |
| Create Sticky Note | Creates a new [Sticky Note](Sticky-Notes.md) on the Graph. |
| Collapse All Previews | Collapses previews on all [Nodes](Node.md) |
| Cut | Cuts selected [Nodes](Node.md) to the clipboard |
| Copy | Copies selected [Nodes](Node.md) to the clipboard |
| Paste | Pastes [Nodes](Node.md) in the clipboard |
| Delete | Deletes selected [Nodes](Node.md) |
| Duplicate | Duplicates selected [Nodes](Node.md) |
| Select / Unused Nodes | Selects all nodes on the graph that are not contributing to the final shader output from the Master Stack. |
| View / Collapse Ports | Collapses unused ports on all selected [Nodes](Node.md) |
| Vierw / Expand Ports | Expands unused ports on all selected [Nodes](Node.md) |
| View / Collapse Previews | Collapses previews on all selected [Nodes](Node.md) |
| View / Expand Previews | Expands previews on all selected [Nodes](Node.md) |
| Precision / Inherit | Sets precision of all selected Nodes to Inherit. |
| Precision / Float | Sets precision on all selected nodes to Float. |
| Precision / Half | Sets precision on all selected nodes to Half. |
