#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Utils;

#endregion

namespace Editor.Extensions
{
    public static class SerializationExtensions
    {
        /// <summary>
        ///     Sets the value of the property to default
        /// </summary>
        public static void SetDefaultValue(this SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = 0f;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.white;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = -1;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = 0;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.arraySize = 0;
                    break;
                case SerializedPropertyType.Character:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = null;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = default;
                    break;
                case SerializedPropertyType.Gradient:
                    SetGradientValue(property, new Gradient());
                    break;
            }
        }


        public static bool EqualsTo(this SerializedProperty self, SerializedProperty other)
        {
            if (self.propertyType != other.propertyType)
            {
                return false;
            }

            switch (self.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return self.intValue == other.intValue;

                case SerializedPropertyType.Boolean:
                    return self.boolValue == other.boolValue;

                case SerializedPropertyType.Float:
                    return Math.Abs(self.floatValue - other.floatValue) < float.Epsilon;

                case SerializedPropertyType.String:
                    return self.stringValue == other.stringValue;

                case SerializedPropertyType.Color:
                    return self.colorValue == other.colorValue;

                case SerializedPropertyType.ObjectReference:
                    return self.objectReferenceValue == other.objectReferenceValue;

                case SerializedPropertyType.LayerMask:
                    return self.intValue == other.intValue;

                case SerializedPropertyType.Enum:
                    return self.enumValueIndex == other.enumValueIndex;

                case SerializedPropertyType.Vector2:
                    return self.vector2Value == other.vector2Value;

                case SerializedPropertyType.Vector3:
                    return self.vector3Value == other.vector3Value;

                case SerializedPropertyType.Vector4:
                    return self.vector4Value == other.vector4Value;

                case SerializedPropertyType.Rect:
                    return self.rectValue == other.rectValue;

                case SerializedPropertyType.ArraySize:
                    return self.arraySize == other.arraySize;

                case SerializedPropertyType.Character:
                    return self.intValue == other.intValue;

                case SerializedPropertyType.AnimationCurve:
                    return self.animationCurveValue.Equals(other.animationCurveValue);

                case SerializedPropertyType.Bounds:
                    return self.boundsValue == other.boundsValue;

                case SerializedPropertyType.Gradient:
                    return GetGradientValue(self).Equals(GetGradientValue(other));
            }

            return false;
        }


        public static object GetValue(this SerializedProperty self)
        {
            string[] fields = GetFieldsPath(self);

            return GetValue(self.serializedObject, fields);
        }


        public static object GetParentValue(this SerializedProperty self)
        {
            string[] fields = GetFieldsPath(self);

            if (fields.Length > 0)
            {
                Array.Resize(ref fields, fields.Length - 1);
            }

            return GetValue(self.serializedObject, fields);
        }


        public static Type GetValueType(this SerializedProperty self)
        {
            string[] fields = GetFieldsPath(self);

            Type value = self.serializedObject.targetObject.GetType();

            foreach (string element in fields)
            {
                if (element.Contains("["))
                {
                    string fieldName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    value = ReflectionUtils.FindField(value, fieldName).FieldType.GetElementType();
                }
                else
                {
                    value = ReflectionUtils.FindField(value, element).FieldType;
                }
            }

            return value;
        }


        public static Type GetArrayElementValueType(this SerializedProperty self)
        {
            Assert.IsTrue(self.isArray);

            Type selfType = self.GetValueType();
            if (selfType.IsArray)
            {
                return selfType.GetElementType();
            }

            return selfType.GetGenericTypeDefinition() == typeof(List<>) ? selfType.GetGenericArguments()[0] : null;
        }


        public static void DeleteArrayElementAtIndexTotally(this SerializedProperty self, int index)
        {
            Assert.IsTrue(self.isArray);

            SerializedProperty element = self.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference)
            {
                element.objectReferenceValue = null;
            }

            self.DeleteArrayElementAtIndex(index);
        }


        public static SerializedProperty GetParent(this SerializedProperty self)
        {
            char dirSeparator = Path.DirectorySeparatorChar;
            string parentPath = Path
                .GetDirectoryName(self.propertyPath.Replace('.', dirSeparator))
                ?.Replace(dirSeparator, '.');
            return self.serializedObject.FindProperty(parentPath);
        }


        private static Gradient GetGradientValue(SerializedProperty property)
        {
            BindingFlags instanceAnyPrivacyBindingFlags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
                "gradientValue",
                instanceAnyPrivacyBindingFlags,
                null,
                typeof(Gradient),
                Type.EmptyTypes,
                null
            );
            if (propertyInfo == null)
            {
                return null;
            }

            Gradient gradientValue = propertyInfo.GetValue(property, null) as Gradient;
            return gradientValue;
        }


        private static void SetGradientValue(SerializedProperty property, Gradient value)
        {
            BindingFlags instanceAnyPrivacyBindingFlags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
                "gradientValue",
                instanceAnyPrivacyBindingFlags,
                null,
                typeof(Gradient),
                Type.EmptyTypes,
                null
            );

            Assert.IsNotNull(
                propertyInfo,
                "Can't set gradientValue. Probably something was changed in the current Unity version.");
            if (propertyInfo == null)
            {
                return;
            }

            propertyInfo.SetValue(property, value);
        }


        private static object GetValue(SerializedObject serializedObject, string[] fields)
        {
            object value = serializedObject.targetObject;

            foreach (string element in fields)
            {
                if (element.Contains("["))
                {
                    string fieldName = element[..element.IndexOf("[", StringComparison.Ordinal)];
                    int index = Convert.ToInt32(
                        element.Substring(element.IndexOf("[", StringComparison.Ordinal))
                            .Replace("[", "")
                            .Replace("]", ""));
                    value = ReflectionUtils.GetArrayValueAt(value, fieldName, index);
                }
                else
                {
                    value = ReflectionUtils.GetValue(value, element);
                }
            }

            return value;
        }


        private static string[] GetFieldsPath(SerializedProperty property)
        {
            return property.propertyPath.Replace(".Array.data[", "[").Split('.');
        }
    }
}
