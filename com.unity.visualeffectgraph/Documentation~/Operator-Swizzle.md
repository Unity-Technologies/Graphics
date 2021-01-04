# Swizzle

Menu Path : **Operator > Math > Vector**

The **Swizzle** [uniform Operator](Operators.md#uniform-operators) rearranges the input vector’s components and outputs them into an output vector, based on a swizzle **Mask** string.

For example, a vector of (x,y,z,w) can swizzle to (y,x,x,z).

## Operator settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Mask**    | string   | The swizzle mask. This is a combination of one to four characters that can be x, y, z, or w. |

## Operator properties

| **Input** | **Type**                                | **Description**                      |
| --------- | --------------------------------------- | ------------------------------------ |
| **X**     | [Configurable](#operator-configuration) | The vector to swizzle the values of. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent | The swizzled output vector (dependant on the Mask setting).<br/>The **Type** changes to match the number of characters in the **Mask**. |

## Operator configuration

To view this [uniform Operator's](Operators.md#uniform-operators) configuration, click the **cog** icon in the Node's header. Here you can configure the data type this Operator uses.

### Available types

You can use the following types for your **Input values** and ports:

- **Position**
- **Vector**
- **Direction**
- **Vector4**
- **Vector3**
- **Vector2**
- **Float**