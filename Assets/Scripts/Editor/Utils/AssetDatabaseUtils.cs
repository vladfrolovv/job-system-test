#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace Editor.Utils
{
    public static class AssetDatabaseUtils
    {
        /// <summary>
        ///     Loads scriptable object based asset of type T in the specified folder or creates it in case it can't be found there.
        /// </summary>
        public static T LoadOrCreateAsset<T>(string folder) where T : ScriptableObject
        {
            return LoadOrCreateAsset(folder, typeof(T)) as T;
        }


        /// <summary>
        ///     Loads scriptable object based asset of the specified type in the specified folder or creates it in case it can't be found there.
        /// </summary>
        public static Object LoadOrCreateAsset(string folder, Type type)
        {
            string name = type.Name;
            string path = Path.Combine(folder, name + ".asset");
            Object result = AssetDatabase.LoadAssetAtPath(path, type);

            if (result == null)
            {
                result = ScriptableObject.CreateInstance(type);
                CreateFolders(folder);
                AssetDatabase.CreateAsset(result, path);
            }

            return result;
        }


        /// <summary>
        ///     Recursive folder creation function. Like <see cref="AssetDatabase.CreateFolder" />, but makes all intermediate-level directories needed to contain the leaf directory.
        /// </summary>
        public static void CreateFolders(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(folder);
                string appPath = new DirectoryInfo(Application.dataPath).Parent.FullName;
                string parentFolder = dirInfo.Parent.FullName.Replace(
                    appPath + Path.DirectorySeparatorChar,
                    string.Empty);

                if (parentFolder != string.Empty)
                {
                    CreateFolders(parentFolder);
                }

                AssetDatabase.CreateFolder(parentFolder, dirInfo.Name);
            }
        }


        /// <summary>
        ///     Creates new ScriptableObject asset inside specified object.
        /// </summary>
        public static T CreateSubAsset<T>(string name, Object parent) where T : ScriptableObject
        {
            return (T)CreateSubAsset(name, typeof(T), parent);
        }


        /// <summary>
        ///     Creates new ScriptableObject asset inside specified object.
        /// </summary>
        public static Object CreateSubAsset(string name, Type type, Object parent)
        {
            ScriptableObject subasset = ScriptableObject.CreateInstance(type);
            return AddSubAsset(name, subasset, parent);
        }


        public static Object AddSubAsset(string name, ScriptableObject subasset, Object parent)
        {
            subasset.name = name;

            AssetDatabase.AddObjectToAsset(subasset, parent);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(parent));
            return subasset;
        }


        public static void DestroyAsset(Object o)
        {
            string path = AssetDatabase.GetAssetPath(o);
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
            Undo.DestroyObjectImmediate(o);
            AssetDatabase.ImportAsset(path);
        }


        [CanBeNull]
        public static string GetAssetDirPath(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (!AssetDatabase.IsValidFolder(path))
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }


        public static T LoadAssetByGuid<T>(string guid) where T : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }


        [NotNull]
        public static AssetImporter GetImporter(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new InvalidOperationException($"Can't obtain importer at {path}.");
            }

            return importer;
        }


        /// <summary>
        ///     Enumerates assets / subassets of the specified type.
        /// </summary>
        public static IEnumerable<T> EnumerateAssets<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            foreach (string guid in guids.Distinct())
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object asset in assets)
                {
                    T result = asset as T;
                    if (result != null)
                    {
                        yield return result;
                    }
                }
            }
        }


        [CanBeNull]
        public static T FindAsset<T>(string name) where T : Object
        {
            string guid = AssetDatabase.FindAssets($"t:{typeof(T).Name} {name}").FirstOrDefault();
            return string.IsNullOrEmpty(guid) ? null : LoadAssetByGuid<T>(guid);
        }


        public static bool IsProjectAsset(string path)
        {
            return !path.StartsWith("Assets/Plugins/") &&
                   !path.StartsWith("Packages/");
        }
    }
}
