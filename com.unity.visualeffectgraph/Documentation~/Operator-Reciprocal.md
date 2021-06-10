# Reciprocal (1/x)

Menu Path : **Operator > Math > Arithmetic > Reciprocal (1/x)**

The **Reciprocal** Operator calculates the result of dividing 1 by the input value. For example, an input of (0.2, 2, -5) outputs (5, 0.5, -0.2).

This Operator accepts a number of input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The **Reciprocal** Operator will always return a value in the same type as its input.

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The value of the input subtracted from one.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **X** port. For the list of types this property supports, see [Available types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
