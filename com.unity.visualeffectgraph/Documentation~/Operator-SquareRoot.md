# Square Root

Menu Path : **Operator > Math > Arithmetic > Square Root**

The **Square Root** Operator calculates the square root of each input value. For example, an input of (0.25,1,9) outputs (0.5,1,3).  The square root of a negative number is undefined.

This Operator accepts a number of input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The **Square Root** Operator always returns a value in the same type as its input.

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The square root of the input.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **X** port. For the list of types this property supports, see [Available Types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
