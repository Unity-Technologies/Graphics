# Normalize

Menu Path : **Operator > Math > Vector**

The **Normalize** [uniform Operator](Operators.md#uniform-operators) normalizes an input vector.


Any zero-length input vector results in a Not-A-Number (NaN) result, which breaks subsequent calculations.

## Operator properties

| **Input** | **Type**                                | **Description**  |
| --------- | --------------------------------------- | ---------------- |
| **X**     | [Configurable](#operator-configuration) | The input vector |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent | The normalized version of **X**.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view this [uniform Operator's](Operators.md#uniform-operators) configuration, click the **cog** icon in the Node's header. Here you can configure the data type this Operator uses.

### Available types

You can use the following types for your **Input values** and ports:

- **Position**
- **Vector**
- **Direction**
- **Vector3**
- **Vector4**
- **Vector2**
- **Float**