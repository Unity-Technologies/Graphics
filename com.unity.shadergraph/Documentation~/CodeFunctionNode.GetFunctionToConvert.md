#### `protected abstract MethodInfo GetFunctionToConvert()`

## Returns

A **MethodInfo** of a class to convert to a shader function.

## Description

Defines which method within the class should be converted to a shader function. Use **Type.GetMethodInfo** to convert a method of return type **string** to a **MethodInfo** to return. The referenced class should define [Ports](Port.md) via [SlotAttribute](CodeFunctionNode.SlotAttribute.md).

For more information on how to create [Nodes](Node.md) using `CodeFunctionNode` see [Custom Nodes with CodeFunctionNode](Custom-Nodes-With-CodeFunctionNode.md).