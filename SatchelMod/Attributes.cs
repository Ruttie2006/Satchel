//Made by Ruttie!!

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
    }

    /// <summary>
    /// Marks a method as an intialize method that needs to be called in initialize. Methods with this attribute must have no parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class InitializerAttribute : Attribute { }
}
