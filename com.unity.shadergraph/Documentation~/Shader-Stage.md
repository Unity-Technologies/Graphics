# Shader Stage

## Description

**Shader Stage** refers to the part of the shader pipeline a [Node](Node.md) or [Port](Port.md) is part of. For example, **Vertex** or **Fragment**.

In [Shader Graph](Shader-Graph.md), **Shader Stage** is defined per [Port](Port.md) but often all [Ports](Port.md) on a [Node](Node.md) are locked to the same **Shader Stage**.  [Ports](Port.md) on some [Nodes](Node.md) are unavailable in certain **Shader Stages** due to limitations in the underlying shader language. See the [Node Library](Node-Library.md) documentation for [Nodes](Node.md) that have **Shader Stage** restrictions.

## Shader Stage List

| Name        | Description                        |
|:------------|:-----------------------------------|
| Vertex      | Operations calculated per vertex   |
| Fragment    | Operations calculated per fragment |