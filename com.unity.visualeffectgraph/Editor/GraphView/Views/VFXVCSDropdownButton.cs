using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXVCSDropdownButton : DropDownButtonBase
    {
        static readonly Dictionary<Asset.States, Texture2D> s_StatusToTexture = new();
        static readonly Dictionary<Asset.States, string> s_StatusToPathMap = new()
        {
            { Asset.States.OutOfSync, "VersionControl/P4_OutOfSync.png" },
            { Asset.States.CheckedOutLocal, "VersionControl/P4_CheckOutLocal.png" },
            { Asset.States.CheckedOutRemote, "VersionControl/P4_CheckOutRemote.png" },
            { Asset.States.AddedLocal, "VersionControl/P4_AddedLocal.png" },
        };

        static readonly Dictionary<Asset.States, string> s_StatusToTooltip = new()
        {
            { Asset.States.OutOfSync, "Click to check out" },
            { Asset.States.CheckedOutLocal, "Already checked out" },
            { Asset.States.CheckedOutRemote, "Checked out remotely" },
            { Asset.States.AddedLocal, "Added locally" },
        };

        const string MainButtonName = "checkout";

        readonly Button m_RevertButton;
        readonly Button m_SubmitButton;
        readonly Button m_GetLatestButton;

        Image m_VCSStatusImage;

        public VFXVCSDropdownButton(VFXView vfxView)
            : base(
                vfxView,
                "VFXVCSDropDownPanel",
                "Checkout",
                "checkout",
                EditorResources.iconsPath + "UnityEditor.VersionControl.png",
                true,
                false)
        {
            m_RevertButton = m_PopupContent.Q<Button>("revert");
            m_RevertButton.clicked += OnRevert;

            m_SubmitButton = m_PopupContent.Q<Button>("submit");
            m_SubmitButton.clicked += OnSubmit;

            m_GetLatestButton = m_PopupContent.Q<Button>("getlatest");
            m_GetLatestButton.clicked += OnGetLatest;
        }

        public void SetStatus(Asset.States state)
        {
            if (state == Asset.States.None)
            {
                if (m_VCSStatusImage != null)
                {
                    m_VCSStatusImage.image = null;
                }
                m_GetLatestButton.SetEnabled(false);
                m_RevertButton.SetEnabled(false);
                m_SubmitButton.SetEnabled(false);

                return;
            }

            CreateVCSImageIfNeeded();

            state &= ~(Asset.States.Local | Asset.States.Synced | Asset.States.Missing | Asset.States.ReadOnly);

            var kvp = s_StatusToPathMap.FirstOrDefault(x => state.HasFlag(x.Key));

            if (!s_StatusToTexture.TryGetValue(kvp.Key, out var image))
            {
                if (string.IsNullOrEmpty(kvp.Value))
                {
                    image = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + s_StatusToPathMap[Asset.States.OutOfSync]);
                }
                else
                {
                    image = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + kvp.Value);
                    s_StatusToTexture[kvp.Key] = image;
                }
            }

            m_GetLatestButton.SetEnabled(state.HasFlag(Asset.States.OutOfSync));
            m_VCSStatusImage.image = image;
            m_VCSStatusImage.parent.tooltip = s_StatusToTooltip.GetValueOrDefault(state) ?? s_StatusToTooltip[Asset.States.OutOfSync];

            var isEditable = m_VFXView.IsAssetEditable() && !m_VFXView.IsOffline();
            m_RevertButton.SetEnabled(isEditable);
            m_SubmitButton.SetEnabled((state & (Asset.States.CheckedOutLocal | Asset.States.AddedLocal)) != 0);
        }

        protected override Vector2 GetPopupSize() => new Vector2(150, 76);

        protected override void OnMainButton()
        {
            m_VFXView.Checkout();
        }

        private void OnRevert()
        {
            m_VFXView.Revert();
            ClosePopup();
        }

        private void OnSubmit()
        {
            m_VFXView.Submit();
        }

        private void OnGetLatest()
        {
            m_VFXView.GetLatest();
        }

        private void CreateVCSImageIfNeeded()
        {
            if (m_VCSStatusImage == null)
            {
                m_VCSStatusImage = new Image();
                m_VCSStatusImage.style.width = 12;
                m_VCSStatusImage.style.height = 12;
                m_VCSStatusImage.style.position = UnityEngine.UIElements.Position.Absolute;
                m_VCSStatusImage.style.top = 5;
                m_VCSStatusImage.style.left = 7;
                m_VCSStatusImage.style.marginTop = 2;
                m_VCSStatusImage.style.marginLeft = 2;
                m_VCSStatusImage.style.alignSelf = Align.Center;

                var mainButton = this.Q<Button>(MainButtonName);
                mainButton.Insert(0, m_VCSStatusImage);
            }
        }
    }
}
