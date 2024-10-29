using System;
using System.Linq;
using System.Reflection;

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace MergeNow.Core.Utils
{
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
            Type[] parameterTypes = methodArguments?.Select(ma => ma.GetType()).ToArray();

			var methodInfo = GetMethodInfo(methodName, parentObject, parameterTypes);
            if (methodInfo == null)
            {
                return;
            }

            methodInfo.Invoke(parentObject, methodArguments);
        }

        public static T InvokeMethod<T>(string methodName, object parentObject, params object[] methodArguments)
            where T : class
        {
            Type[] parameterTypes = methodArguments?.Select(ma => ma.GetType()).ToArray();

            var methodInfo = GetMethodInfo(methodName, parentObject, parameterTypes);
            if (methodInfo == null)
            {
                return default;
            }

            return methodInfo.Invoke(parentObject, methodArguments) as T;
        }

        public static Type GetNestedType(string typeName, Type parentType)
        {
            return parentType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static PropertyInfo GetPropertyInfo(string propertyName, object parentObject)
        {
            return GetPropertyInfo(propertyName, parentObject?.GetType());
        }

        private static PropertyInfo GetPropertyInfo(string propertyName, Type parentType)
        {
            if (string.IsNullOrWhiteSpace(propertyName) || parentType == null)
            {
                return null;
            }

            var propertyInfo = parentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (propertyInfo == null)
            {
                return GetPropertyInfo(propertyName, parentType.BaseType);
            }

            return propertyInfo;
        }

        private static MethodInfo GetMethodInfo(string methodName, object parentObject, Type[] parameterTypes)
        {
            if (string.IsNullOrWhiteSpace(methodName) || parentObject == null)
            {
                return null;
            }

            var methodInfo = parentObject.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, parameterTypes, null);
            return methodInfo;
        }
    }
}
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
