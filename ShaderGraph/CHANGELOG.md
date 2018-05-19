# Next version

### Master node settings

![](.data/menu_settings.png)

The settings on master nodes now live in a small window that you can toggle on and off. The settings menu allows you to change various rendering settings for your shader.

### Property reference names and exposed state

![](.data/editable_property_references.gif)

The property reference names are now editable. Furthermore, it is now possible to control whether the properties are exposed.

### Editable paths for graphs

![](.data/change_path.gif)
![](.data/use_path.gif)

Enables the user to change the path of Shader Graphs and Sub Graphs. Changing the path of a Shader Graph modifies the location it has in the shader selection list. Likewise changing the path of Sub Graph modifies its location in the node creation menu.


### Gradient nodes

![](.data/gradient_node.png)

This adds gradient functionality via two new nodes. Sample Gradient node samples a gradient given a Time parameter. This gradient can be defined on the Gradient slot control view. The Gradient Asset node defines a gradient that can be sampled by multiple Sample Gradient nodes using different Time parameters.




### Bug fixes and minor changes

- Fixed an issue where vector 1 node was not evaluating properly. ([#334](https://github.com/Unity-Technologies/ShaderGraph/issues/334) and [#337](https://github.com/Unity-Technologies/ShaderGraph/issues/337))
- Properties can now be copied and pasted.
- Pasting a property node into another graph will now convert it to a concrete node. ([#300](https://github.com/Unity-Technologies/ShaderGraph/issues/300) and [#307](https://github.com/Unity-Technologies/ShaderGraph/pull/307))
- Make nodes that are copied from one graph to another spawn in the center of the current view. ([#333](https://github.com/Unity-Technologies/ShaderGraph/issues/333))
- Fixed an issue with editable sub graph paths, causing the search window to sometimes yield a null reference exception.
- Ensure that the blackboard is within view when deserialized.
