# Ceiling

Menu Path : **Operator > Math > Clamp > Ceiling**

The **Ceiling** Operator rounds the input up to the nearest integer. For example, an input of (4.1, 4, 4.8) outputs (5, 4, 5).

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types).

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The input (**X**) rounded up to the nearest integer.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view this Operator’s configuration, click the **cog** icon in the Operator’s header. You can choose a type beyond all [Available Types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
