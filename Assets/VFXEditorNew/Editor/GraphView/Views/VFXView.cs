using System;
using System.Linq;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.VFX.UI
{
    class VFXNodeProvider : VFXAbstractProvider<VFXNodeProvider.Descriptor>
    {
        public class Descriptor
        {
            public object modelDescriptor;
            public string category;
            public string name;
        }

        public VFXNodeProvider(Action<Descriptor, Vector2> onAddBlock) : base(onAddBlock)
        {
        }

        protected override string GetCategory(Descriptor desc)
        {
            return desc.category;
        }

        protected override string GetName(Descriptor desc)
        {
            return desc.name;
        }
        protected override IEnumerable<Descriptor> GetDescriptors()
        {
            var descriptorsContext = VFXLibrary.GetContexts().Select(o =>
            {
                return new Descriptor()
                {
                    modelDescriptor = o,
                    category = "Context/" + o.info.category,
                    name = o.name
                };
            });

            var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
            {
                return new Descriptor()
                {
                    modelDescriptor = o,
                    category = "Operator/" + o.info.category,
                    name = o.name
                };
            });

            return descriptorsContext.Concat(descriptorsOperator);
        }
    }

    //[StyleSheet("Assets/VFXEditorNew/Editor/GraphView/Views/")]
    class VFXView : GraphView
    {
        public VFXView()
        {
            forceNotififcationOnAdd = true;
            AddManipulator(new ContentZoomer());
			AddManipulator(new ContentDragger());
			AddManipulator(new RectangleSelector());
			AddManipulator(new SelectionDragger());
			AddManipulator(new ClickSelector());
			AddManipulator(new ShortcutHandler(
				new Dictionary<Event, ShortcutDelegate>
				{
					{Event.KeyboardEvent("a"), FrameAll},
					{Event.KeyboardEvent("f"), FrameSelection},
					{Event.KeyboardEvent("o"), FrameOrigin},
					{Event.KeyboardEvent("delete"), DeleteSelection},
					{Event.KeyboardEvent("#tab"), FramePrev},
					{Event.KeyboardEvent("tab"), FrameNext}
				}));

            var bg = new GridBackground() { name = "VFXBackgroundGrid" };
            InsertChild(0, bg);


            
            AddManipulator(new FilterPopup(new VFXNodeProvider((d, mPos) =>
            {
                Vector2 tPos = this.ChangeCoordinatesTo(contentViewContainer, mPos);
                if (d.modelDescriptor is VFXModelDescriptor<VFXOperator>)
                {
                    AddVFXOperator(tPos, (d.modelDescriptor as VFXModelDescriptor<VFXOperator>).CreateInstance());
                }
                else if (d.modelDescriptor is VFXModelDescriptor<VFXContext>)
                {
                    AddVFXContext(tPos, d.modelDescriptor as VFXModelDescriptor<VFXContext>);
                }
            }), null));
            
            typeFactory[typeof(VFXOperatorPresenter)] = typeof(VFXOperatorUI);

            typeFactory[typeof(VFXContextPresenter)] = typeof(VFXContextUI);
            typeFactory[typeof(VFXFlowEdgePresenter)] = typeof(VFXFlowEdge);
            typeFactory[typeof(VFXDataEdgePresenter)] = typeof(VFXDataEdge);
            typeFactory[typeof(VFXFlowInputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXFlowOutputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXDataInputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXDataOutputAnchorPresenter)] = typeof(VFXDataAnchor);

            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);
        }

        void AddVFXContext(Vector2 pos,VFXModelDescriptor<VFXContext> desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXContext(pos,desc);
        }

        void AddVFXOperator(Vector2 pos, VFXOperator desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXOperator(pos, desc);
        }
    }
}
