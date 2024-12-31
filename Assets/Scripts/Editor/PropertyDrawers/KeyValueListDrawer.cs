#region

using Editor.Extensions;
using Editor.Utils;
using UnityEditor;
using UnityEngine;
using Utils.Containers;

#endregion

namespace Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(DrawableKeyValueList), true)]
    public class KeyValueListDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                SerializedProperty keysProperty = property.FindPropertyRelative("serializedKeys");
                return (keysProperty.arraySize + 2) *
                       (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect r = EditorGUIUtils.SubstractSingleLineRect(ref position);
            property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, label);

            if (property.isExpanded)
            {
                SerializedProperty keysProperty = property.FindPropertyRelative("serializedKeys");
                SerializedProperty valuesProperty = property.FindPropertyRelative("serializedValues");

                bool uniqueKeysEnabled =
                    fieldInfo.GetCustomAttributes(typeof(NonUniqueKeysAttribute), true).Length == 0 ||
                    fieldInfo.FieldType.GetCustomAttributes(typeof(NonUniqueKeysAttribute), true).Length == 0;

                bool containsAllEnumKeysEnabled =
                    fieldInfo.GetCustomAttributes(typeof(ContainsAllEnumKeysAttribute), true).Length != 0 ||
                    fieldInfo.FieldType.GetCustomAttributes(typeof(ContainsAllEnumKeysAttribute), true).Length != 0;

                if (containsAllEnumKeysEnabled)
                {
                    if (keysProperty.arraySize != keysProperty.enumNames.Length)
                    {
                        bool isAdding = keysProperty.arraySize < keysProperty.enumNames.Length;
                        keysProperty.arraySize = keysProperty.enumNames.Length;
                        valuesProperty.arraySize = keysProperty.enumNames.Length;
                        for (int i = 0; i < keysProperty.enumNames.Length; ++i)
                        {
                            int elementIndex = keysProperty.arraySize - 1;
                            SerializedProperty enumProperty = keysProperty.GetArrayElementAtIndex(elementIndex);
                            enumProperty.enumValueIndex = i;

                            if (isAdding)
                            {
                                valuesProperty.GetArrayElementAtIndex(elementIndex).SetDefaultValue();
                            }
                        }
                    }
                }
                else
                {
                    r = EditorGUIUtils.SubstractSingleLineRect(ref position);
                    if (GUI.Button(new Rect(r.xMax - 60, r.y, 60, r.height), "Add"))
                    {
                        keysProperty.arraySize++;
                        keysProperty.GetArrayElementAtIndex(keysProperty.arraySize - 1).SetDefaultValue();

                        valuesProperty.arraySize++;
                    }
                }

                for (int i = 0; i < keysProperty.arraySize; i++)
                {
                    r = EditorGUIUtils.SubstractSingleLineRect(ref position);
                    float padding = 1;
                    float deleteButtonWidth = containsAllEnumKeysEnabled ? 0 : 16;
                    float halfWidth = (r.width - 2 * padding - deleteButtonWidth) / 2;

                    SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(i);
                    if (uniqueKeysEnabled)
                    {
                        for (int j = 0; j < keysProperty.arraySize; j++)
                        {
                            if (i != j && keyProperty.EqualsTo(keysProperty.GetArrayElementAtIndex(j)))
                            {
                                GUI.color = Color.red;
                                break;
                            }
                        }
                    }

                    EditorGUI.PropertyField(new Rect(r.x, r.y, halfWidth, r.height), keyProperty, GUIContent.none);

                    if (uniqueKeysEnabled)
                    {
                        GUI.color = Color.white;
                    }

                    EditorGUI.PropertyField(
                        new Rect(r.x + halfWidth + padding, r.y, halfWidth, r.height),
                        valuesProperty.GetArrayElementAtIndex(i),
                        GUIContent.none);
                    GUIStyle deleteButtonStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };

                    if (!containsAllEnumKeysEnabled)
                    {
                        if (GUI.Button(
                            new Rect(r.xMax - deleteButtonWidth, r.y, deleteButtonWidth, r.height),
                            "X",
                            deleteButtonStyle))
                        {
                            keysProperty.DeleteArrayElementAtIndexTotally(i);
                            valuesProperty.DeleteArrayElementAtIndexTotally(i);
                        }
                    }
                }
            }

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
