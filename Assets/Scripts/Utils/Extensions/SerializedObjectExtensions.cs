#region

using UnityEditor;
using UnityEngine;

#endregion

namespace Utils.Extensions
{
    public static class SerializedObjectExtensions
    {
#if UNITY_EDITOR
        public static void ApplyModifiedPropertiesWithDirtyFlag(this SerializedObject self)
        {
            self.ApplyModifiedProperties();

            // SetDirty
            if (self.isEditingMultipleObjects)
            {
                foreach (Object o in self.targetObjects)
                {
                    EditorUtility.SetDirty(o);
                }
            }
            else
            {
                EditorUtility.SetDirty(self.targetObject);
            }
        }
#endif
    }
}
