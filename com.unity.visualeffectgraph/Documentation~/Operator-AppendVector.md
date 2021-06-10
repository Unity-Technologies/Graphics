# Append Vector

Menu Path : **Operator > Math > Vector**

The **Append Vector** [cascaded Operator](Operators.md#cascaded-operators) appends all the inputs sequentially and outputs them into a single Vector with a maximum length of four. If the inputs include more than four values, this Operator discards further values. For example, if the inputs are a Vector3 and a Vector2, the output is a Vector4 that includes the three values from the Vector3 and the first value from the Vector2.

## Operator properties

| **Input**                                                  | **Type**                                | **Description**                                  |
| ---------------------------------------------------------- | --------------------------------------- | ---------------------------------------------- |
| [Configurable](#operator-configuration) - **A** by default | [Configurable](#operator-configuration) | An input value to append to the output Vector. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent | A Vector packed with the inputs.<br/>The **Type** changes to match the number of values the inputs include. This can be a float, Vector2, Vector3, or Vector4. |

## Operator configuration

To view this [cascaded Operator's](Operators.md#cascaded-operators) configuration, click the **cog** icon in the Node's header.

Here, you can change the name of each input as well as the type for each input's value.

### Available types

You can use the following types for your **Input values** and ports:

- **Position**
- **Vector**
- **Direction**
- **Vector3**
- **Vector2**
- **Float**
