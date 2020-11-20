# Atan2

Menu Path : **Operator > Math > Trigonometry > Atan2**  

The **Atan2** Operator calculates the value of the angle between the x-axis and a vector that starts at zero and terminates at (**X**, **Y**). The result is in radians. 

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types).

## Operator properties

| **Property** | **Type**                                | **Description**                                              |
| ------------ | --------------------------------------- | ------------------------------------------------------------ |
| **X**        | [Configurable](#operator-configuration) | The value this Operator evaluates.                           |
| **Y**        | Dependent                               | An input of the same type as X.<br/>The **Type** changes to match the type of **X**. |
| **Out**      | Dependent                               | The signed angle between the vector defined by **X** and **Y** and the positive x-axis of the input.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **X** and **Y** port. For the list of types these properties support, see [Available types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**