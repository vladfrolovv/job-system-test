#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine;

#endregion

namespace Utils
{
    public static class ReflectionUtils
    {
        /// <summary>
        ///     Sets the value into the private or protected field marked with SerializedField attribute
        /// </summary>
        public static void SetSerializedField(this object obj, string field, object value)
        {
            Type type = obj.GetType();
            while (type != null)
            {
                FieldInfo fieldInfo = type.GetField(
                    field,
                    BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic
                );

                if (fieldInfo != null && fieldInfo.GetCustomAttribute<SerializeField>() != null)
                {
                    fieldInfo.SetValue(obj, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException(
                $"The field '{field}' of the object {obj} doesn't exist or doesn't have {nameof(SerializeField)} attribute.");
        }


        /// <summary>
        ///     Invokes non-public static method returning a value using reflection
        /// </summary>
        public static TResult StaticInvoke<TResult, TClass>(string methodName, bool silent, params object[] parameters)
        {
            return InvokeMethod<TResult>(
                typeof(TClass),
                null,
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static,
                silent,
                parameters);
        }


        /// <summary>
        ///     Invokes non-public static method not returning a value using reflection
        /// </summary>
        public static void StaticInvoke<TClass>(string methodName, bool silent, params object[] parameters)
        {
            InvokeMethod<object>(
                typeof(TClass),
                null,
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static,
                silent,
                parameters);
        }


        /// <summary>
        ///     Invokes non-public static method returning a value using reflection
        /// </summary>
        public static TResult StaticInvoke<TResult>(
            Type calleeType,
            string methodName,
            bool silent,
            params object[] parameters)
        {
            return InvokeMethod<TResult>(
                calleeType,
                null,
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static,
                silent,
                parameters);
        }


        /// <summary>
        ///     Invokes non-public static method not returning a value using reflection
        /// </summary>
        public static void StaticInvoke(Type calleeType, string methodName, bool silent, params object[] parameters)
        {
            InvokeMethod<object>(
                calleeType,
                null,
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static,
                silent,
                parameters);
        }


        /// <summary>
        ///     Invokes non-public method returning a value using reflection
        /// </summary>
        public static TResult Invoke<TResult>(object callee, string methodName, bool silent, params object[] parameters)
        {
            return InvokeMethod<TResult>(
                callee.GetType(),
                callee,
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance,
                silent,
                parameters);
        }


        /// <summary>
        ///     Invokes public method not returning a value using reflection
        /// </summary>
        public static void Invoke(object callee, string methodName, bool silent, params object[] parameters)
        {
            InvokeMethod<object>(
                callee.GetType(),
                callee,
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance,
                silent,
                parameters);
        }


        /// <summary>
        ///     Invokes method not returning a value using reflection and contrete type
        /// </summary>
        public static void Invoke(Type calleeType, object callee, string methodName, bool silent, params object[] parameters)
        {
            InvokeMethod<object>(
                calleeType,
                callee,
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance,
                silent,
                parameters);
        }


        public static FieldInfo FindField(object target, string fieldName)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "The assignment target cannot be null.");
            }

            return FindField(target.GetType(), fieldName);
        }


        public static FieldInfo FindField(Type t, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(nameof(fieldName), "The field name cannot be null or empty.");
            }

            FieldInfo fi = null;

            while (t != null)
            {
                fi = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fi != null)
                {
                    break;
                }

                t = t.BaseType;
            }

            if (fi == null)
            {
                throw new Exception($"Field '{fieldName}' not found in type hierarchy.");
            }

