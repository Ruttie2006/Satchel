namespace Satchel.BetterPreloads{
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]   
    public class PreloadAttribute : Attribute{
        internal string scene;
        internal string objPath;
        public PreloadAttribute(string scene, string objPath){
            this.scene = scene;
            this.objPath = objPath;
        }
    }
}