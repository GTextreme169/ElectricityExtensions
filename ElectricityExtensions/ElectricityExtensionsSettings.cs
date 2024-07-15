namespace ElectricityExtensions;

public class ElectricityExtensionsSettings
{
    // Oven
    public int OvenMaxTemperature = 1100;
    public int OvenMaxPowerConsumption = 100;
    public int OvenMinPowerConsumption = 10;
    
    
    // Electric Forge
    public int ElectricForgeMaxTemperature = 1100;
    public int ElectricForgeMaxPowerConsumption = 100;
    public int ElectricForgeMinPowerConsumption = 10;

    public void Verify()
    {
        // No Negative Values or Zero
        if (OvenMaxTemperature <= 0) throw new Exception("OvenMaxTemperature must be greater than 0");
        if (OvenMaxPowerConsumption <= 0) throw new Exception("OvenMaxPowerConsumption must be greater than 0");
        if (OvenMinPowerConsumption <= 0) throw new Exception("OvenMinPowerConsumption must be greater than 0");
        if (OvenMaxPowerConsumption <= OvenMinPowerConsumption) throw new Exception("OvenMaxPowerConsumption must be greater than OvenMinPowerConsumption");
        
        if (ElectricForgeMaxTemperature <= 0) throw new Exception("ElectricForgeMaxTemperature must be greater than 0");
        if (ElectricForgeMaxPowerConsumption <= 0) throw new Exception("ElectricForgeMaxPowerConsumption must be greater than 0");
        if (ElectricForgeMinPowerConsumption <= 0) throw new Exception("ElectricForgeMinPowerConsumption must be greater than 0");
        if (ElectricForgeMaxPowerConsumption <= ElectricForgeMinPowerConsumption) throw new Exception("ElectricForgeMaxPowerConsumption must be greater than ElectricForgeMinPowerConsumption");
    }
}