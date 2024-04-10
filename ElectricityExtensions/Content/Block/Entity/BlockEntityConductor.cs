using Electricity.Content.Block.Entity;
using Electricity.Utils;
using Vintagestory.API.Common;

namespace ElectricityExtensions.Content.Block.Entity;


public class BlockEntityConductor : Cable {
    
    // This is the same as the Electricity property in Cable.cs But has to be done due to private access modifier
    private Electricity.Content.Block.Entity.Behavior.Electricity Electricity
    {
        get => this.GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();
    }
    
    public override void OnBlockPlaced(ItemStack? byItemStack = null) {
        base.OnBlockPlaced(byItemStack);

        var electricity = this.Electricity;

        if (electricity != null) {
            electricity.Connection = Facing.AllAll;
        }
    }
}

