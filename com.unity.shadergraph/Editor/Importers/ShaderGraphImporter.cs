using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(1, Extension, -902)]
    class ShaderGraphImporter : ScriptedImporter
    {
        public const string Extension = "sg2";

        public const string k_ErrorShader = @"
Shader ""Hidden/GraphErrorShader2""
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include ""UnityCG.cginc""

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1,0,1,1);
            }
            ENDCG
        }
    }
    Fallback Off
}";

        private Shader GetShader(AssetImportContext ctx)
        {
            return ShaderUtil.CreateShaderAsset(ctx, k_ErrorShader, false);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string path = ctx.assetPath;
            string fileText = File.ReadAllText(path, Encoding.UTF8);
            ShaderGraphAssetHelper helper = ScriptableObject.CreateInstance<ShaderGraphAssetHelper>();
            EditorJsonUtility.FromJsonOverwrite(fileText, helper);

            //GraphDelta.GraphDelta graph = new GraphDelta.GraphDelta(helper.GraphDeltaJSON);

            ShaderGraphAssetModel model = ScriptableObject.CreateInstance<ShaderGraphAssetModel>();
            model.CreateGraph("foo", typeof(ShaderGraphStencil));
            EditorJsonUtility.FromJsonOverwrite(helper.GTFJSON, model);
            model.Init();

            var shader = GetShader(ctx);
            Material mat = new Material(shader);

            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon");
            ctx.AddObjectToAsset("MainAsset", shader, texture);
            ctx.SetMainObject(shader);
            ctx.AddObjectToAsset("View", model);
            ctx.AddObjectToAsset("Material", mat);
        }
    }
}
