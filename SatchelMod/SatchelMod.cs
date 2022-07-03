using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#nullable enable
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

//Made by Ruttie!!

namespace Satchel
{
    public abstract class SatchelMod : Mod
    {
        /// <summary>
        /// Called after initialize.
        /// </summary>
        public event Action? OnInitialize;
        /// <summary>
        /// Contains all preloads.
        /// </summary>
        public Dictionary<string, Dictionary<string, GameObject>> Preloads { get; private set; } = new();
        /// <summary>
        /// The current satchel core.
        /// </summary>
        public Core SatchelCore { get; private set; } = new();

        internal List<SubMod<SatchelMod>> SubMods { get; private set; } = new();

        private object InheritingObj { get; set; } = null!;
        private Type InheritingType { get; set; } = null!;
        private List<((string, string), FieldInfo)> PreloadFields { get; set; } = new();
        private List<((string, string), PropertyInfo)> PreloadProperties { get; set; } = new();
        private List<(string, FieldInfo)> PreloadGroupFields { get; set; } = new();
        private List<(string, PropertyInfo)> PreloadGroupProperties { get; set; } = new();
        private List<MethodInfo> InitMethods { get; set; } = new();

        /// <summary>
        /// Needs to be called in the constructor of the inheriting type
        /// </summary>
        /// <param name="inherit">The object that inherits the class</param>
        protected void ctor<T>(T inherit) where T: notnull, SatchelMod
        {
            if (InheritingObj is not null)
                throw new InvalidOperationException($"You're not allowed to call the {nameof(ctor)} method of {nameof(SatchelMod)} more than once.");
            InheritingObj = inherit;
            InheritingType = inherit.GetType();
            foreach (var item in InheritingType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty)
                .Where(x => x.GetCustomAttributes().Any(x => x.GetType() == typeof(PreloadAttribute)))
                .Select(x => (x.GetCustomAttribute<PreloadAttribute>(), x)))
            {
                if (item.x is FieldInfo field && (field.FieldType == typeof(GameObject) || item.Item1.IgnoreIsGOCheck))
                    PreloadFields.Add((item.Item1.GetTuple(), field));
                else if (item.x is PropertyInfo property && (property.PropertyType == typeof(GameObject) || item.Item1.IgnoreIsGOCheck))
                    PreloadProperties.Add((item.Item1.GetTuple(), property));
            }
            foreach (var item in InheritingType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty)
                .Where(x => x.GetCustomAttributes().Any(x => x.GetType() == typeof(PreloadCollectionAttribute)))
                .Select(x => (x.GetCustomAttribute<PreloadCollectionAttribute>(), x)))
            {
                if (item.x is FieldInfo field && field.FieldType == typeof(Dictionary<string, GameObject>))
                    PreloadGroupFields.Add((item.Item1.Scene, field));
                else if (item.x is PropertyInfo property && property.PropertyType == typeof(Dictionary<string, GameObject>))
                    PreloadGroupProperties.Add((item.Item1.Scene, property));
            }
            foreach (var item in InheritingType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.GetCustomAttributes().Any(x => x.GetType() == typeof(InitializerAttribute))))
            {
                if (!item.GetParameters().Any())
                    InitMethods.Add(item);
                else
                    Debug.Log($"Method {item.Name} is not a valid initialize method.");
            }
        }

        /// <summary>
        /// DO NOT OVERRIDE!
        /// </summary>
        public override List<(string, string)> GetPreloadNames()
        {
            var list = new List<(string, string)>(PreloadFields.Select(x => x.Item1));
            list.AddRange(PreloadProperties.Select(x => x.Item1));
            list.AddRange(CustomPreloads());
            return list;
        }

        /// <summary>
        /// Override this to add your own preloads.
        /// </summary>
        /// <returns></returns>
        public virtual List<(string, string)> CustomPreloads() =>
            new();

        /// <summary>
        /// DO NOT OVERRIDE, OVERRIDE <see cref="Mod.Initialize()"/> INSTEAD! <para/> IF YOU DO OVERRIDE, CALL <see cref="Initialize(Dictionary{string, Dictionary{string, GameObject}})"/> BEFORE YOUR CODE!
        /// </summary>
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (InheritingObj is null)
                throw new InvalidOperationException($"The {nameof(ctor)} method needs to be called!");
            foreach (var item in PreloadGroupFields)
            {
                try
                {
                    var preload = preloadedObjects[item.Item1];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1}) was not found.");
                    Log($"No preloads for scene {item.Item1} were found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            foreach (var item in PreloadFields)
            {
                try
                {
                    var preload = preloadedObjects[item.Item1.Item1][item.Item1.Item2];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1.Item1}, {item.Item1.Item2}) was not found.");
                    Log($"Preload {item.Item1.Item2} in scene {item.Item1.Item1} was not found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            foreach (var item in PreloadGroupProperties)
            {
                try
                {
                    var preload = preloadedObjects[item.Item1];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1}) was not found.");
                    Log($"No preloads for scene {item.Item1} were found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            foreach (var item in PreloadProperties)
            {
                try
                {
                    var preload = preloadedObjects[item.Item1.Item1][item.Item1.Item2];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1.Item1}, {item.Item1.Item2}) was not found.");
                    Log($"Preload {item.Item1.Item2} in scene {item.Item1.Item1} was not found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            Preloads = preloadedObjects;
            base.Initialize(preloadedObjects);
            foreach (var item in InitMethods)
                item.Invoke(InheritingObj, null);

            foreach (var item in SubMods)
                item.LoadPreloads();

            #region Cleanup
            PreloadFields.Clear();
            PreloadFields = null!;
            PreloadProperties.Clear();
            PreloadProperties = null!;
            PreloadGroupFields.Clear();
            PreloadGroupFields = null!;
            PreloadGroupProperties.Clear();
            PreloadGroupProperties = null!;
            InitMethods.Clear();
            InitMethods = null!;
            Task.Run(GC.Collect);
            #endregion

            OnInitialize?.Invoke();
        }
    }

    public class TEST : SatchelMod
    {
        [Preload("Scene", "Object_name")]
        public GameObject Preload { get; set; }
        [PreloadCollection("Scene")]
        private Dictionary<string, GameObject> PreloadsInScene;

        public static TEST Instance { get; set; }
        
        public TEST()
        {
            ctor(this);
            Instance = this;
        }

        [Initializer]
        public void Initialize1()
        {
            
        }
    }

    public class SubTEST : SubMod<TEST>
    {
        public SubTEST() : base(TEST.Instance)
        {
            ctor(this);
        }

        protected override void Initialize()
        {
            //Do stuff.
            base.Initialize(); //Not required, something normal people probably won't do.
        }

        [Initializer]
        public void thing()
        {

        }
    }
}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.