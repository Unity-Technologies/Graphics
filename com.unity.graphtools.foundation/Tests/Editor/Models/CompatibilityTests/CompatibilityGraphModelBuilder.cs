using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public static class CompatibilityGraphModelBuilder
    {
        const ulong k_PlacematBaseGuid = 1;
        static readonly (Rect Position, string Title, Color ? Color, bool Collapsed)[] k_Placemats =
        {
            (
                new Rect(15, 15, 500, 400),
                "Placemat 1",
                new Color(.5f, .5f, .5f),
                false),
            (
                new Rect(515, 415, 500, 400),
                "Placemat 2",
                null,
                false)
        };

        const ulong k_StickyNoteBaseGuid = 2;
        static readonly (Rect Position, string Title, string Contents, string Theme, string TextSize)[] k_StickyNotes =
        {
            (
                new Rect(15, 15, 500, 400),
                "StickyNote 1",
                "text",
                StickyNoteColorTheme.Red.ToString(),
                StickyNoteTextSize.Large.ToString()
            )
        };

        const ulong k_NodeBaseGuid = 3;
        static readonly (Vector2 Position, string Title, Color ? Color, ModelState State,
            bool Collapsed, (int ExecIn, int ExecOut, int DataIn, int DataOut)Ports)[] k_Nodes =
        {
            (
                new Vector2(20, 20),
                "Node 1",
                null,
                ModelState.Enabled,
                false,
                (0, 1, 2, 2)
            ),
            (
                new Vector2(-220, 20),
                "Node 2",
                null,
                ModelState.Disabled,
                false,
                (1, 1, 2, 2)
            ),
            (
                new Vector2(-250, 20),
                "Node 3",
                Color.magenta,
                ModelState.Enabled,
                true,
                (1, 0, 2, 0)
            )
        };

        const ulong k_ConstantNodeBaseGuid = 4;
        static readonly (TypeHandle TypeHandle, Vector2 Position, string Title, Color ? Color,
            ModelState State, bool IsLocked, Action<IConstantNodeModel> InitializationCallback)[] k_ConstantNodes =
        {
            (
                TypeHandle.Bool,
                new Vector2(20, -200),
                "Constant 1",
                Color.green,
                ModelState.Enabled,
                false,
                c => c.SetValue(true)
            ),
            (
                TypeHandle.Int,
                new Vector2(20, -250),
                "Constant 2",
                Color.magenta,
                ModelState.Disabled,
                false,
                c => c.SetValue(42)
            ),
            (
                TypeHandle.Double,
                new Vector2(20, -300),
                "Constant 3",
                null,
                ModelState.Enabled,
                true,
                c => c.SetValue(42.0)
            )
        };

        const ulong k_VariableDeclarationBaseGuid = 5;
        const ulong k_VariableNodeBaseGuid = 6;
        static readonly (TypeHandle TypeHandle, string Title, ModifierFlags ModifierFlags, bool IsExposed,
            IConstant InitializationModel)[] k_VariableDeclarations =
        {
            (
                TypeHandle.Bool,
                "Variable Decl 1",
                ModifierFlags.Read,
                false,
                null
            ),
            (
                TypeHandle.Int,
                "Variable Decl 2",
                ModifierFlags.Write,
                false,
                null
            ),
            (
                TypeHandle.Double,
                "Variable Decl 3",
                ModifierFlags.ReadWrite,
                true,
                null
            ),
            (
                TypeHandle.Double,
                "Variable Decl 4",
                ModifierFlags.Read,
                true,
                new DoubleConstant() { Value = 42 }
            )
        };

        const ulong k_EdgeBaseGuid = 7;
        static readonly (SerializableGUID FromNodeGuid, string FromPortName, SerializableGUID ToNodeGuid, string ToPortName)[] k_Edges =
        {
            (
                new SerializableGUID(k_NodeBaseGuid, 0),
                $"{CompatibilityTestNodeModel.outputExecutionPortPrefix}{0}",
                new SerializableGUID(k_NodeBaseGuid, 1),
                $"{CompatibilityTestNodeModel.inputExecutionPortPrefix}{0}"
            ),
            (
                new SerializableGUID(k_NodeBaseGuid, 0),
                $"{CompatibilityTestNodeModel.outputDataPortPrefix}{0}",
                new SerializableGUID(k_NodeBaseGuid, 1),
                $"{CompatibilityTestNodeModel.inputDataPortPrefix}{1}"
            ),
            (
                new SerializableGUID(k_NodeBaseGuid, 1),
                $"{CompatibilityTestNodeModel.outputDataPortPrefix}{0}",
                new SerializableGUID(k_NodeBaseGuid, 2),
                $"{CompatibilityTestNodeModel.inputDataPortPrefix}{0}"
            )
        };

        const ulong k_PortalBaseGuid = 8;
        const ulong k_PortalDeclarationBaseGuid = 9;
        static readonly (Type Type, string Name, Vector2 Position, int? ShareDeclarationWithEntryPortalIndex)[] k_Portals =
        {
            (
                typeof(ExecutionEdgePortalEntryModel),
                "Execution Entry Portal",
                new Vector2(-750, 300),
                null
            ),
            (
                typeof(ExecutionEdgePortalExitModel),
                "Execution Exit Portal",
                new Vector2(-750, 350),
                0
            ),
            (
                typeof(DataEdgePortalEntryModel),
                "Data Entry Portal",
                new Vector2(-750, 400),
                null
            ),
            (
                typeof(DataEdgePortalExitModel),
                "Data Exit Portal",
                new Vector2(-750, 450),
                2
            ),
            (
                typeof(DataEdgePortalEntryModel),
                "Friendless Data Entry Portal",
                new Vector2(-750, 500),
                null
            ),
            (
                typeof(DataEdgePortalExitModel),
                "Friendless Data Exit Portal",
                new Vector2(-750, 550),
                null
            ),
        };

        static readonly TypeHandle[] k_TypeHandles =
        {
            TypeHandle.Bool,
            TypeHandle.Double,
            TypeHandle.Float,
            TypeHandle.Int,
            TypeHandle.Quaternion,
            TypeHandle.String,
            TypeHandle.Vector2,
            TypeHandle.Vector3,
            TypeHandle.Vector4,
            typeof(Color).GenerateTypeHandle(),
            typeof(AnimationClip).GenerateTypeHandle(),
            typeof(Mesh).GenerateTypeHandle(),
            typeof(Texture2D).GenerateTypeHandle(),
            typeof(Texture3D).GenerateTypeHandle()
        };

        const ulong k_SectionBaseGuid = 0xE;

        public static CompatibilityTestGraphAssetModel CreateAsset(string name, string path)
        {
            AssetDatabase.DeleteAsset(path);

            GraphAssetCreationHelpers<CompatibilityTestGraphAssetModel>.CreateGraphAsset(
                typeof(CompatibilityTestStencil), name, path);
            var asset = AssetDatabase.LoadAssetAtPath<CompatibilityTestGraphAssetModel>(path);

            AddPlacemats(asset.GraphModel);
            AddStickyNotes(asset.GraphModel);
            AddNodes(asset.GraphModel);
            AddConstantNodes(asset.GraphModel);
            var section = asset.GraphModel.GetSectionModel(asset.GraphModel.Stencil.SectionNames.First());
            section.Guid =  new SerializableGUID(k_SectionBaseGuid, 0);
            AddVariableDeclarations(asset.GraphModel);
            AddVariableNodes(asset.GraphModel);
            AddEdges(asset.GraphModel);
            AddPortals(asset.GraphModel);

            return asset;
        }

        static void AddPlacemats(IGraphModel graph)
        {
            ulong i = 0;
            foreach (var data in k_Placemats)
            {
                var placemat = graph.CreatePlacemat(data.Position);
                placemat.Guid = new SerializableGUID(k_PlacematBaseGuid, i++);
                placemat.Title = data.Title;
                if (data.Color != null)
                    placemat.Color = data.Color.Value;
                placemat.Collapsed = data.Collapsed;
            }
        }

        static void AddStickyNotes(IGraphModel graph)
        {
            ulong i = 0;
            foreach (var data in k_StickyNotes)
            {
                var sticky = graph.CreateStickyNote(data.Position);
                sticky.Guid = new SerializableGUID(k_StickyNoteBaseGuid, i++);
                sticky.Title = data.Title;
                sticky.Contents = data.Contents;
                sticky.Theme = data.Theme;
                sticky.TextSize = data.TextSize;
            }
        }

        static void AddNodes(IGraphModel graph)
        {
            ulong i = 0;
            foreach (var data in k_Nodes)
            {
                graph.CreateNode<CompatibilityTestNodeModel>(data.Title, data.Position,
                    new SerializableGUID(k_NodeBaseGuid, i++),
                    n =>
                    {
                        if (data.Color != null)
                            n.Color = data.Color.Value;
                        n.State = data.State;
                        n.Collapsed = data.Collapsed;
                        n.PortCounts = data.Ports;
                    });
            }
        }

        static void AddConstantNodes(IGraphModel graph)
        {
            ulong i = 0;
            foreach (var data in k_ConstantNodes)
            {
                var node = graph.CreateConstantNode(data.TypeHandle, data.Title, data.Position,
                    new SerializableGUID(k_ConstantNodeBaseGuid, i++), data.InitializationCallback);

                if (data.Color != null)
                    node.Color = data.Color.Value;
                node.State = data.State;
                node.IsLocked = data.IsLocked;
            }

            var position = new Vector2(-200, -500);
            var delta = new Vector2(0, -50);
            foreach (var typeHandle in k_TypeHandles)
            {
                graph.CreateConstantNode(typeHandle, typeHandle.ToString(), position,
                    new SerializableGUID(k_ConstantNodeBaseGuid, i++));
                position += delta;
            }
        }

        static void AddVariableDeclarations(IGraphModel graph)
        {
            ulong i = 0;

            foreach (var data in k_VariableDeclarations)
            {
                graph.CreateGraphVariableDeclaration(data.TypeHandle, data.Title, data.ModifierFlags, data.IsExposed,
                    null, int.MaxValue, data.InitializationModel,
                    new SerializableGUID(k_VariableDeclarationBaseGuid, i++));
            }

            foreach (var typeHandle in k_TypeHandles)
            {
                graph.CreateGraphVariableDeclaration(typeHandle, typeHandle.ToString(), ModifierFlags.Read,
                    false, null, int.MaxValue, null,
                    new SerializableGUID(k_VariableDeclarationBaseGuid, i++));
            }
        }

        static void AddVariableNodes(IGraphModel graph)
        {
            ulong i = 0;

            var position = new Vector2(-1200, -500);
            var delta = new Vector2(0, -50);
            foreach (var variableDeclarationModel in graph.VariableDeclarations)
            {
                // Create two nodes per variable.
                graph.CreateVariableNode(variableDeclarationModel, position, new SerializableGUID(k_VariableNodeBaseGuid, i++));
                position += delta;
                graph.CreateVariableNode(variableDeclarationModel, position, new SerializableGUID(k_VariableNodeBaseGuid, i++));
                position += delta;
            }
        }

        static void AddEdges(IGraphModel graph)
        {
            ulong i = 0;

            foreach (var data in k_Edges)
            {
                var fromNode = graph.NodeModels.OfType<IPortNodeModel>().FirstOrDefault(n => n.Guid == data.FromNodeGuid);
                var fromPort = fromNode?.Ports.FirstOrDefault(p => p.UniqueName == data.FromPortName);
                Debug.Assert(fromPort != null, $"Cannot find port {data.FromPortName} one node {data.FromNodeGuid}");

                var toNode = graph.NodeModels.OfType<IPortNodeModel>().FirstOrDefault(n => n.Guid == data.ToNodeGuid);
                var toPort = toNode?.Ports.FirstOrDefault(p => p.UniqueName == data.ToPortName);
                Debug.Assert(toPort != null, $"Cannot find port {data.ToPortName} one node {data.ToNodeGuid}");

                graph.CreateEdge(fromPort, toPort, new SerializableGUID(k_EdgeBaseGuid, i++));
            }
        }

        static void AddPortals(IGraphModel graph)
        {
            ulong i = 0;
            ulong j = 0;

            foreach (var data in k_Portals)
            {
                EdgePortalModel portal = null;
                if (data.Type == typeof(ExecutionEdgePortalEntryModel))
                {
                    portal = graph.CreateNode<ExecutionEdgePortalEntryModel>(data.Name, data.Position, new SerializableGUID(k_PortalBaseGuid, i++));
                }
                else if (data.Type == typeof(ExecutionEdgePortalExitModel))
                {
                    portal = graph.CreateNode<ExecutionEdgePortalExitModel>(data.Name, data.Position, new SerializableGUID(k_PortalBaseGuid, i++));
                }
                else if (data.Type == typeof(DataEdgePortalEntryModel))
                {
                    portal = graph.CreateNode<DataEdgePortalEntryModel>(data.Name, data.Position, new SerializableGUID(k_PortalBaseGuid, i++));
                }
                else if (data.Type == typeof(DataEdgePortalExitModel))
                {
                    portal = graph.CreateNode<DataEdgePortalExitModel>(data.Name, data.Position, new SerializableGUID(k_PortalBaseGuid, i++));
                }

                if (portal != null)
                {
                    if (data.ShareDeclarationWithEntryPortalIndex == null)
                    {
                        portal.DeclarationModel = graph.CreateGraphPortalDeclaration(data.Name, new SerializableGUID(k_PortalDeclarationBaseGuid, j++));
                    }
                    else
                    {
                        var otherPortal = graph.NodeModels.OfType<EdgePortalModel>().ElementAt(data.ShareDeclarationWithEntryPortalIndex.Value);
                        Assert.IsNotNull(otherPortal);
                        Assert.IsTrue(otherPortal is IEdgePortalEntryModel);
                        portal.DeclarationModel = otherPortal.DeclarationModel;
                    }
                }
            }
        }
    }
}
