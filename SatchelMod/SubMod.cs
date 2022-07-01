//Made by Ruttie!!

namespace Satchel
{
    public abstract class SubMod<TBase> where TBase: SatchelMod
    {
        public Dictionary<string, Dictionary<string, GameObject>> Preloads { get => Mod.Preloads; }

        private TBase Mod { get; set; }

        public SubMod(TBase mod)
        {
            Mod = mod;
            Mod.OnInitialize += Initialize;
        }

        /// <summary>
        /// Called right after RMod.Initialize() is called
        /// </summary>
        public virtual void Initialize() { }
    }
}
