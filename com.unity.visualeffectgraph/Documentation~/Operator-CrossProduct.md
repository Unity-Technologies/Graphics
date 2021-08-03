# Cross Product

Menu Path : **Operator > Math > Vector**

The **Cross Product** [uniform Operator](Operators.md#uniform-operators) performs a [cross-product](https://docs.unity3d.com/ScriptReference/Vector3.Cross.html) between two input vectors and outputs the result.

The cross-product of two vectors is a vector that is perpendicular to the two vectors. Its length depends on the angle between the two input vectors as well as their length.

The cross-product of two perpendicular normalized vectors is a normalized vector perpendicular to the two vectors.

## Operator properties

| **Input** | **Type**                                | **Description**                         |
| --------- | --------------------------------------- | --------------------------------------- |
| **A**     | [Configurable](#operator-configuration) | The first vector of the cross product.  |
| **B**     | [Configurable](#operator-configuration) | The second vector of the cross product. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent | The result of the cross product.<br/>The **Type** changes to match the type of **A** and **B**. |

## Operator configuration

To view this [uniform Operator's](Operators.md#uniform-operators) configuration, click the **cog** icon in the Node's header. Here you can configure the data type this Operator uses.

### Available types

You can use the following types for your **Input values** and ports:

- **Position**
- **Vector**
- **Direction**
- **Vector3**