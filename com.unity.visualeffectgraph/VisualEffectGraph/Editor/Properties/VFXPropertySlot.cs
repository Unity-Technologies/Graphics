using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor; // Shouldnt be included!
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEngine;

namespace UnityEngine.Experimental.VFX
{
    // Transform mode stuff
    [Flags]
    public enum SlotTransformMode
    {
        kUsed = 1 << 0,        // Set if semantics CanTransform returns true
        kInherited = 1 << 1,   // Is the transform inherited from connected slot
        kHidden = 1 << 2,      // Is the transform hidden in the editor
        kWorld = 1 << 3,       // Is the transform mode in world (local otherwise)
    }

    public enum SpaceRef
    {
        kWorld,
        kLocal,
        kNone, // None at the end as this enum is cast to int to index an array on C++ side and kNone is never used
    }

    public interface VFXPropertySlotObserver
    {
        void OnSlotEvent(VFXPropertySlot.Event type,VFXPropertySlot slot);
    }

    public struct VFXNamedValue
    {
        public VFXNamedValue(string name, VFXExpression value)
        {
            m_Name = name;
            m_Value = value;
        }

        public string m_Name;
        public VFXExpression m_Value;
    }

    public abstract class VFXPropertySlot : VFXUIDataHolder
    {
        public enum Event
        {
            kLinkUpdated,
            kValueUpdated,
            kTransformModeUpdated,
            kExposedUpdated,
        }

        public VFXPropertySlot() {}

        protected void Init<T>(VFXPropertySlot parent, VFXProperty desc) where T : VFXPropertySlot, new()
        {
            m_Desc = desc;

            // Init Transform mode
            m_TransformMode = 0;
            if (Semantics.CanTransform())
                m_TransformMode |= SlotTransformMode.kUsed;
            if (parent != null && (parent.m_TransformMode & (SlotTransformMode.kUsed | SlotTransformMode.kInherited)) != 0)
                m_TransformMode |= SlotTransformMode.kInherited;

            CreateChildren<T>();
            Semantics.CreateValue(this);
            SetDefault();

            // Set the parent at the end
            m_Parent = parent;
        }

        protected void CreateChildren<T>() where T : VFXPropertySlot, new()
        {
            VFXProperty[] children = Semantics.GetChildren();
            if (children != null)
            {
                int nbChildren = children.Length;
                m_Children = new VFXPropertySlot[nbChildren];
                for (int i = 0; i < nbChildren; ++i)
                {
                    VFXPropertySlot child = new T();     
                    child.Init<T>(this, children[i]);
                    m_Children[i] = child;
                }
            }
        }

        public void SetDefault()
        {
            if (!Semantics.Default(this))
                foreach (var child in m_Children)
                    child.SetDefault();
        }

        public int GetNbChildren()
        {
            return m_Children.Length;
        }

        public VFXPropertySlot GetChild(int index)
        {
            return m_Children[index];
        }

        public VFXPropertySlot Parent
        {
            get { return m_Parent; }
        }

        public T Get<T>(bool linked = false)        { return Semantics.Get<T>(this, linked); }
        public void Set<T>(T t,bool linked = false) { Semantics.Set(this,t,linked); }
   
        // Direct access to owned value
        // Prefer using get<T> and Set<T> instead to correctly set value depending on the semantics
        public void SetInnerValue<T>(T t)
        {
            if (m_OwnedValue.Set(t))
                NotifyChange(Event.kValueUpdated);
        }
        public T GetInnerValue<T>() { return m_OwnedValue.Get<T>(); }

        public VFXExpression Value
        {
            set
            {
                if (value != m_OwnedValue)
                {
                    m_OwnedValue = value;
                    NotifyChange(Event.kLinkUpdated); // Link updated as expressions needs to be recomputed 
                }
            }
            get
            {
                return m_OwnedValue;
            }
        }

        public VFXExpression ValueRef
        {
            set { CurrentValueRef.Value = value; }
            get { return CurrentValueRef.Value; }
        }

