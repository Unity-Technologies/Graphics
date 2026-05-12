# Introduction to HLSL in Shader Graph

Create nodes based on your own custom HLSL functions to extend the Shader Graph functionality.

If you're familiar with [High Level Shader Language (HLSL)](https://docs.unity3d.com/Manual/shaders-reference.html), you can code your own custom shader functions to include them as nodes in your graphs.

There are two different methods to include your own HLSL code in a shader graph:
* Add a Custom Function node from the Shader Graph node library to make it execute HLSL code.
* Use reflection from an HLSL include file to automatically generate a custom node and make it available in the Shader Graph node library.

## Code execution through a Custom Function node 

This method uses a [Custom Function node](Custom-Function-Node.md) from the Shader Graph node library as a starting point.

When you configure a Custom Function node, you can either write an HLSL function directly in the node settings or specify a path to a function you saved in an HLSL include file of your project. 
In both cases, you have to manually add input and output ports to the node and make sure their names and types match the parameter names and types you use in the HLSL function.

For more details, refer to [Custom function node](Custom-Function-Node.md).

## Node creation by reflection from an HLSL include file

This method uses an HLSL include file of your project as a starting point.

When you prepare the HLSL include file, you have to add specific lines of code to make sure the HLSL function reflects as a node in the Shader Graph node library. Unity automatically creates input and output ports in the node by reflection from the parameters and return type specified in the HLSL function signature.

With this method, you can also further customize the behavior and appearance of the node and of its ports and parameters in Shader Graph. For example, you can set default values and force a parameter to be constant.

For more details, refer to [Create a custom node by reflection from HLSL](Custom-nodes-hlsl-create-node-by-reflection.md).

## Additional resources

- [Node Reference sample](Shader-Graph-Sample-Node-Reference.md): in the `Utility` folder, refer to the `CustomFunction` and `ReflectedFunction` graphs.
