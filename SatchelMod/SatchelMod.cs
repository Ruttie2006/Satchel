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
        private List<((string scene, string obj) path, FieldInfo field)> PreloadFields { get; set; } = new();
        private List<((string scene, string obj) path, PropertyInfo property)> PreloadProperties { get; set; } = new();
        private List<(string scene, FieldInfo field)> PreloadGroupFields { get; set; } = new();
        private List<(string scene, PropertyInfo property)> PreloadGroupProperties { get; set; } = new();
        private List<MethodInfo> InitMethods { get; set; } = new();

        /// <summary>
        /// Needs to be called in the constructor of the inheriting type.
        /// </summary>
        /// <param name="inherit">The object that inherits the class. (pass <see langword="this"/>)</param>
        protected void ctor<T>(T inherit) where T: notnull, SatchelMod
        {
            if (InheritingObj is not null)
                throw new InvalidOperationException($"You're not allowed to call the {nameof(ctor)} method of {nameof(SatchelMod)} more than once.");
            InheritingObj = inherit;
            InheritingType = inherit.GetType();

            foreach (var (attr, member) in PreloadAttribute.GetForType(InheritingType))
            {
                if (member is FieldInfo field && (field.FieldType == typeof(GameObject) || attr.IgnoreIsGOCheck))
                    PreloadFields.Add((attr.GetTuple(), field));
                else if (member is PropertyInfo property && (property.PropertyType == typeof(GameObject) || attr.IgnoreIsGOCheck))
                    PreloadProperties.Add((attr.GetTuple(), property));
            }

            foreach (var (attr, member) in PreloadCollectionAttribute.GetForType(InheritingType))
            {
                if (member is FieldInfo field && field.FieldType == typeof(Dictionary<string, GameObject>))
                    PreloadGroupFields.Add((attr.Scene, field));
                else if (member is PropertyInfo property && property.PropertyType == typeof(Dictionary<string, GameObject>))
                    PreloadGroupProperties.Add((attr.Scene, property));
            }

            foreach (var item in InitializerAttribute.GetForType(InheritingType))
                InitMethods.Add(item);
        }

        /// <summary>
        /// DO NOT OVERRIDE!
        /// </summary>
        public override List<(string, string)> GetPreloadNames()
        {
            var list = new List<(string, string)>(PreloadFields.Select(x => x.path));
            list.AddRange(PreloadProperties.Select(x => x.path));
            foreach (var item in SubMods)
                list.AddRange(item.GetPreloadNames());
            list.AddRange(CustomPreloads());
            return list;
        }

        /// <summary>
        /// Override this to add your own preloads.
        /// </summary>
        /// <returns></returns>
        public virtual List<(string scene, string obj)> CustomPreloads() =>
            new();

        /// <summary>
        /// DO NOT OVERRIDE, OVERRIDE <see cref="Mod.Initialize()"/> INSTEAD! <para/> IF YOU DO OVERRIDE, CALL <see cref="Initialize(Dictionary{string, Dictionary{string, GameObject}})"/> BEFORE YOUR CODE!
        /// </summary>
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (InheritingObj is null)
                throw new InvalidOperationException($"The {nameof(ctor)} method needs to be called!");

            foreach (var (scene, field) in PreloadGroupFields)
            {
                try
                {
                    var preload = preloadedObjects[scene];
                    field.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({scene}) was not found.");
                    Log($"No preloads for scene {scene} were found! Assigning null instead.");
                    field.SetValue(InheritingObj, null);
                }
            }

            foreach (var (path, field) in PreloadFields)
            {
                try
                {
                    var preload = preloadedObjects[path.scene][path.obj];
                    field.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({path.scene}, {path.obj}) was not found.");
                    Log($"Preload {path.obj} in scene {path.scene} was not found! Assigning null instead.");
                    field.SetValue(InheritingObj, null);
                }
            }

            foreach (var (scene, property) in PreloadGroupProperties)
            {
                try
                {
                    var preload = preloadedObjects[scene];
                    property.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({scene}) was not found.");
                    Log($"No preloads for scene {scene} were found! Assigning null instead.");
                    property.SetValue(InheritingObj, null);
                }
            }

            foreach (var (path, property) in PreloadProperties)
            {
                try
                {
                    var preload = preloadedObjects[path.scene][path.obj];
                    property.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({path.scene}, {path.obj}) was not found.");
                    Log($"Preload {path.obj} in scene {path.scene} was not found! Assigning null instead.");
                    property.SetValue(InheritingObj, null);
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