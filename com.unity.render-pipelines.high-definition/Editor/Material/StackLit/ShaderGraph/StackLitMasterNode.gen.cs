
using System;
using System.Linq;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using UnityEditor.Graphing.Util;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class StackLitMasterNode : IVersionable<StackLitMasterNode.Version>
    {
        enum Version
        {
            InitialVersion = 0,
        }
        [SerializeField] Version m_Version;
        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }
        static readonly MigrationDescription<Version, StackLitMasterNode> k_Migrations = new MigrationDescription<Version, StackLitMasterNode>(
            new MigrationStep<Version, StackLitMasterNode>(Version.InitialVersion, Migrations.InitialVersion)
        );
    }

    partial class StackLitMasterNode
    {
        #region Fields
        [SerializeField] bool m_ReceiveDecals = true;
        public bool receiveDecals
        {
            get => m_ReceiveDecals;
            set
            {
                if (m_ReceiveDecals == value) return;
        
                m_ReceiveDecals = value;
                Dirty(ModificationScope.Graph);
            }
        }
        #endregion
        #region Migration
        #endregion
    }

    partial class StackLitSubShader
    {
        static void SetReceiveDecalsField(StackLitMasterNode masterNode, ActiveFields.Base baseActiveFields)
        {
            if (!masterNode.receiveDecals)
                baseActiveFields.AddAll("DisableDecals");
        }
    }
}

namespace UnityEditor.Rendering.HighDefinition.Drawing
{
    partial class StackLitSettingsView
    {
        void AddReceiveDecalsField(PropertySheet ps, int indentLevel)
        {
            ps.Add(new PropertyRow(CreateLabel("Receive Decal", indentLevel)), (row) =>
            {
                row.Add(new Toggle(),
                    field =>
                    {
                        field.value = m_Node.receiveDecals;
                        field.RegisterValueChangedCallback(ChangeReceiveDecals);
                    });
            });
        }
        
        void ChangeReceiveDecals(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Support Decals Change");
            m_Node.receiveDecals = evt.newValue;
        }
    }
}
