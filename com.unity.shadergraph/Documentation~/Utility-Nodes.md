# Utility Nodes

| [Preview](Preview-Node.md) | [Sub-Graph](Sub-graph-Node.md) |
|:-------------|:------|
| ![Image](images/PreviewNodeThumb.png) | ![Image](images/SubgraphNodeThumb.png) |
| Provides a preview window and passes the input value through without modification. | Provides a reference to a Sub-graph asset. |

## Logic

| [All](All-Node.md) | [And](And-Node.md) |
|:-------------|:------|
| ![Image](images/AllNodeThumb.png) | ![Image](images/AndNodeThumb.png) |
| Returns true if all components of the input In are non-zero. | Returns true if both the inputs A and B are true. |
|[**Any**](Any-Node.md)|[**Branch**](Branch-Node.md)|
|![Image](images/AnyNodeThumb.png)|![Image](images/BranchNodeThumb.png)|
|Returns true if any of the components of the input In are non-zero.|Provides a dynamic branch to the shader.|
|[**Comparison**](Comparison-Node.md)|[**Is Infinite**](Is-Infinite-Node.md)|
|![Image](images/ComparisonNodeThumb.png)|![Image](images/IsInfiniteNodeThumb.png)|
|Compares the two input values A and B based on the condition selected on the dropdown.|Returns true if any of the components of the input In is an infinite value.|
|[**Is NaN**](Is-NaN-Node.md)|[**Nand**](Nand-Node.md)|
|![Image](images/IsNaNNodeThumb.png)|![Image](images/NandNodeThumb.png)|
|Returns true if any of the components of the input In is not a number (NaN).|Returns true if both the inputs A and B are false.|
|[**Not**](Not-Node.md)|[**Or**](Or-Node.md)|
|![Image](images/NotNodeThumb.png)|![Image](images/OrNodeThumb.png)|
|Returns the opposite of input In. If In is true, the output is false. Otherwise, it returns true.|Returns true if either input A or input B is true.|

