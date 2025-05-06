using System.Reflection;

namespace JG.Inventory
{
    /// <summary>
    /// Helper to set private serialized fields using reflection.
    /// </summary>
    public static class ReflectionUtil
    {
        public static void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            FieldInfo fi = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            fi?.SetValue(obj, value);
        }
    }
}
