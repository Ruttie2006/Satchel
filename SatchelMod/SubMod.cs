//Made by Ruttie!!

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Satchel
{
    public abstract class SubMod<TBase> where TBase: SatchelMod
    {
        /// <summary>
        /// Contains all preloads.
        /// </summary>
        public Dictionary<string, Dictionary<string, GameObject>> Preloads { get => Mod.Preloads; }
        /// <summary>
        /// The current satchel core of the main mod instance.
        /// </summary>
        public Core SatchelCore { get => Mod.SatchelCore; }

        private protected TBase Mod { get; set; }
        private object InheritingObj { get; set; }

        private List<((string scene, string obj) path, FieldInfo field)> PreloadFields { get; set; } = new();
        private List<((string scene, string obj) path, PropertyInfo property)> PreloadProperties { get; set; } = new();
        private List<(string scene, FieldInfo field)> PreloadGroupFields { get; set; } = new();
        private List<(string scene, PropertyInfo property)> PreloadGroupProperties { get; set; } = new();
        private List<MethodInfo> InitMethods { get; set; } = new();

        public SubMod(TBase mod)
        {
            Mod = mod;
            Mod.OnInitialize += InitializeInternal;
        }

        /// <summary>
        /// Needs to be called in the constructor of the inheriting type
        /// </summary>
        /// <param name="inherit">The object that inherits the class</param>
        protected void ctor(object inherit)
        {
            if (InheritingObj is not null)
                throw new InvalidOperationException($"You're not allowed to call the {nameof(ctor)} method of {nameof(SubMod<TBase>)} more than once.");
            InheritingObj = inherit;
            var InheritingType = inherit.GetType();

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

            Mod.SubMods.Add(this as SubMod<SatchelMod>);
        }

        /// <summary>
        /// DO NOT OVERRIDE!
        /// </summary>
        internal List<(string, string)> GetPreloadNames()
        {
            var list = new List<(string, string)>(PreloadFields.Select(x => x.Item1));
            list.AddRange(PreloadProperties.Select(x => x.path));
            list.AddRange(CustomPreloads());
            return list;
        }

        /// <summary>
        /// Override this to add your own preloads.
        /// </summary>
        /// <returns>Custom preloads.</returns>
        public virtual List<(string, string)> CustomPreloads() =>
            new();

        protected internal virtual void LoadPreloads()
        {
            if (InheritingObj is null)
                throw new InvalidOperationException($"The {nameof(ctor)} method needs to be called!");

            foreach (var (scene, field) in PreloadGroupFields)
            {
                try
                {
                    var preload = Preloads[scene];
                    field.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({scene}) was not found.");
                    Mod.Log($"No preloads for scene {scene} were found! Assigning null instead.");
                    field.SetValue(InheritingObj, null);
                }
            }

            foreach (var (path, field) in PreloadFields)
            {
                try
                {
                    var preload = Preloads[path.scene][path.obj];
                    field.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({path.scene}, {path.obj}) was not found.");
                    Mod.Log($"Preload {path.obj} in scene {path.scene} was not found! Assigning null instead.");
                    field.SetValue(InheritingObj, null);
                }
            }

            foreach (var (scene, property) in PreloadGroupProperties)
            {
                try
                {
                    var preload = Preloads[scene];
                    property.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({scene}) was not found.");
                    Mod.Log($"No preloads for scene {scene} were found! Assigning null instead.");
                    property.SetValue(InheritingObj, null);
                }
            }

            foreach (var (path, property) in PreloadProperties)
            {
                try
                {
                    var preload = Preloads[path.scene][path.obj];
                    property.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({path.scene}, {path.obj}) was not found.");
                    Mod.Log($"Preload {path.obj} in scene {path.scene} was not found! Assigning null instead.");
                    property.SetValue(InheritingObj, null);
                }
            }

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
            //Let the main mod instance handle the GC, otherwise we get too much GC spam.
            #endregion 
        }

        private void InitializeInternal()
        {
            Initialize();
            foreach (var item in InitMethods)
                item.Invoke(InheritingObj, null);
        }

        /// <summary>
        /// Called right before all [<see cref="InitializerAttribute"/>] methods, and after all RMod.Initialize methods.
        /// </summary>
        protected virtual void Initialize() { }
    }
}
