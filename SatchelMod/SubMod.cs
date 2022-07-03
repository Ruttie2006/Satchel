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

        private TBase Mod { get; set; }
        private object InheritingObj { get; set; }

        private List<((string, string), FieldInfo)> PreloadFields { get; set; } = new();
        private List<((string, string), PropertyInfo)> PreloadProperties { get; set; } = new();
        private List<(string, FieldInfo)> PreloadGroupFields { get; set; } = new();
        private List<(string, PropertyInfo)> PreloadGroupProperties { get; set; } = new();
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
                throw new InvalidOperationException($"You're not allowed to call the {nameof(ctor)} method of {nameof(SatchelMod)} more than once.");
            InheritingObj = inherit;
            var InheritingType = inherit.GetType();
            foreach (var item in InheritingType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.GetCustomAttribute<PreloadAttribute>() is not null)
                .Select(x => (x.GetCustomAttribute<PreloadAttribute>(), x)))
            {
                if (item.x is FieldInfo field && (field.FieldType == typeof(GameObject) || item.Item1.IgnoreIsGOCheck))
                    PreloadFields.Add((item.Item1.GetTuple(), field));
                else if (item.x is PropertyInfo property && (property.PropertyType == typeof(GameObject) || item.Item1.IgnoreIsGOCheck))
                    PreloadProperties.Add((item.Item1.GetTuple(), property));
            }
            foreach (var item in InheritingType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.GetCustomAttribute<PreloadCollectionAttribute>() is not null)
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

            Mod.SubMods.Add(this as SubMod<SatchelMod>);
        }

        /// <summary>
        /// DO NOT OVERRIDE!
        /// </summary>
        internal List<(string, string)> GetPreloadNames()
        {
            var list = new List<(string, string)>(PreloadFields.Select(x => x.Item1));
            list.AddRange(PreloadProperties.Select(x => x.Item1));
            list.AddRange(CustomPreloads());
            return list;
        }

        /// <summary>
        /// Override this to add your own preloads.
        /// </summary>
        /// <returns>Custom preloads.</returns>
        public virtual List<(string, string)> CustomPreloads() =>
            new();

        internal void LoadPreloads()
        {
            if (InheritingObj is null)
                throw new InvalidOperationException($"The {nameof(ctor)} method needs to be called!");
            foreach (var item in PreloadGroupFields)
            {
                try
                {
                    var preload = Preloads[item.Item1];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1}) was not found.");
                    Mod.Log($"No preloads for scene {item.Item1} were found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            foreach (var item in PreloadFields)
            {
                try
                {
                    var preload = Preloads[item.Item1.Item1][item.Item1.Item2];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1.Item1}, {item.Item1.Item2}) was not found.");
                    Mod.Log($"Preload {item.Item1.Item2} in scene {item.Item1.Item1} was not found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            foreach (var item in PreloadGroupProperties)
            {
                try
                {
                    var preload = Preloads[item.Item1];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1}) was not found.");
                    Mod.Log($"No preloads for scene {item.Item1} were found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            foreach (var item in PreloadProperties)
            {
                try
                {
                    var preload = Preloads[item.Item1.Item1][item.Item1.Item2];
                    item.Item2.SetValue(InheritingObj, preload);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Preload ({item.Item1.Item1}, {item.Item1.Item2}) was not found.");
                    Mod.Log($"Preload {item.Item1.Item2} in scene {item.Item1.Item1} was not found! Assigning null instead.");
                    item.Item2.SetValue(InheritingObj, null);
                }
            }
            foreach (var item in InitMethods)
                item.Invoke(InheritingObj, null);

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
        /// Called right before all [<see cref="InitializerAttribute"/>] methods.
        /// </summary>
        protected virtual void Initialize() { }
    }
}
