# Graph Tools Foundation

Graph Tools Foundation is a framework to build graph editing tools
including a graph data model, a UI foundation and graph-to-asset pipeline.

Graph Tools Foundation provides a set of API to help you develop
graph editing tools. As such, it has no functionality immediately
available to the user; however, if you write a tool that deals with
graphs, Graph Tools Foundation can help you reach your goals more
quickly while adhering to a set of Unity UI and UX standards.

## What this package provides

Graph Tools Foundation is a framework that you can use to build graph
editing tools. It is meant to be configurable and extensible. It
provides:

- A set of modular user interface elements, such as nodes and edges.
- A set of interfaces that should be implemented by your graph model.
  Alternatively, you can choose to derive your graph model from a
  basic graph model included in the package.
- An extensible and adaptable action-response system that defines how
  the UI interacts with the graph model. Graph Tools Foundation defines
  a set of basic graph operations for node and edge creation, cut, copy,
  and paste of elements, node manipulation, etc. All operations support
  undo and redo.

### Some features

- Pan and zoom of the work area
- Node snapping (to grid, to other nodes)
- Placemats to group nodes together
- Horizontal and vertical flow
- Portals
- Editable edge paths
- Edge ordering
- Minimap showing an reduced view of the entire graph
- A blackboard, an working area where you can define things for use
  in the graph
- A node editor, to edit properties of the nodes
