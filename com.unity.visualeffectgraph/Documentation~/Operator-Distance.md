# Distance

Menu Path : **Operator > Math > Vector**

The **Distance** [uniform Operator](Operators.md#uniform-operators) calculates the distance between two points in 1D, 2D, or 3D space.

## Operator properties

| **Input** | **Type**                                | **Description**               |
| --------- | --------------------------------------- | ----------------------------- |
| **A**     | [Configurable](#operator-configuration) | The position to measure from. |
| **B**     | [Configurable](#operator-configuration) | The position to measure to.   |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent | The distance between the two points.<br/>The **Type** changes to match the type of **A** and **B**. |

## Operator configuration

To view this [uniform Operator's](Operators.md#uniform-operators) configuration, click the **cog** icon in the Node's header. Here you can configure the data type this Operator uses.

### Available types

You can use the following types for your **Input values** and ports:

- **Position**
- **Vector**
- **Direction**
- **Vector3**
- **Vector2**
- **Float**
