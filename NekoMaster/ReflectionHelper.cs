using System;
using System.Linq;
using System.Reflection;

namespace NekoMaster.Reflection;

public static class ReflectionHelper
{
    public const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    public const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    public const BindingFlags PubStcFlags = BindingFlags.NonPublic | BindingFlags.Instance;
    public const BindingFlags PubInsFlags = BindingFlags.Public | BindingFlags.Instance;
    public const BindingFlags PriInsFlags = BindingFlags.NonPublic | BindingFlags.Instance;
    public const BindingFlags PubPriInsFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    public static object GetFoP(this object obj, string name)
    {
        return obj.GetType().GetField(name, AllFlags)?.GetValue(obj) 
            ?? obj.GetType().GetProperty(name, AllFlags)?.GetValue(obj);
    }

    public static T GetFoP<T>(this object obj, string name)
    {
        return (T)GetFoP(obj, name);
    }

    public static object GetBF(this object obj, string name)
    {
        return obj.GetType().GetField($"<{name}>k__BackingField", AllFlags)?.GetValue(obj);
    }

    public static T GetBF<T>(this object obj, string name)
    {
        return (T)GetFoP(obj, name);
    }

    public static void SetFoP(this object obj, string name, object value)
    {
        var field = obj.GetType().GetField(name, AllFlags);
        if(field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            obj.GetType().GetProperty(name, AllFlags).SetValue(obj, value);
        }
    }

    public static void SetBF(this object obj, string name, object value)
    {
        var field = obj.GetType().GetField($"<{name}>k__BackingField", AllFlags);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    public static object GetStaticFoP(this object obj, string type, string name)
    {
        return obj.GetType().Assembly.GetType(type).GetField(name, StaticFlags)?.GetValue(null)
            ?? obj.GetType().Assembly.GetType(type).GetProperty(name, StaticFlags)?.GetValue(null);
    }

    public static T GetStaticFoP<T>(this object obj, string type, string name)
    {
        return (T)GetStaticFoP(obj, type, name);
    }

    public static void SetStaticFoP(this object obj, string type, string name, object value)
    {
        var field = obj.GetType().Assembly.GetType(type).GetField(name, StaticFlags);
        if (field != null)
        {
            field.SetValue(null, value);
        }
        else
        {
            obj.GetType().Assembly.GetType(type).GetProperty(name, StaticFlags).SetValue(null, value);
        }
    }
    public static object CallEvent(this object obj, string name, params object[] values)
    {
        var einfo= obj.GetType().GetEvent(name);
        return einfo.RaiseMethod.Invoke(obj, values);
    }

    public static object Call(this object obj, string name, params object[] values)
    {
        var info = obj.GetType().GetMethod(name, AllFlags, values.Select(x => x.GetType()).ToArray());
        return info.Invoke(obj, values);
    }

    public static object GetService(this Assembly a, string serviceName, string typeName, BindingFlags bind = BindingFlags.Default)
    {
        return a.GetType(serviceName, true)
                 .MakeGenericType(a.GetType(typeName, true))
                 .GetMethod("Get").Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
    }
}
