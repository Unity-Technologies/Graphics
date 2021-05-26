using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.Converters;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal sealed class AnimationClipConverter : RenderPipelineConverter
    {
        public override string name => "Animation Clip Converter";
        public override string info => "Need to update all Animation Clips. This will run after Materials has been converted.";
        public override string category { get; }
        public override Type container => typeof(BuiltInToURPConverterContainer);
        public override void OnInitialize(InitializeConverterContext ctx, Action callback)
        {
            callback.Invoke();
        }

        public override void OnRun(ref RunItemContext ctx)
        {
        }

        public override void OnPostRun(ConverterState converterState, List<ConverterItemDescriptor> itemsToConvert)
        {
            //var matUpgraders = new List<MaterialUpgrader>();
            //UniversalRenderPipelineMaterialUpgrader urpUpgrader = new UniversalRenderPipelineMaterialUpgrader();
            //matUpgraders = urpUpgrader.upgraders;

            //AnimationClipUpgrader.DoUpgradeAllClipsMenuItem(matUpgraders, "Upgrade Animation Clips to URP Materials");

            // Create a list here with all our data
            // Populate that into items
            // The Converter need to know the items it supposed to show
            converterState.items = new List<ConverterItemState>();

            for (int i = 0; i < 15; i++)
            {
                itemsToConvert.Add(new ConverterItemDescriptor
                {
                    name = "Muppet : 12345678910111213141516171819 ::  " + i,
                    info = "",
                    warningMessage = "",
                    helpLink = "",
                });

                Status status;
                string msg;
                status = i % 2 == 0 ? Status.Success : Status.Error;
                msg = i % 2 == 0 ? "Status.Success" : "Status.Error";
                converterState.items.Add(new ConverterItemState
                {
                    isActive = true,
                    message = msg,
                    status = status,
                    hasConverted = true,
                });
                if (status == Status.Success)
                {
                    converterState.success++;
                }
                else
                {
                    converterState.errors++;
                }
            }
        }
    }
}
