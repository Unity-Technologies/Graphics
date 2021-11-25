# Sign

Menu Path : **Operator > Math > Arithmetic > Sign**

The **Sign** Operator calculates the sign of the input and returns whether the input is positive, negative, or 0:

- If **X** is greater than 0, the result is 1.
- If **X** is less than 0, the result is -1.
- If **X** is 0, the result is 0.

For example, an input of (0.3, 0.4, -5) outputs (1, 1, -1).

This Operator accepts a number of input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). This Operator always returns a value in the same type as its input.

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The value of the input subtracted from one.  The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **X** port. For the list of types this property supports, see [Available Types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **Int**
- **uint**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
