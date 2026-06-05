# Create a custom node by reflection from HLSL

Create custom nodes by reflection from HLSL files and automatically make them available in the Shader Graph node library.

## Reflect an HLSL function into a Shader Graph node

Follow these steps:

1. Create an empty HLSL file anywhere in your Unity project: from the Editor's menu, select **Assets** > **Create** > **Shader** > **Empty HLSL**.

1. Write your HLSL function.

1. Add the following include at the top of the file: `#include "ShaderApiReflectionSupport.hlsl"`.

1. Precede the function declaration with `UNITY_EXPORT_REFLECTION`.

1. Save the HLSL file.

### Example

```
#include "ShaderApiReflectionSupport.hlsl"

UNITY_EXPORT_REFLECTION
float3 MyCustomFunction(
    float3 param1,
    float param2)
{
    return param1 * param2;
}
```

## List the reflected node in Shader Graph's Create Node menu.

To make a reflected node available in Shader Graph's [**Create Node** menu](Create-Node-Menu.md), you minimally have to assign it a provider key.

To specify a provider key value, in the HLSL include file, precede the function with the following hint:

```
///<funchints>
///     <sg:ProviderKey>MyValue</sg:ProviderKey>
///</funchints>
```

By default, as soon as you specify a provider key, Unity places the reflected node in the **Reflected by Path** category and uses its corresponding function name to identify it. However, you can use [additional reflection hints](Custom-nodes-hlsl-reflection-hints-reference.md) to the HLSL include file to specify custom category and node display names, and more. For more details, refer to the next section.

## Further customize the reflected node

To further customize the behavior and appearance of the reflected function node and its parameters in Shader Graph, you have to precede the function with a list of [reflection hints](Custom-nodes-hlsl-reflection-hints-reference.md) in the HLSL include file.

There are two types of hints:
* Function hints for the function node as a whole.
* Parameter hints for the node parameters and ports. 

### Add function hints

Add function hints (`funchints`) to further customize the node as a whole.

For example, to specify a provider key, a display name, and a **Create Node** menu search category for the node, use the following :
  
```
///<funchints>
///     <sg:ProviderKey>MyCustomUniqueID</sg:ProviderKey>
///     <sg:DisplayName>My Very Custom Function</sg:DisplayName>
///     <sg:SearchCategory>Custom/MyFunction</sg:SearchCategory>
///</funchints>
```

Make sure to meet the following criteria:
* Use a `<funchints></funchints>` xml tag pair to wrap the list.
* Start all lines with a triple-slash (`///`).

### Add parameter hints

Add parameter hints (`paramhints`) to further customize the node parameters and ports.

For example, to specify input port display names, types, and default values, use the following:
  
  ```
  ///<paramhints name = "param1">
  ///    <sg:DisplayName>Color</sg:DisplayName>
  ///    <sg:Color/>
  ///    <sg:Default>1,1,0</sg:Default>
  ///</paramhints>
  ///<paramhints name = "param2">
  ///     <sg:DisplayName>Range</sg:DisplayName>
  ///     <sg:Range>0, 1</sg:Range>
  ///     <sg:Default>0.25</sg:Default>
  ///</paramhints>
  ```

Make sure to meet the following criteria:
* Use a `<paramhints name = "paramname"></paramhints>` xml tag pair to wrap the list for each parameter.
* For `paramname`, use the name of the parameter as you specified it in the HLSL function.
* Start all lines with a triple-slash (`///`).

## Complete example

The following code snippet groups all the example cases provided in the previous sections. You can use it as is in your project to test the feature.

```
#include "ShaderApiReflectionSupport.hlsl"

///<funchints>
///     <sg:ProviderKey>MyCustomUniqueID</sg:ProviderKey>
///     <sg:DisplayName>My Very Custom Function</sg:DisplayName>
///     <sg:SearchCategory>Custom/MyFunction</sg:SearchCategory>
///</funchints>
///<paramhints name = "param1">
///    <sg:DisplayName>Color</sg:DisplayName>
///    <sg:Color/>
///    <sg:Default>1,1,0</sg:Default>
///</paramhints>
///<paramhints name = "param2">
///     <sg:DisplayName>Range</sg:DisplayName>
///     <sg:Range>0, 1</sg:Range>
///     <sg:Default>0.25</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
float3 MyCustomFunction(
    float3 param1,
    float param2)
{
    return param1 * param2;
}
```

## Additional resources

- [Introduction to HLSL in Shader Graph](Custom-nodes-hlsl-introduction.md)
- [Node Reference sample](Shader-Graph-Sample-Node-Reference.md): in the `Utility` folder, refer to the `ReflectedFunction` graph.
- [Reflected function hints reference](Custom-nodes-hlsl-reflection-hints-reference.md)
