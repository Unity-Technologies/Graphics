# Absolute

Menu Path : **Operator > Math > Arithmetic > Absolute**  

The **Absolute** Operator calculates the absolute value of the input. For example, an input value of (4 ,0, -4) outputs (4, 0, 4).

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). 

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The absolute value of the input.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **X**        | The value type for the **X** port. For the list of types this property supports, see [Available types](#available-types). |



### Available types

You can use the following types for your **input** ports:

- **float**
- **int**
- **uint**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**