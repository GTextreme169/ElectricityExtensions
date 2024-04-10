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
    Version = "0.0.4",
    Authors = new[] {
        "GTextreme169"
    }, 
    RequiredOnClient = true, 
    RequiredOnServer = true
)]

namespace ElectricityExtensions {
    public class ElectricityExtensions : ModSystem {
        public override void Start(ICoreAPI api) {
            base.Start(api);
            
            // Auto Electric Forge
            api.RegisterBlockEntityBehaviorClass("AutoElectricForge", typeof(EntityBehaviorAutoElectricForge));

            // Metal Blocks Conductor
            api.RegisterBlockClass("Conductor", typeof(BlockConductor));
            api.RegisterBlockEntityClass("Conductor", typeof(BlockEntityConductor));
            
            // Oven
            api.RegisterBlockClass("Oven", typeof(BlockOven));
            api.RegisterBlockEntityClass("Oven", typeof(BlockEntityOven));
            api.RegisterBlockEntityBehaviorClass("Oven", typeof(EntityBehaviorOven));

        }

    }

}