        public void AddObserver(VFXPropertySlotObserver observer, bool addRecursively = false)
        {
            if (observer != null && !m_Observers.Contains(observer))
                m_Observers.Add(observer);

            if (addRecursively)
                foreach (var child in m_Children)
                    child.AddObserver(observer, true);
        }

        public void RemoveObserver(VFXPropertySlotObserver observer, bool removeRecursively = false)
        {
            m_Observers.Remove(observer);

            if (removeRecursively)
                foreach (var child in m_Children)
                    child.RemoveObserver(observer, true);
        }

        public void NotifyChange(Event type)
        {
            // Invalidate expression cache
            if (m_OwnedValue != null)
            {
                m_OwnedValue.Invalidate();
                m_OwnedValue.Reduce(); // Trigger a reduce but this is TMP as it should be reduced lazily (the model compiler should do it on demand)
            }

            foreach (var observer in m_Observers)
                observer.OnSlotEvent(type, this);

            PropagateChange(type);

            // Invalidate parent's cache and Update parent proxy if any in case of link update
            if (m_Parent != null)
                if (type == Event.kValueUpdated || m_Parent.Semantics.UpdateProxy(m_Parent))
                    m_Parent.NotifyChange(type);
        }

        public virtual void PropagateChange(Event type) {}

        public abstract VFXPropertySlot CurrentValueRef { get; }

        public bool IsValueUsed()
        {
            return CurrentValueRef == this;
        }

        public abstract bool IsLinked();
        public abstract void UnlinkAll();
        public void UnlinkRecursively()
        {
            UnlinkAll();
            foreach (var child in m_Children)
                child.UnlinkRecursively();
        }

        public void Link(VFXPropertySlot slot)
        {
            if (this is VFXInputSlot)
                ((VFXInputSlot)this).Link((VFXOutputSlot)slot);
            else
                ((VFXOutputSlot)this).Link((VFXInputSlot)slot);
        }

        public VFXProperty Property                 { get { return m_Desc; }}
        public string Name                          { get { return m_Desc.m_Name; }}
        public VFXPropertyTypeSemantics Semantics   { get { return m_Desc.m_Type; }}
        public VFXValueType ValueType               { get { return Semantics.ValueType; }}

        public void FlattenValues<T>(List<T> values)
        {
            if (GetNbChildren() == 0)
            {
                if (ValueType == VFXValue.ToValueType<T>())
                    values.Add(Get<T>());
            }
            else foreach (var child in m_Children)
                    child.FlattenValues(values);
        }

        public int ApplyValues<T>(List<T> values, int index = 0)
        {
            if (GetNbChildren() == 0)
            {
                if (ValueType == VFXValue.ToValueType<T>())
                    Set(values[index++]);
            }
            else foreach (var child in m_Children)
                    index = child.ApplyValues(values, index);

            return index;
        }

