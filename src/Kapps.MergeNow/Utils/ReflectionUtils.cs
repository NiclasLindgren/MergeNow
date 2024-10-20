using EnvDTE;
using System;
using System.Reflection;

namespace MergeNow.Utils
{
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

    public static class ReflectionUtils
    {
        public static T GetProperty<T>(string propertyName, object parentObject)
            where T : class
        {
            var property = GetProperty(propertyName, parentObject);
            return property as T;
        }

        public static object GetProperty(string propertyName, object parentObject)
        {
            var propertyInfo = GetPropertyInfo(propertyName, parentObject);
            if (propertyInfo == null)
            {
                return null;
            }

            var property = propertyInfo.GetValue(parentObject);
            return property;
        }

        public static void SetProperty(string propertyName, object parentObject, object value)
        {
            var propertyInfo = GetPropertyInfo(propertyName, parentObject);
            if (propertyInfo == null)
            {
                return;
            }

            if (propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(parentObject, value);
            }
        }

        public static void InvokeMethod(string methodName, object parentObject, params object[] methodArguments)
        {
            var methodInfo = GetMethodInfo(methodName, parentObject);
            if (methodInfo == null)
            {
                return;
            }

            methodInfo.Invoke(parentObject, methodArguments);
        }

        public static Type GetNestedType(string typeName, Type parentType)
        {
            return parentType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static PropertyInfo GetPropertyInfo(string propertyName, object parentObject)
        {
            if (string.IsNullOrWhiteSpace(propertyName) || parentObject == null)
            {
                return null;
            }

            var propertyInfo = parentObject.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return propertyInfo;
        }

        private static MethodInfo GetMethodInfo(string methodName, object parentObject)
        {
            if (string.IsNullOrWhiteSpace(methodName) || parentObject == null)
            {
                return null;
            }

            var methodInfo = parentObject.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return methodInfo;
        }
    }

#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
}
