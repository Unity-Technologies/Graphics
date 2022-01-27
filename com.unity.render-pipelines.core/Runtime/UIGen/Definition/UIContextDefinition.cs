using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public class UIContextDefinition : ITakeAndMerge<UIContextDefinition>
    {
        public readonly struct Member
        {
            public readonly Type type;
            public readonly string name;

            public Member(Type type, string name)
            {
                this.type = type;
                this.name = name;
            }
        }

        public List<Member> members { get; } = new List<Member>();

        [MustUseReturnValue]
        public bool AddMember(
            [DisallowNull] Type type,
            [DisallowNull] string name,
            [NotNullWhen(false)] out Exception error
        )
        {
            members.Add(new Member(type, name));
            error = default;
            return true;
        }

        public bool TakeAndMerge(UIContextDefinition input, out Exception error)
        {
            members.AddRange(input.members);
            input.members.Clear();
            error = default;
            return true;
        }
    }
}
