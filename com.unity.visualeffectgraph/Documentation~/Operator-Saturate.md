# Saturate

Menu Path : **Operator > Math > Clamp > Saturate**  

The **Saturate** Operator clamps the return value between 0 and 1.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types).

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **Input** | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Input** | **Type**  | **Description**                                              |
| --------- | --------- | ------------------------------------------------------------ |
| **Out**   | Dependent | The input value clamped between 0 and 1.<br/>The **Type** changes to match the type of **Input**. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. You can choose a type beyond all [Available Types](#available-types). 



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**