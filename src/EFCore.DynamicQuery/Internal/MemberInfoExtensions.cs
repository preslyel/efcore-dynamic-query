using System.Reflection;

namespace EFCore.DynamicQuery.Internal;

internal static class MemberInfoExtensions
{
    public static object? GetMemberValue(this MemberInfo member, object target) => member switch
    {
        PropertyInfo property => property.GetValue(target),
        FieldInfo field => field.GetValue(target),
        _ => throw new NotSupportedException($"Unsupported member type '{member.MemberType}' on '{member.Name}'.")
    };

    public static void SetMemberValue(this MemberInfo member, object target, object? value)
    {
        switch (member)
        {
            case PropertyInfo property:
                property.SetValue(target, value);
                break;
            case FieldInfo field:
                field.SetValue(target, value);
                break;
            default:
                throw new NotSupportedException($"Unsupported member type '{member.MemberType}' on '{member.Name}'.");
        }
    }
}
