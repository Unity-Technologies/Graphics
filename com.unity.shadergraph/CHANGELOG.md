# Next version

### Master node settings

![](.data/menu_settings.png)

The settings for master nodes now live in a small window that you can toggle on and off. Here, you can change various rendering settings for your shader.

### Property reference names and exposed state

![](.data/editable_property_references.gif)

You can now edit the Reference name for a property. To do so, select the property and type a new name next to Reference. If you want to reset to the default name, right-click Reference, and select Reset reference. 

In the expanded property window, you can now also toggle if the property is exposed.

### Editable paths for graphs

![](.data/change_path.gif)
![](.data/use_path.gif)

You can now change the path of Shader Graphs and Sub Graphs. When you change the path of a Shader Graph, this modifies the location it has in the shader selection list. When you change the path of Sub Graph, it will have a different location in the node creation menu.


### Gradient nodes

![](.data/gradient_node.png)

This adds gradient functionality via two new nodes. The Sample Gradient node samples a gradient given a Time parameter. You can define this gradient on the Gradient slot control view. The Gradient Asset node defines a gradient that can be sampled by multiple Sample Gradient nodes using different Time parameters.


### Show generated code

![](.data/show_generated_code.gif)

You can now see the generated code for any specific node. To do so, right-click the node, and select Show Generated Code. The code snippet will now open in the code editor that you have linked to Unity.


### Bug fixes and minor changes

- Vector 1 nodes now evaluate correctly. ([#334](https://github.com/Unity-Technologies/ShaderGraph/issues/334) and [#337](https://github.com/Unity-Technologies/ShaderGraph/issues/337))
- Properties can now be copied and pasted.
- Pasting a property node into another graph will now convert it to a concrete node. ([#300](https://github.com/Unity-Technologies/ShaderGraph/issues/300) and [#307](https://github.com/Unity-Technologies/ShaderGraph/pull/307))
- Nodes that are copied from one graph to another now spawn in the center of the current view. ([#333](https://github.com/Unity-Technologies/ShaderGraph/issues/333))
- When you edit sub graph paths, the search window no longer yields a null reference exception.
- The blackboard is now within view when deserialized.
- Your system locale can no longer cause incorrect commands due to full stops being converted to commas.
- Deserialization of subgraphs now works correctly.
- Sub graphs are now suffixed with (sub), so you can tell them apart from other nodes.
