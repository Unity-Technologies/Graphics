# Change Space

Menu Path : **Operator > Math > Geometry > Change Space**

The **Change Space** Operator can change the space of an input from local to world space or from world to local space.

You can use this to convert the Position of particles in a system from local space to their corresponding world-space position.

## Operator settings

| **Setting**      | **Type** | **Description**                                              |
| ---------------- | -------- | ------------------------------------------------------------ |
| **Target Space** | enum     | Specifies the space to change the input to. The options are:<br/>&#8226;**Local**: Changes the input to local space.<br/>&#8226;**World**: Changes the input to world space. |

## Operator properties

| **Input** | **Type**                                | **Description**                               |
| --------- | --------------------------------------- | --------------------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The input this Operator changes the space of. |

| **Output** | **Type**                                | **Description**                   |
| ---------- | --------------------------------------- | --------------------------------- |
| **-**      | [Configurable](#operator-configuration) | The input in its converted space. |

## Operator configuration

To view this Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the input type. For the list of types these properties support, see [Available types](#available-types).



### Available types

You can use the following types for your **Input values** and **Output** ports:

- **Direction**
- **Position**
- **Vector**
