# Subtract

Menu Path : **Operator > Math > Arithmetic > Subtract**

The **Subtract** Operator takes the first input and subtracts all subsequent inputs from it. For example, an input of (1, 2) - (3, 4, 5) outputs (-2, -2, -5), and an input of (1, 2) - 3 outputs (-2, -1).

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The **Subtract** Operator interprets any missing input as **0** and always returns a value in the largest vector type of its input. It sees scalar values such as float, int, and uint as the largest vector and uses the value in all the vector's fields.

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **Input** | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The subtraction of the first input by all the other inputs. <br/>The **Type** changes to match the largest vector type of the Operator's inputs. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. You will see a list of inputs that you can reorder, rename, change their type, or delete. The minimum number of inputs is two, there is no maximum. Use the “+” button to add elements. If there are three or more inputs you can use the “-” button to delete the selected input. Use the handle at the left of the input to drag and reorder it.

The **Subtract** Operator also has an empty input that you can link to add a new input.



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
