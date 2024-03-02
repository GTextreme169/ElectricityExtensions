using Vintagestory.API.Common;

[assembly: ModDependency("game", "1.18.4")]
[assembly: ModInfo(
    "Electricity Extensions",
    "electricityextensions",
    Website = "https://github.com/GTextreme169/ElectricityExtensions",
    Description = "Adds Extra Electricity Components to Vintage Story.",
    Version = "0.0.1",
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
            api.RegisterBlockEntityBehaviorClass("AutoElectricForge", typeof(Content.Block.Entity.Behavior.AutoElectricForge));
        }

    }

}
