# Sample Point Cache

**Menu Path : Operator > Sampling > Sample Point Cache**

The Sample Point Cache Operator enables you to sample a [Point Cache](point-cache-in-vfx-graph.md).

This Operator takes a [Point Cache asset](point-cache-asset.md) and an index then outputs the values of each point attribute at the index. It also outputs the number of points in the Point Cache. The output attribute values can be of various types depending on the data they store.

![](Images/Operator-SamplePointCache.png)

### Operator settings

| **Setting** | **Description**                                              |
| ----------- | ------------------------------------------------------------ |
| **Asset**   | The Point Cache asset to sample from.                        |
| **Mode**    | The wrap mode to use if the value of **Index** is out of range for the Point Cache **Asset**. The options are: <br/>&#8226; **Clamp**: Clamps the index between the first and last index of the **Map**.<br/>&#8226; **Wrap**: Wraps the index around to the other side of the **Map**.<br/>&#8226; **Mirror**: Mirrors the vertex list so out of range indices move back and forth through the **Map**. |

### Operator properties

| **Input** | **Type** | **Description**                   |
| --------- | -------- | --------------------------------- |
| **Index** | uint     | The index of the point to sample. |

| **Output**       | **Type**  | **Description**                                              |
| ---------------- | --------- | ------------------------------------------------------------ |
| **Point Count**  | uint      | The number of points present in the Point Cache              |
| **\<attribute>** | Dependent | The content of each attribute map at the point specified by the input Index.<br/>**Note**: The **Type** changes depending on the data the attribute map stores. |