        public void GetStringValues(XmlWriter writer)
        {
            if (GetNbChildren() == 0)
            {
                string name = ValueType.ToString();
                switch (ValueType)
                {
                    case VFXValueType.kFloat:

                        writer.WriteElementString(name, Value.Get<float>().ToString());
                        break;
                    /* case VFXValueType.kFloat2:
                         output.Add(Value.Get<Vector2>().ToString());
                         break;
                     case VFXValueType.kFloat3:
                         output.Add(Value.Get<Vector3>().ToString());
                         break;
                     case VFXValueType.kFloat4:
                         output.Add(Value.Get<Vector4>().ToString());
                         break;*/
                    case VFXValueType.kInt:
                        writer.WriteElementString(name, Value.Get<int>().ToString());
                        break;
                    case VFXValueType.kUint:
                        writer.WriteElementString(name, Value.Get<uint>().ToString());
                        break;
                    case VFXValueType.kTexture2D:
                        writer.WriteElementString(name, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Value.Get<Texture2D>())));
                        break;
                    case VFXValueType.kTexture3D:
                        writer.WriteElementString(name, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Value.Get<Texture3D>())));
                        break;
                    case VFXValueType.kCurve:
                        SerializationUtils.WriteCurve(writer, Value.Get<AnimationCurve>());
                        break;
                    case VFXValueType.kColorGradient:
                        SerializationUtils.WriteGradient(writer, Value.Get<Gradient>());
                        break;
                    default:
                        Debug.LogWarning("Cannot serialize value of type " + ValueType);
                        break;
                }
            }
            else foreach (var child in m_Children)
                    child.GetStringValues(writer);
        }

        public void SetValuesFromString(XmlReader reader)
        {
            if (GetNbChildren() == 0)
            {
                switch (ValueType)
                {
                    case VFXValueType.kFloat:
                        reader.MoveToElement();
                       // reader.MoveToContent();
                        Set(reader.ReadElementContentAsFloat());
                        break;
                    /*case VFXValueType.kFloat2:
                       Set(Vector2.Parse(input[index]));
                       break;
                    case VFXValueType.kFloat3:
                       output.Add(Value.Get<Vector3>().ToString());
                       break;
                   case VFXValueType.kFloat4:
                       output.Add(Value.Get<Vector4>().ToString());
                       break;*/
                    case VFXValueType.kInt:
                        reader.MoveToElement();
                        Set(reader.ReadElementContentAsInt());
                        break;
                    case VFXValueType.kUint:
                        reader.MoveToElement();
                        Set((uint)reader.ReadElementContentAsInt());
                        break;
                    case VFXValueType.kTexture2D:
                        reader.MoveToElement();
                        Set(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(reader.ReadElementContentAsString())));
                        break;
                    case VFXValueType.kTexture3D:
                        reader.MoveToElement();
                        Set(AssetDatabase.LoadAssetAtPath<Texture3D>(AssetDatabase.GUIDToAssetPath(reader.ReadElementContentAsString())));
                        break;
                    case VFXValueType.kCurve:
                        reader.MoveToElement();
                        Set(SerializationUtils.ReadCurve(reader));
                        break;
                    case VFXValueType.kColorGradient:
                        reader.MoveToElement();
                        Set(SerializationUtils.ReadGradient(reader));
                        break;
                    default:
                        Debug.LogWarning("Cannot deserialize value of type " + ValueType);
                        break;
                }
            }
            else foreach (var child in m_Children)
                    child.SetValuesFromString(reader);
        }

        public void CopyValuesFrom(VFXPropertySlot slot)
        {
            var buffer = new StringBuilder();
            var writer = XmlWriter.Create(buffer);
            writer.WriteStartElement("Values");
            slot.GetStringValues(writer);
            writer.WriteEndElement();
            writer.Flush();

            var str = buffer.ToString();
            Debug.Log("SLOT VALUES:\n" + str);

            var reader = XmlReader.Create(new StringReader(buffer.ToString()));
            reader.ReadToFollowing("Values");
            while (reader.Read() && reader.NodeType != XmlNodeType.Element) {} // Advance to element
            SetValuesFromString(reader);
        }

        // Collect all values in the slot hierarchy with its name used in the shader
        // Called from the model compiler
        public void CollectNamedValues(List<VFXNamedValue> values, SpaceRef spaceRef = SpaceRef.kNone)
        {
            CollectNamedValues(values, spaceRef, "");
        }

        private void CollectNamedValues(List<VFXNamedValue> values, SpaceRef spaceRef, string fullName)
        {
            VFXPropertySlot refSlot = CurrentValueRef;
            if (refSlot.Value != null) // if not null it means value has a concrete type (not kNone)
                values.Add(new VFXNamedValue(AggregateName(fullName, Name), GetTransformedExpression(refSlot, spaceRef)));
            else foreach (var child in refSlot.m_Children) // Continue only until we found a value
                    child.CollectNamedValues(values, spaceRef, AggregateName(fullName, Name));
        }

        // Collect exposed named values
        // Always collect the deepest values. For instance for proxy vectors, it will collect the x,y and z component values
        public void CollectExposableNamedValues(List<VFXNamedValue> values, string fullName, bool skipFirstName = true) 
        {
            VFXPropertySlot refSlot = CurrentValueRef;
            if (refSlot.GetNbChildren() > 0)
                foreach (var child in refSlot.m_Children) // Continue only until we found a value
                    child.CollectExposableNamedValues(values, skipFirstName ? fullName : AggregateName(fullName, Name), false);
            else
                values.Add(new VFXNamedValue(skipFirstName ? fullName : AggregateName(fullName, Name), refSlot.Value));
        }

        public void CollectExpressions(HashSet<VFXExpression> expressions, SpaceRef spaceRef = SpaceRef.kNone)
        {
            VFXPropertySlot refSlot = CurrentValueRef;
            if (refSlot.Value != null)
                expressions.Add(GetTransformedExpression(refSlot, spaceRef));
            else foreach (var child in refSlot.m_Children) // Continue only until we found a value
                    child.CollectExpressions(expressions, spaceRef); 
        }

        private static string AggregateName(string parent,string child)
        {
            return parent.Length == 0 ? child : parent + "_" + child;
        }

        private static VFXExpression GetTransformedExpression(VFXPropertySlot slot, SpaceRef spaceRef)
        {
            bool needsTransform = spaceRef != SpaceRef.kNone // We got a reference space
                && slot.TransformModeUsed // value can be transformed 
                && ((slot.WorldSpace && spaceRef == SpaceRef.kLocal)    // Needs a world to local transformation on value
                || (!slot.WorldSpace && spaceRef == SpaceRef.kWorld));  // Needs a local to world transformation on value

            return needsTransform ? slot.Semantics.GetTransformedExpression(slot, spaceRef) : slot.Value;
        }

        public void UpdatePosition(Vector2 position) {}
        public void UpdateCollapsed(bool collapsed)
        {
            m_UIChildrenCollapsed = collapsed;
        }

        public bool UICollapsed { get { return m_UIChildrenCollapsed; } }

        public abstract List<VFXPropertySlot> GetConnectedSlots();

        private VFXExpression m_OwnedValue;

        protected List<VFXPropertySlotObserver> m_Observers = new List<VFXPropertySlotObserver>();

        private VFXProperty m_Desc; // Contains semantic type and name for this value

        protected VFXPropertySlot m_Parent;
        protected VFXPropertySlot[] m_Children = new VFXPropertySlot[0];

        private bool m_UIChildrenCollapsed = true;

        public bool IsTransformWorld() 
        { 
            if ((m_TransformMode & SlotTransformMode.kInherited) == 0)
                return (m_TransformMode & SlotTransformMode.kWorld) != 0;
            else
                return CurrentValueRef.IsTransformWorld();
        }

        public bool WorldSpace
        {
            get { return (m_TransformMode & SlotTransformMode.kWorld) != 0; }
            set
            {
                if (TransformModeSettable && WorldSpace != value)
                    SetAndPropagateWorldSpace(value);
            }
        }

        private void SetAndPropagateWorldSpace(bool worldSpace)
        {
            m_TransformMode = worldSpace ? (m_TransformMode | SlotTransformMode.kWorld) : (m_TransformMode & ~SlotTransformMode.kWorld);
            if (TransformModeUsed)
                NotifyChange(Event.kTransformModeUpdated);
            foreach (var child in m_Children)
                child.SetAndPropagateWorldSpace(worldSpace);
        }

        public bool TransformModeInherited  { get { return (m_TransformMode & SlotTransformMode.kInherited) != 0; } }
        public bool TransformModeUsed       { get { return (m_TransformMode & SlotTransformMode.kUsed) != 0; } }

        // Used by UI for edition
        public bool TransformModeVisible    { get { return TransformModeUsed && !TransformModeInherited; } }
        public bool TransformModeSettable   { get { return TransformModeVisible && (IsValueUsed() || !CurrentValueRef.TransformModeUsed); } } // TODO

        protected SlotTransformMode m_TransformMode;
    }

    // Concrete implementation for input slot (can be linked to only one output slot)
    public class VFXInputSlot : VFXPropertySlot
    {
        public VFXInputSlot() {}
        public VFXInputSlot(VFXProperty desc)
        {
            Init<VFXInputSlot>(null, desc);  
        }

        public bool Link(VFXOutputSlot slot)
        {
            if (slot != m_ConnectedSlot)
            {
                if (slot != null && !Semantics.CanLink(slot.Semantics))
                    throw new ArgumentException();

                if (m_ConnectedSlot != null)
                    m_ConnectedSlot.InnerRemoveOutputLink(this);

                m_ConnectedSlot = slot;
                VFXPropertySlot old = m_ValueRef;
                m_ValueRef = m_ConnectedSlot;
      
                if (m_ValueRef != old)
                {
                    if (slot != null)
                        slot.InnerAddOutputLink(this);
                    NotifyChange(Event.kLinkUpdated);

                    if (slot != null)
                        foreach (var child in m_Children)
                            child.UnlinkRecursively();

                    return true;
                }
            }

            return false;
        }

        public new VFXInputSlot GetChild(int index)
        {
            return (VFXInputSlot)m_Children[index];
        }

        public new VFXInputSlot Parent
        {
            get { return (VFXInputSlot)m_Parent; }
        }

        public override bool IsLinked()
        {
            return m_ConnectedSlot != null;
        }

        public override List<VFXPropertySlot> GetConnectedSlots()
        {
            List<VFXPropertySlot> connected = new List<VFXPropertySlot>();
            if (IsLinked())
                connected.Add(m_ConnectedSlot);
            return connected;
        }

        public void Unlink()
        {
            Link(null);
        }

        public override void UnlinkAll()
        {
            Unlink();
        }

        public override VFXPropertySlot CurrentValueRef
        {
            get { return m_ValueRef ?? this; }
        }
      
        private VFXPropertySlot m_ValueRef;
        private VFXOutputSlot m_ConnectedSlot;
    }

    // Concrete implementation for output slot (can be linked to several input slot)
    public class VFXOutputSlot : VFXPropertySlot
    {
        public VFXOutputSlot() {}
        public VFXOutputSlot(VFXProperty desc)
        {
            Init<VFXOutputSlot>(null, desc); 
        }

        public override void PropagateChange(VFXPropertySlot.Event type)
        {
            foreach (var slot in m_ConnectedSlots)
                slot.NotifyChange(type);
        }

        // Called internally only!
        internal void InnerAddOutputLink(VFXInputSlot slot)
        {
            // Do we need to notify the output when a new slot is linked ?
            m_ConnectedSlots.Add(slot);
        }

        internal void InnerRemoveOutputLink(VFXInputSlot slot)
        {
            // Do we need to notify the output when a new slot is linked ?
            m_ConnectedSlots.Remove(slot);
        }

        public override VFXPropertySlot CurrentValueRef
        {
            get { return this; }
        }

        public new VFXOutputSlot GetChild(int index)
        {
            return (VFXOutputSlot)m_Children[index];
        }

        public new VFXOutputSlot Parent
        {
            get { return (VFXOutputSlot)m_Parent; }
        }

        public override bool IsLinked()
        {
            return m_ConnectedSlots.Count > 0;
        }

        public override List<VFXPropertySlot> GetConnectedSlots()
        {
            List<VFXPropertySlot> connected = new List<VFXPropertySlot>();
            foreach (var slot in m_ConnectedSlots)
                connected.Add(slot);
            return connected;
        }

        public void Link(VFXInputSlot slot)
        {
            if (slot == null)
                return;

            slot.Link(this); // This will call InnerUpdateOutputLink if needed
        }

        public void Unlink(VFXInputSlot slot)
        {
            if (slot == null)
                return;

            slot.Unlink();
        }

        public override void UnlinkAll()
        {
            foreach (var slot in m_ConnectedSlots.ToArray()) // must copy the list else we'll get an issue with iterator invalidation
                slot.Unlink();
            m_ConnectedSlots.Clear();
        }

        private List<VFXInputSlot> m_ConnectedSlots = new List<VFXInputSlot>();
    }
}
