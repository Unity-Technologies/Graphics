# Set Position (Sequential)

Menu Path : **Position > Set Position (Sequential : \<SequentialMode\>)**

The **Set Position (Sequential)** Block calculates a position based on arithmetic sequences and stores the result in the **position** attribute. Optionally, it can also calculate a position based on an offset index in the sequence and store the result in the **targetPosition** attribute.

There are different modes available which determine which index to use for the sequence, whether to write the position and/or the target position, and how the sequence wraps when it reaches its limits.

This Block also calculates the direction for the sampled **position** and stores it to the [direction attribute](Reference-Attributes.md), based on composition. The way this Block calculates the direction changes depending on the sequence type. The selection types available are:

* **Line Sequencer**: The direction equals the direction of the line from start to finish.
![](Images/Block-SetPosition(Sequential)Line.gif)

* **Circle Sequencer**: The direction is the normal of the circle at the calculated position.
![img](Images/Block-SetPosition(Sequential)Circle.gif)

* **Three Dimensional Sequencer**: The direction equals the normalized vector from the origin to the calculated position.
![img](Images/Block-SetPosition(Sequential)3D.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)

## Block settings

| **Setting**                     | **Type** | **Description**                                              |
| ------------------------------- | -------- | ------------------------------------------------------------ |
| **Composition Position**        | Enum     | **(Inspector)** Specifies how this Block composes the position attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Direction**       | Enum     | **(Inspector)** Specifies how this Block composes the direction attribute. The options are:<br/>&#8226; **Set**: Overwrites the direction attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the direction attribute value.<br/>&#8226; **Multiply**: Multiplies the direction attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the direction attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Target Position** | Enum     | **(Inspector)** Specifies how this Block composes the targetPosition attribute. The options are:<br/>&#8226; **Set**: Overwrites the targetPosition attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the targetPosition attribute value.<br/>&#8226; **Multiply**: Multiplies the targetPosition attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the targetPosition attribute value and the new value. You can specify the blend factor between 0 and 1.<br/>This setting only appears if you enable **Write Target Position**. |
| **Index**                       | Enum     | The index to use to sample the sequence. The options are:<br/>&#8226; **ParticleID**: Uses the particleID attribute.<br/>&#8226; **Custom**: Uses a custom which you provide in the **Index** property. |
| **Write Position**              | Bool     | Toggles whether or not the sequence writes to the [position attribute](Reference-Attributes.md). |
| **Write Target Position**       | Bool     | Toggles whether or not the sequence writes to the [targetposition attribute](Reference-Attributes.md). |
| **Mode**                        | Enum     | The wrap mode to use for the sequence. The options are:<br/>&#8226; **Clamp**: Elements with an index greater than the last element of the sequence repeat the last element of the sequence.<br/>&#8226; **Wrap**: Elements with an index greater than the last element repeat from the first element. <br/>&#8226; **Mirror**: Elements with an index greater than the last element repeat in inverse order, then back into correct order after reaching zero. |

## Block properties

| **Input**                 | **Type** | **Description**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Index**                 | int      | Determines the custom provided index to sample the sequence.<br/>This property only appears if you set **Index** to **Custom**. |
| **Offset Index**          | int      | Applies an offset to the sampled index in order to determine the position in the sequence. |
| **Blend Position**        | Float    | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition Position** to **Blend**. |
| **Blend Direction**       | Float    | The blend percentage between the current direction attribute value and the newly computed direction value.<br/>This property only appears if you set **Composition Direction** to **Blend**. |
| **Offset Target Index**   | int      | Applies an offset to the sampled index in order to determine the targetPosition in the sequence. |
| **Blend Target Position** | Float    | The blend percentage between the current targetPosition attribute value and the newly calculated targetPosition value.<br/>This property only appears if you set **Composition Target Position** to **Blend**. |
