# Fractional

Menu Path : **Operator > Math > Arithmetic > Fractional**

The **Fractional** Operator returns the fractional part of the input. For example, an input of (4.5, 0, 2.2) outputs (0.5, 0, 0.2).

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types).

## Node properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The fractional of the input. <br/>The **Type** changes to match the largest vector type of the Operator's inputs. |

## Operator configuration

To view the Node's configuration, click the **cog** icon in the Node's header. You can choose a type beyond all [Available Types](#available-types)



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
