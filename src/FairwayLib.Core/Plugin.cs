using BepInEx;
using BepInEx.Logging;

namespace FairwayLib.Core
{
    // This BepInAutoPlugin attribute comes from the Hamunii.BepInEx.AutoPlugin
    // NuGet package, and it will generate the BepInPlugin attribute for you!
    // For more info, see https://github.com/Hamunii/BepInEx.AutoPlugin
    [BepInAutoPlugin]
    public partial class CorePlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");
        }
    }
}
