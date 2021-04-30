# Dot Product

Menu Path : **Operator > Math > Vector**

The **Dot Product** [uniform Operator](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@8.0/manual/Operators.html#uniform-operators) performs a [dot-product](https://docs.unity3d.com/ScriptReference/Vector3.Dot.html) between two input vectors and outputs the result.

This Operator is useful for numerous purposes which include:

- To compute the squared length of a given vector. To do this, connect the same vector to both input ports.
- To determine whether two normalized vectors point in the same direction. This returns 1.0 if the vectors point in the same direction, 0.0 if they are perpendicular, and -1.0 if they point in opposite directions.
- To project a vector **A** on a normalized vector **B** and return its projected length along **B**.

## Operator properties

| **Input** | **Type**                                | **Description**                       |
| --------- | --------------------------------------- | ------------------------------------- |
| **A**     | [Configurable](#operator-configuration) | The first vector of the dot product.  |
| **B**     | [Configurable](#operator-configuration) | The second vector of the dot product. |

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **Output** | float    | The result of the dot product.<br/>The **Type** changes to match the type of **A** and **B**. |

## Operator configuration

To view this [uniform Operator's](Operators.md#uniform-operators) configuration, click the **cog** icon in the Node's header. Here you can configure the data type this Operator uses.

### Available types

You can use the following types for your **Input values** and ports:

- **Position**
- **Vector**
- **Direction**
- **Vector3**
- **Vector2**
