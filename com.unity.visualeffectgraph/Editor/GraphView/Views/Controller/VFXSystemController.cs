#define NOTIFICATION_VALIDATION
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Profiling;

using UnityObject = UnityEngine.Object;
using Branch = UnityEditor.VFX.Operator.Branch;

namespace UnityEditor.VFX.UI
{
    internal class VFXSystemController : Controller<VFXUI>
    {
        VFXViewController m_ViewController;
        public VFXSystemController(VFXViewController viewController,VFXUI model):base(model)
        {
            m_ViewController = viewController;
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {

        }

        public string title
        {
            get
            {
                return contexts.Length > 0 ? contexts[0].model.GetGraph().systemNames.GetUniqueSystemName(contexts[0].model.GetData()) : string.Empty;
            }
            set
            {
                if (value != title && contexts.Length > 0)
                {
                    VFXData data = contexts.First().model.GetData();
                    if (data != null)
                    {
                        int index = value.IndexOfAny(new char[] { '\r', '\n' });
                        data.title = index == -1 ? value : value.Substring(0, index);
                    }
                    data.owners.First().Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                }
            }
        }

        internal VFXContextController[] contexts { get; set; }
    }
}
