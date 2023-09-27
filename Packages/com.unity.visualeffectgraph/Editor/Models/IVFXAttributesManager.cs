using System.Collections.Generic;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX
{
    internal enum RenameStatus
    {
        Success,
        InvalidName,
        NameUsed,
        NotFound,
    }

    internal interface IVFXAttributesManager
    {
        IEnumerable<VFXAttribute> GetAllAttributesOrCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly);
        IEnumerable<VFXAttribute> GetAllAttributesAndCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly);
        IEnumerable<string> GetAllNamesOrCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly);
        IEnumerable<string> GetAllNamesAndCombination(bool includeVariadic, bool includeVariadicComponents, bool includeReadOnly, bool includeWriteOnly);

        bool TryFind(string name, out VFXAttribute attribute);
        bool TryFindWithMode(string name, VFXAttributeMode mode, out VFXAttribute attribute);
        bool Exist(string name);
        bool TryUpdate(string name, CustomAttributeUtility.Signature type, string description);
        bool IsCustom(string name);
        bool TryRegisterCustomAttribute(string name, CustomAttributeUtility.Signature type, string description, out VFXAttribute newAttribute);
        void UnregisterCustomAttribute(string name);
        RenameStatus TryRename(string oldName, string newName);
        VFXAttribute Duplicate(string name);
    }
}
