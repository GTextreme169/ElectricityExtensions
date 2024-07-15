namespace ElectricityExtensions.Content.Block.Entity.Behavior;

using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;
using ElectricForge = Electricity.Content.Block.Entity.ElectricForge;

public sealed class EntityBehaviorAutoElectricForge : BlockEntityBehavior, IElectricConsumer {
    private int maxTemp;
    private int powerSetting;
    private bool hasItems = false;
    private int MaxTemp => ElectricityExtensions.Instance?.Settings.ElectricForgeMaxTemperature ?? 1100;
    private int PowerConsumption => ElectricityExtensions.Instance?.Settings.ElectricForgeMaxPowerConsumption ?? 100;
    private int MinPowerConsumption => ElectricityExtensions.Instance?.Settings.ElectricForgeMinPowerConsumption ?? 10;

    public EntityBehaviorAutoElectricForge(BlockEntity blockEntity) : base(blockEntity) { }

    public ConsumptionRange ConsumptionRange => hasItems ? new ConsumptionRange(MinPowerConsumption, PowerConsumption) : new ConsumptionRange(0, 0);

    public void Consume(int amount)
    {
        ElectricForge? entity = null;
        if (this.Blockentity is ElectricForge temp)
        {
            entity = temp;
            hasItems = entity?.Contents?.StackSize > 0;
        }
        if (!hasItems) {
            amount = 0;
        }
        if (this.powerSetting != amount) {
            this.powerSetting = amount;
            this.maxTemp = (amount * MaxTemp) / PowerConsumption;

            if (entity != null)
            {
                entity.MaxTemp = this.maxTemp;
                entity.IsBurning = amount > 0 && this.hasItems;
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