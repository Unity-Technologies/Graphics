# Master Stack 

## Description

The Master Stack is the end point of a Shader Graph that defines the final appearance of the shader. Your Shader Graph should always contain one, and only one, Master Stack. 

![image](images/MasterStack_Populated.png)

The content of the Master Stack may change depending on the [Graph Settings](Graph-Settings-Menu.md) selected. The master stack is made up of **Contexts**, which contain [Block Nodes](Block-Node.md). 

## Contexts
![image](images/MasterStack_Empty.png)

The Master Stack contains two **Contexts**, Vertex and Fragment. These represent the two stages of a shader. Anything connected to the Vertex Context is performed in the vertex calculations of the shader, and anything connected to the Fragment Context is performed in the fragment (or pixel) calculations of the shader. Contexts cannot be cut, copied, or pasted.