            return fi;
        }


        public static object GetValue(object source, string fieldName)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.GetType();
            FieldInfo fi = FindField(type, fieldName);
            if (fi == null)
            {
                throw new Exception($"Field '{fieldName}' not found in type '{type.Name}'.");
            }

            return fi.GetValue(source);
        }


        public static T GetPropertyValue<T>(object source, string propertyName)
        {
            return (T)source.GetType().GetProperty(propertyName).GetValue(source);
        }


        public static object GetArrayValueAt(object source, string fieldName, int index)
        {
            object value = GetValue(source, fieldName);
            if (value == null)
            {
                return null;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                throw new ArgumentException($"Field '{fieldName}' in the object '{source}' is not IEnumerable.");
            }

            IEnumerator enm = enumerable.GetEnumerator();
            int i = index;
            while (i >= 0 && enm.MoveNext())
            {
                i--;
            }

            if (i < 0)
            {
                return enm.Current;
            }

            return null;
        }


        public static Type FindParent(this Type self, Type parentType)
        {
            Type baseType = self.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == parentType.Name)
                {
                    return baseType;
                }

                baseType = baseType.BaseType;
            }

            return null;
        }


        public static IEnumerable<Type> EnumerateTypeHierarchy(this Type self)
        {
            var types = new HashSet<Type>
            {
                self
            };

            types.UnionWith(self.GetInterfaces());

            foreach (Type parentType in self.GetParentTypes())
            {
                types.Add(parentType);
                types.UnionWith(parentType.GetInterfaces());
            }

            return types;
        }


        /// <summary>
        ///     Adds event handler.
        ///     Uses invocation of add method instead of _onValueChanged.AddEventHandler(),
        ///     because the latter doesn't work with AOT compilation
        /// </summary>
        public static void AddEventHandlerAOT(this EventInfo self, object target, Delegate handler)
        {
            MethodInfo addMethod = self.GetAddMethod();
            addMethod.Invoke(
                target,
                new object[]
                {
                    handler
                });
        }


        /// <summary>
        ///     Removes event handler.
        ///     Uses invocation of remove method instead of _onValueChanged.RemoveEventHandler(),
        ///     because the latter doesn't work with AOT compilation
        /// </summary>
        public static void RemoveEventHandlerAOT(this EventInfo self, object target, Delegate handler)
        {
            MethodInfo removeMethod = self.GetRemoveMethod();
            removeMethod.Invoke(
                target,
                new object[]
                {
                    handler
                });
        }


        public static Type FindTypeInAssemblies(string typeName, string nameSpace)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types = assemblies[i].GetTypes();
                for (int n = 0; n < types.Length; n++)
                {
                    if (typeName == types[n].Name && nameSpace == types[n].Namespace)
                    {
                        return types[n];
                    }
                }
            }

            return null;
        }


        public static UnityEngine.Object[] FindObjectsOfTypeByName(string typeName, string nameSpace)
        {
            Type type = FindTypeInAssemblies(typeName, nameSpace);
            if (type == null)
            {
                return null;
            }

            return UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None);
        }


        public static UnityEngine.Object FindObjectOfTypeByName(string typeName, string nameSpace)
        {
            Type type = FindTypeInAssemblies(typeName, nameSpace);
            if (type == null)
            {
                return null;
            }

            return UnityEngine.Object.FindFirstObjectByType(type);
        }


        public static IEnumerable<Type> GetParentTypes(this Type type)
        {
            if (type == null || type.BaseType == null || type == typeof(object) || type.BaseType == typeof(object))
            {
                yield break;
            }

            yield return type.BaseType;

            foreach (Type ancestor in type.BaseType.GetParentTypes())
            {
                yield return ancestor;
            }
        }


        public static bool IsSubclassOrEqual<TBase, TSubclass>()
        {
            Type subclassType = typeof(TSubclass);
            Type baseType = typeof(TBase);
            return subclassType == baseType || subclassType.IsSubclassOf(baseType);
        }


        private static TResult InvokeMethod<TResult>(
            Type type,
            object callee,
            string methodName,
            BindingFlags flags,
            bool silent,
            object[] parameters)
        {
            Type[] types = parameters.Select(parameter => parameter.GetType()).ToArray();

            MethodInfo method = type.GetMethod(methodName, flags | BindingFlags.Public, null, types, null);
            if (method != null)
            {
                try
                {
                    return (TResult)method.Invoke(callee, parameters);
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException != null)
                    {
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }

                    throw;
                }
            }
            else
            {
                if (silent)
                {
                    return default(TResult);
                }
                else
                {
                    throw new ArgumentException(string.Format("Method '{0}' doesn't exist", methodName), "methodName");
                }
            }
        }
    }
}
