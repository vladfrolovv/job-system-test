using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Utils;
using Utils.Extensions;
using Object = UnityEngine.Object;

namespace Editor.Utils
{
    public static class EditorGUIUtils
    {
        /// <summary>
        ///     Draws object field that can create new object of specified type.
        /// </summary>
        public static void CreatablePropertyField(
            SerializedProperty property,
            Func<Type, Object> createFunc,
            Action<Object> destructFunc,
            bool showsInspector,
            FoldableEditorState state)
        {
            Object value = property.objectReferenceValue;

            if (value != null)
            {
                EditorGUILayout.BeginHorizontal();

                if (showsInspector)
                {
                    GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);

                    state._isFoldedOut = EditorGUILayout.Foldout(
                        state._isFoldedOut,
                        new GUIContent(property.displayName),
                        true,
                        foldoutStyle
                    );
                    GUILayout.Space(-4f);
                }

                EditorGUILayout.PropertyField(property, GUIContent.none, true);

                EditorGUILayout.EndHorizontal();

                // Property is cleared or another object is dragged into
                if (property.objectReferenceValue != value)
                {
                    Undo.RecordObjects(property.serializedObject.targetObjects, "Delete Object");

                    foreach (Object target in property.serializedObject.targetObjects)
                    {
                        destructFunc(ReflectionUtils.FindField(target, property.name).GetValue(target) as Object);
                    }

                    state._editor = null;
                    state._isFoldedOut = false;
                }

                if (state._isFoldedOut)
                {
                    EditorGUI.indentLevel++;

                    var objects = new List<Object>();
                    foreach (Object target in property.serializedObject.targetObjects)
                    {
                        Object obj = new SerializedObject(target).FindProperty(property.propertyPath)
                            .objectReferenceValue;
                        if (obj != null && obj.GetType() == value.GetType())
                        {
                            objects.Add(obj);
                        }
                    }

                    if (state._editor == null)
                    {
                        if (objects.Count > 1)
                        {
                            state._editor = UnityEditor.Editor.CreateEditor(objects.ToArray());
                        }
                        else
                        {
                            state._editor = UnityEditor.Editor.CreateEditor(objects[0]);
                        }
                    }

                    state._editor.OnInspectorGUI();

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                state._isFoldedOut = false;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(property, true);

                GUILayout.Space(-4f);

                Type propertyType = ReflectionUtils.FindField(property.serializedObject.targetObject, property.name)
                    .FieldType;

                Action<Type> creationDelegate = t =>
                {
                    Undo.RecordObjects(property.serializedObject.targetObjects, "Create Object");

                    state._isFoldedOut = true;

                    foreach (Object target in property.serializedObject.targetObjects)
                    {
                        Object obj = createFunc(t);

                        // Setting property.objectReferenceValue doesn't work for some reason, so using reflection
                        ReflectionUtils.FindField(target, property.name).SetValue(target, obj);
                    }
                };

                ObjectCreationButton(
                    propertyType,
                    creationDelegate,
                    new GUIContent("+"),
                    EditorStyles.label,
                    GUILayout.ExpandWidth(false));

                EditorGUILayout.EndHorizontal();
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        public static void ObjectCreationButton(
            Type baseType,
            Action<Type> callback,
            GUIContent content,
            GUIStyle style,
            params GUILayoutOption[] options)
        {
            if (GUILayout.Button(content, style, options))
            {
                IEnumerable<Type> types = from t in baseType.Assembly.GetTypes()
                    where !t.IsAbstract && (t == baseType || t.IsSubclassOf(baseType))
                    select t;

                GenericMenu menu = new GenericMenu();

                foreach (Type type in types)
                {
                    menu.AddItem(new GUIContent(type.Name), false, t => callback(t as Type), type);
                }

                menu.ShowAsContext();
            }
        }

        public static void CreatableScriptableObjectPropertyField(
            SerializedProperty property,
            bool showsInspector,
            FoldableEditorState state)
        {
            CreatablePropertyField(
                property,
                type => AssetDatabaseUtils.CreateSubAsset(
                    $"{property.serializedObject.targetObject.name}.{type.Name}",
                    type,
                    property.serializedObject.targetObject),
                AssetDatabaseUtils.DestroyAsset,
                showsInspector,
                state
            );
        }

        public static void CreatableScriptableObjectList(
            SerializedProperty objects,
            List<FoldableEditorState> editorStates)
        {
            Assert.IsTrue(objects.isArray);

            int objectsCount = objects.arraySize;
            int statesCount = editorStates.Count;
            int difference = objectsCount - statesCount;

            if (difference > 0)
            {
                for (int i = 0; i < difference; ++i)
                {
                    Object obj = objects.GetArrayElementAtIndex(statesCount + i).GetValue() as Object;
                    editorStates.Add(
                        new FoldableEditorState
                        {
                            _isFoldedOut = true,
                            _editor = UnityEditor.Editor.CreateEditor(obj)
                        }
                    );
                }
            }
            else if (difference < 0)
            {
                editorStates.RemoveRange(objectsCount, -difference);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(objects.displayName);

            ObjectCreationButton(
                objects.GetArrayElementValueType(),
                t =>
                {
                    Object obj = AssetDatabaseUtils.CreateSubAsset(
                        $"{t.Name}",
                        t,
                        objects.serializedObject.targetObject);
                    objects.arraySize++;
                    objects.GetArrayElementAtIndex(objects.arraySize - 1).objectReferenceValue = obj;
                    objects.serializedObject.ApplyModifiedProperties();
                },
                new GUIContent("Add"),
                GUI.skin.button
            );
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            for (int i = 0; i < editorStates.Count; ++i)
            {
                FoldableEditorState state = editorStates[i];
                SerializedProperty valueProperty = objects.GetArrayElementAtIndex(i);
                Object obj = valueProperty.GetValue() as Object;

                EditorGUILayout.BeginHorizontal();
                state._isFoldedOut = EditorGUILayout.Foldout(state._isFoldedOut, obj.name, true);

                if (GUILayout.Button("x", EditorStyles.label, GUILayout.ExpandWidth(false)))
                {
                    Object.DestroyImmediate(obj, true);
                    AssetDatabase.SaveAssets();
                    objects.DeleteArrayElementAtIndexTotally(i);
                    editorStates.RemoveAt(i);
                    --i;
                    objects.serializedObject.ApplyModifiedPropertiesWithDirtyFlag();
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                if (state._isFoldedOut)
                {
                    EditorGUI.indentLevel++;
                    state._editor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        public static Rect SubstractSingleLineRect(ref Rect position)
        {
            return SubstractSingleLineRect(ref position, EditorGUIUtility.standardVerticalSpacing);
        }

        public static Rect SubstractSingleLineRect(ref Rect position, float verticalSpacing)
        {
            return SubstractRect(ref position, verticalSpacing, EditorGUIUtility.singleLineHeight);
        }

        public static Rect SubstractRect(ref Rect position, float height)
        {
            return SubstractRect(ref position, EditorGUIUtility.standardVerticalSpacing, height);
        }

        public static Rect SubstractRect(ref Rect position, float verticalSpacing, float height)
        {
            Rect r = new Rect(position.x, position.y, position.width, height);
            position = new Rect(
                position.x,
                r.yMax + verticalSpacing,
                position.width,
                position.height - height - verticalSpacing);
            return r;
        }

        public static string FolderPathField(string label, string value, string basePath = "")
        {
            return PathField(
                label,
                value,
                Directory.Exists,
                (caption, folder) => EditorUtility.OpenFolderPanel(caption, folder, value),
                basePath
            );
        }

        public static string FilePathField(string label, string value, string extension, string basePath = "")
        {
            return PathField(
                label,
                value,
                File.Exists,
                (caption, folder) => EditorUtility.OpenFilePanel(caption, folder, extension),
                basePath
            );
        }

        private static string PathField(
            string label,
            string value,
            Func<string, bool> validator,
            Func<string, string, string> selector,
            string basePath)
        {
            EditorGUILayout.BeginHorizontal();

            try
            {
                string newValue = EditorGUILayout.DelayedTextField(label, value);
                if (newValue != value)
                {
                    string path = !string.IsNullOrEmpty(basePath) ? Path.Combine(basePath, newValue) : newValue;

                    if (validator(path))
                    {
                        return newValue;
                    }
                    else
                    {
                        Debug.LogError($"File {path} is not found.");
                        return value;
                    }
                }

                if (GUILayout.Button("Reveal", GUILayout.ExpandWidth(false)))
                {
                    string path = !string.IsNullOrEmpty(basePath) ? Path.Combine(basePath, value) : value;
                    EditorUtility.RevealInFinder(path);
                }

                if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
                {
                    string file = selector(
                        "Browse " + label,
                        string.IsNullOrEmpty(value) ? "." : Path.GetDirectoryName(value)
                    );

                    return !string.IsNullOrEmpty(file) ? FileUtils.GetRelativePath(file, basePath) : value;
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            return value;
        }

        #region Inner Classes

        [Serializable]
        public class FoldableEditorState
        {
            public bool _isFoldedOut;
            public UnityEditor.Editor _editor;
        }

        #endregion
    }
}
