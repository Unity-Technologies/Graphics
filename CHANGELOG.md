# Next version

### Master node settings

![](.data/menu_settings.png)

The settings on master nodes now live in a small window that you can toggle on and off. The settings menu allows you to change various rendering settings for your shader.

### Editable property names

![](.data/editable_property_references.gif)

The property reference names are now editable. It is also now possible to make the properties not exposed.

### Bug fixes and minor changes

- Fixed an issue where vector 1 node was not evaluating properly. ([#334](https://github.com/Unity-Technologies/ShaderGraph/issues/334) and [#337](https://github.com/Unity-Technologies/ShaderGraph/issues/337))
- Properties can now be copied and pasted.
- Pasting a property node into another graph will now convert it to a concrete node. ([#300](https://github.com/Unity-Technologies/ShaderGraph/issues/300))
- Make nodes that are copied from one graph to another spawn in the center of the current view. ([#333](https://github.com/Unity-Technologies/ShaderGraph/issues/333))
