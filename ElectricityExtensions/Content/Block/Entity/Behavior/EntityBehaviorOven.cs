namespace ElectricityExtensions.Content.Block.Entity.Behavior;

using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;

public sealed class EntityBehaviorOven : BlockEntityBehavior, IElectricConsumer {
    private int maxTemp;
    private int powerSetting;
    private bool hasItems = true;
    private int MaxTemp => ElectricityExtensions.Instance?.Settings.OvenMaxTemperature ?? 1100;
    private int PowerConsumption => ElectricityExtensions.Instance?.Settings.OvenMaxPowerConsumption ?? 100;
    private int MinPowerConsumption => ElectricityExtensions.Instance?.Settings.OvenMinPowerConsumption ?? 10;
    
    public EntityBehaviorOven(BlockEntity blockEntity) : base(blockEntity)
    {
        
    }

    public ConsumptionRange ConsumptionRange => hasItems ? new ConsumptionRange(MinPowerConsumption, PowerConsumption) : new ConsumptionRange(0, 0);

    public void Consume(int amount)
    {
        Entity.BlockEntityOven? entity = null;
        if (this.Blockentity is Entity.BlockEntityOven temp)
        {
            entity = temp;
            hasItems =  entity.CanHeatInput();
            temp.SetBlockState();
        }
        if (!hasItems) {
            amount = 0;
        }
        if (this.powerSetting != amount) {
            this.powerSetting = amount;
            this.maxTemp = (amount * MaxTemp) / PowerConsumption;
            if (entity != null) {
                entity.MaxTemp = this.maxTemp;
            }
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);

        stringBuilder.AppendLine(StringHelper.Progressbar(this.powerSetting));
        stringBuilder.AppendLine("├ Consumption: " + this.powerSetting + "/" + PowerConsumption + "⚡   ");
        stringBuilder.AppendLine("└ Temperature: " + this.maxTemp + "° (max.)");
        stringBuilder.AppendLine();
    }
}
