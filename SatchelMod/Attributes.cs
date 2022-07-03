//Made by Ruttie!!

using System.Linq;
using System.Reflection;

namespace Satchel
{
    /// <summary>
    /// Marks a field or property as a preload. If the type of the field or property is not <see cref="UnityEngine.GameObject"/>, it is skipped
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PreloadAttribute : Attribute
    {
        public string Scene { get; private set; }
        public string ObjectName { get; private set; }
        public bool IgnoreIsGOCheck { get; set; }

        public PreloadAttribute(string scene, string obj)
        {
            Scene = scene;
            ObjectName = obj;
        }

        public (string, string) GetTuple() =>
            (Scene, ObjectName);

        public static IEnumerable<(PreloadAttribute attr, MemberInfo member)> GetForType(Type type) =>
            type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.GetCustomAttribute<PreloadAttribute>() is not null)
                .Select(x => (x.GetCustomAttribute<PreloadAttribute>(), x));
    }

    /// <summary>
    /// Marks a field or property as a preload collection. If the type of the field or property is not Dictionary{string, GameObject}, it is skipped
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PreloadCollectionAttribute : Attribute
    {
        public string Scene { get; private set; }

        public PreloadCollectionAttribute(string scene)
        {
            Scene = scene;
        }

        public static IEnumerable<(PreloadCollectionAttribute attr, MemberInfo member)> GetForType(Type type) =>
            type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField
                | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.GetCustomAttribute<PreloadCollectionAttribute>() is not null)
                .Select(x => (x.GetCustomAttribute<PreloadCollectionAttribute>(), x));
    }

    /// <summary>
    /// Marks a method as an intialize method that needs to be called in initialize. Methods with this attribute must have no parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class InitializerAttribute : Attribute
    {
        public static IEnumerable<MethodInfo> GetForType(Type type) =>
            type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0 && x.GetCustomAttributes().Any(x => x.GetType() == typeof(InitializerAttribute)));
    }
}
