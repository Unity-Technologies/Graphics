# Maximum

Menu Path : **Operator > Math > Clamp > Maximum**

The **Maximum** Operator outputs the largest value from all input values.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). This Operator calculates the maximum value by value for Vector types. For scalar values like float, int, or uint, this Operator calculates the maximum between the scalar value and all the vector values.

## Output properties

| **Input** | **Type**                                | **Description**                                              |
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **A**     | [Configurable](#operator-configuration) | The value this Operator evaluates. If this value is the largest of all the inputs, **Out** becomes this value. |
| **B**     | [Configurable](#operator-configuration) | The value this Operator evaluates. If this value is the largest of all the inputs, **Out** becomes this value. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The rounded value of the input.<br/>The **Type** changes to match the input type. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. You will see a list of inputs that you can reorder, rename, change their type, or delete. The minimum number of inputs is two, there is no maximum. Use the “+” button to add elements. If there are 3 or more inputs, you can use the “-” button to delete the selected input. Use the handle at the left of each input to drag and reorder it.

This node also has an empty input that you can link to add a new input.

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
