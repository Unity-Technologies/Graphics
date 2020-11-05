# Discretize

Menu Path : **Operator > Math > Clamp > Discretize**  

The **Discretize** Operator outputs the multiple of the **B** value that is closest to **A**. For example, an input of **A** = 7.2 and **B** = 3, outputs 6.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The **B** input can be either the same as **A** or a float.

## Operator properties

| **Input** | **Type**                                | **Description**                                              |
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **A**     | [Configurable](#operator-configuration) | The value to discretize. The Operator rounds this value to the nearest multiple of **B**. |
| **B**     | [Configurable](#operator-configuration) | The value this Operator discretizes **A** to. This input type can be either a float or the same type as **A**. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The rounded value of the input.<br/>The **Type** changes to match the type of **A**. |

## Operator configuration

To view the Node's configuration, click the **cog** icon in the Node's header. 

### Available types

You can use the following types for your **input** ports:

- **Float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**