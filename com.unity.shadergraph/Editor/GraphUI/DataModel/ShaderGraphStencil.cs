using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphStencil : Stencil
    {
        public const string Name = "ShaderGraph";

        public override string ToolName => Name;

        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel) => new SGBlackboardGraphModel(graphAssetModel);

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return typeHandle == ShaderGraphTypes.DayOfWeek
                ? typeof(DayOfWeekConstant)
                : TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return new ShaderGraphSearcherDatabaseProvider(this,
                new List<ShaderGraphSearcherDatabaseProvider.Preset>
                {
                    new("Vectors")
                    {
                        inputs =
                        {
                            {"Vec2", TypeHandle.Vector2},
                            {"Vec3", TypeHandle.Vector3},
                            {"Vec4", TypeHandle.Vector4},
                        },
                        outputs =
                        {
                            {"Vec2", TypeHandle.Vector2},
                            {"Vec3", TypeHandle.Vector3},
                            {"Vec4", TypeHandle.Vector4},
                        }
                    },

                    new("And")
                    {
                        inputs =
                        {
                            {"A", TypeHandle.Bool},
                            {"B", TypeHandle.Bool},
                        },
                        outputs =
                        {
                            {"A AND B", TypeHandle.Bool},
                        }
                    },

                    new("Construct Vec3")
                    {
                        inputs =
                        {
                            {"X", TypeHandle.Float},
                            {"Y", TypeHandle.Float},
                            {"Z", TypeHandle.Float},
                        },
                        outputs =
                        {
                            {"Vec3", TypeHandle.Vector3},
                        }
                    },

                    new("Sample Texture2D")
                    {
                        inputs =
                        {
                            {"UV", TypeHandle.Vector2},
                            {"Texture", ShaderGraphTypes.Texture2D},
                        },
                        outputs =
                        {
                            {"RGBA", TypeHandle.Vector4},
                        }
                    },
                });
        }
    }
}
