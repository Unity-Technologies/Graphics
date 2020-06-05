# Power

Menu Path : **Operator > Math > Arithmetic > Power** 

The **Power** Operator raises the first input to the power of the second input, then continues to raise the result to subsequent inputs. For example, an input value of (3, 2) (3, 4, 5) outputs (27,16,1).

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). This Operator always returns a value in the largest vector type of its input. 

This Operator interprets missing values as zero. It interprets scalar values such as float, int, and unit as being as large as the largest vector input with the scalar value taking up every member of the vector.

This Operator also has an empty input that you can link to add a new input.

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **Input** | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The first input raised to the power of subsequent inputs.<br/>The **Type** changes to match the largest vector type of the Operator's inputs. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. You will see a list of inputs that you can reorder, rename, change their type, or delete. The minimum number of inputs is two, there is no maximum. Use the “+” button to add elements. If there are three or more inputs you can use the “-” button to delete the selected input. Use the handle at the left of the input to drag and reorder it.

The **Power** Operator also has an empty input that you can link to add a new input.



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**