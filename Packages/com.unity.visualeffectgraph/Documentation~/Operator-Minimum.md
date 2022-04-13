# Minimum

Menu Path : **Operator > Math > Clamp > Minimum**

The **Minimum** Operator outputs the smallest value from all input values.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). This Operator calculates the minimum value by value for Vector types. For scalar values like float, int, or uint, this Operator calculates the minimum between the scalar value and all the vector values.

## Operator properties

| **Input** | **Type**                                | **Description**                                              |
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **A**     | [Configurable](#operator-configuration) | A value this Operator evaluates. If this value is the smallest of all the inputs, **Out** becomes this value. |
| **B**     | [Configurable](#operator-configuration) | A value this Operator evaluates. If this value is the smallest of all the inputs, **Out** becomes this value. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The minimum value of all inputs.<br/>The **Type** changes to match the input type. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. You will see a list of inputs that you can reorder, rename, change their type, or delete. The minimum number of inputs is two, there is no maximum. Use the “+” button to add elements. If there are three or more inputs, you can use the “-” button to delete the selected input. Use the handle at the left of the input to drag and reorder it.

This Operator also has an empty input that you can link to add a new input.

### Available types

You can use the following types for your **input** ports:

- **Float**
- **Int**
- **uint**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
