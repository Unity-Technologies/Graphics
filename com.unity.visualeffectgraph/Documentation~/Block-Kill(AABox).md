# Kill (AABox)

Menu Path : **Kill >** **Kill (AABox)**  

The Kill (AABox) Block kills (sets the `alive` attribute to false) particles depending on where they are in comparison to a given [AABox](Type-AABox.md).

## Block settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Mode**    | Enum     | The method Unity uses to determine whether to kill a particle. The options are:<br/>&#8226; **Solid**: Kills particles within the AABox.<br/>&#8226; **Inverted**: Kills particles outside of the AABox. |

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block properties

| **Input** | **Type**               | **Description**                          |
| --------- | ---------------------- | ---------------------------------------- |
| **Box**   | [AABox](Type-AABox.md) | The box to compare particle position to. |

## Remark

- If you use this block inside an output context, particles temporarily disappear, but don't stay killed.
- Make sure to enable the **Reap Particles** option in update, otherwise killed particles don't disappear.