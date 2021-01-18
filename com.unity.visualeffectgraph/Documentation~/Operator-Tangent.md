# Tangent

Menu Path : **Operator > Math > Trigonometry > Tangent**  

The **Tangent** Operator calculates the [tangent](https://docs.unity3d.com/ScriptReference/Mathf.Tan.html) of the input in radians.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types).

## Operator properties

| **Input** | **Type**                                | **Description**                                              |
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **X**     | [Configurable](#operator-configuration) | The value, in radians, this Operator calculates the tangent of. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The tangent of the input.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **X** port. For the list of types this property supports, see [Available types](#available-types).



### Available types

You can use the following types for your input ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**