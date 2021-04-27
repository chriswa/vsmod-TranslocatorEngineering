using System;
using System.Reflection;

namespace TranslocatorEngineering {
  public static class ReflectionExtensions {
    public static T XXX_GetFieldValue<T>(this object obj, string name) {
      return XXX_GetFieldValue<T>(obj, obj.GetType(), name);
    }
    public static T XXX_GetFieldValue<T>(this object obj, Type type, string name) {
      var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
      var field = type.GetField(name, bindingFlags);
      return (T)field.GetValue(obj);
    }
    public static void XXX_SetFieldValue<T>(this object obj, string name, T value) {
      XXX_SetFieldValue<T>(obj, obj.GetType(), name, value);
    }
    public static void XXX_SetFieldValue<T>(this object obj, Type type, string name, T value) {
      var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField;
      var field = type.GetField(name, bindingFlags);
      field.SetValue(obj, value);
    }
    // e.g. .XXX_GetMethod("foo", new Type[] { typeof(int), typeof(byte[]) }) // finds `void foo(inf, byte[])`
    public static MethodInfo XXX_GetMethod(this object obj, string name, Type[] parameterTypes = null) {
      return XXX_GetMethod(obj, obj.GetType(), name, parameterTypes);
    }
    public static MethodInfo XXX_GetMethod(this object obj, Type type, string name, Type[] parameterTypes = null) {
      var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
      var method = obj.GetType().GetMethod(name, bindingFlags, null, CallingConventions.Any, parameterTypes, null);
      return method;
    }
    public static T XXX_InvokeMethod<T>(this object obj, string name, Type[] parameterTypes = null, object[] parameters = null) {
      return (T)_XXX_InvokeMethod(obj, obj.GetType(), name, parameterTypes, parameters);
    }
    public static T XXX_InvokeMethod<T>(this object obj, Type type, string name, Type[] parameterTypes = null, object[] parameters = null) {
      return (T)_XXX_InvokeMethod(obj, type, name, parameterTypes, parameters);
    }
    public static void XXX_InvokeVoidMethod(this object obj, string name, Type[] parameterTypes = null, object[] parameters = null) {
      _XXX_InvokeMethod(obj, obj.GetType(), name, parameterTypes, parameters);
    }
    public static void XXX_InvokeVoidMethod(this object obj, Type type, string name, Type[] parameterTypes = null, object[] parameters = null) {
      _XXX_InvokeMethod(obj, type, name, parameterTypes, parameters);
    }
    private static object _XXX_InvokeMethod(this object obj, Type type, string name, Type[] parameterTypes = null, object[] parameters = null) {
      var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
      if (parameters == null) {
        parameters = new object[0];
        parameterTypes = new Type[0];
      }
      var method = type.GetMethod(name, bindingFlags, null, CallingConventions.Any, parameterTypes, null);
      var result = method.Invoke(obj, bindingFlags, null, parameters, null);
      return result;
    }
  }
}