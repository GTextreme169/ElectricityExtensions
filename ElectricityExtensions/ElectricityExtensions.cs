using ElectricityExtensions.Content.Block;
using ElectricityExtensions.Content.Block.Entity;
using ElectricityExtensions.Content.Block.Entity.Behavior;
using Vintagestory.API.Common;

[assembly: ModDependency("game", "1.19.5")]
[assembly: ModInfo(
    "Electricity Extensions",
    "electricityextensions",
    Website = "https://github.com/GTextreme169/ElectricityExtensions",
    Description = "Adds Extra Electricity Components to Vintage Story.",
    Version = "0.0.7",
    Authors = new[] {
        "GTextreme169"
    }, 
    RequiredOnClient = true, 
    RequiredOnServer = true
)]

namespace ElectricityExtensions {
    public class ElectricityExtensions : ModSystem {
        private static ElectricityExtensions? instance;
        public ElectricityExtensionsSettings Settings { get; private set; } = new ();
        public static ElectricityExtensions? Instance => instance;
        
        const string SettingsFileName = "electricity-extensions.json";
        
        public override void Start(ICoreAPI api) {
            base.Start(api);
            instance = this;
            
            // Get the mod config
            try
            {
                var temp = api.LoadModConfig<ElectricityExtensionsSettings>(SettingsFileName);
                if (temp != null)
                {
                    temp.Verify();
                    Settings = temp;
                }
                else api.StoreModConfig(Settings, SettingsFileName);
            } catch (Exception e)
            {
                api.World.Logger.Error("Failed to load mod config: {0}", e);
            }
            
            // Auto Electric Forge
            api.RegisterBlockEntityBehaviorClass("AutoElectricForge", typeof(EntityBehaviorAutoElectricForge));

            // Metal Blocks Conductor
            api.RegisterBlockClass("Conductor", typeof(BlockConductor));
            api.RegisterBlockEntityClass("Conductor", typeof(BlockEntityConductor));
            
            // Oven
            api.RegisterBlockClass("ElectricOven", typeof(BlockOven));
            api.RegisterBlockEntityClass("ElectricOven", typeof(BlockEntityOven));
            api.RegisterBlockEntityBehaviorClass("ElectricOven", typeof(EntityBehaviorOven));
        }

    }

}
