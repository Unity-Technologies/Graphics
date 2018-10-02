#### `Attribute in CodeFunctionNode`

## Description

Defines an argument to a method as a [Port](Port.md) for a [Node](Node.md). The type of the [Port](Port.md) is defined by the argument type.

The **SlotAttribute** can also be used to apply a [Port Binding](Port-Bording.md) to the [Port](Port.md) or define its default value.

## Properties

| Property    | Type | Description |
|:------------|:-----|:------------|
| slotId | int | Index for the [Port](Port.md). Must be unique. |
| binding | [Binding](CodeFunctionNode.Binding.md) | Defines the [Port Binding](Port-Bording.md). Set to **None** for no binding. |
| hidden | bool | If true the [Port](Port.md) will be hidden. |
| defaultValue | Vector4 | Default value for the [Port](Port.md). |