using ElectricityExtensions.Content.GUI;

namespace ElectricityExtensions.Content.Block.Entity;

using System;
using System.Collections.Generic;
using Electricity.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

public class BlockEntityOven : BlockEntityOpenableContainer, IHeatSource, IFirePit
{
  internal InventorySmelting inventory;
  public float PrevFurnaceTemperature = 20f;
  public float FurnaceTemperature => this.MaxTemp;
  public int MaxTemperature;
  public float InputStackCookingTime;
  private GuiDialogOven clientDialog;
  private bool clientSidePrevBurning;
  private FirepitContentsRenderer renderer;
  private bool shouldRedraw;
  

  public virtual int EnviromentTemperature() => 20;

  public virtual float MaxCookingTime() => this.inputSlot.Itemstack != null
    ? this.inputSlot.Itemstack.Collectible.GetMeltingDuration(this.Api.World, inventory,
      this.inputSlot)
    : 30f;

  public override string InventoryClassName => "stove";

  public virtual string DialogTitle => Lang.Get("Electric Firepit");

  public override InventoryBase Inventory => inventory;

  public BlockEntityOven()
  {
    this.inventory = new InventorySmelting((string)null, (ICoreAPI)null);
    this.inventory.SlotModified += new Action<int>(this.OnSlotModifid);
  }
  private Electricity.Content.Block.Entity.Behavior.Electricity? Electricity => this.GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();

  public override void OnBlockPlaced(ItemStack? byItemStack = null) {
    base.OnBlockPlaced(byItemStack);

    var electricity = this.Electricity;

    if (electricity != null) {
      electricity.Connection = Facing.DownAll;
    }
  }

  public override void Initialize(ICoreAPI api)
  {
    base.Initialize(api);
    this.inventory.pos = this.Pos;
    this.inventory.LateInitialize(
      "smelting-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api);
    this.RegisterGameTickListener(new Action<float>(this.OnBurnTick), 100);
    this.RegisterGameTickListener(new Action<float>(this.On500msTick), 500);
    if (api is ICoreClientAPI)
    {
      this.renderer = new FirepitContentsRenderer(api as ICoreClientAPI, this.Pos);
      (api as ICoreClientAPI).Event.RegisterRenderer((IRenderer)this.renderer, EnumRenderStage.Opaque, "firepit");
      this.UpdateRenderer();
    }
  }

  private void OnSlotModifid(int slotid)
  {
    this.Block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
    this.UpdateRenderer();
    this.MarkDirty(this.Api.Side == EnumAppSide.Server);
    this.shouldRedraw = true;
    if (this.Api is ICoreClientAPI && this.clientDialog != null)
      this.SetDialogValues(this.clientDialog.Attributes);
    this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos)?.MarkModified();
  }


  public bool IsBurning => MaxTemp > 20f;
  public int MaxTemp { get; set; } = 20;


  private void On500msTick(float dt)
  {
    if (this.Api is ICoreServerAPI &&
        (this.IsBurning || (double)this.PrevFurnaceTemperature != (double)this.FurnaceTemperature))
      this.MarkDirty();
    this.PrevFurnaceTemperature = this.FurnaceTemperature;

    this.MarkDirty(true);
  }
  

  public bool CanHeatInput()
  {
    if (this.CanSmeltInput())
      return true;
    return this.inputStack?.ItemAttributes?["allowHeating"] != null && this.inputStack.ItemAttributes["allowHeating"].AsBool();
  }


  private void HeatInput(float dt)
  {
    float inputStackTemp = this.InputStackTemp;
    float num = inputStackTemp;
    float meltingPoint = this.inputSlot.Itemstack.Collectible.GetMeltingPoint(this.Api.World, (ISlotProvider) this.inventory, this.inputSlot);
    if ((double) inputStackTemp < (double) FurnaceTemperature)
    {
      float dt1 = (1f + GameMath.Clamp((float) (((double) FurnaceTemperature - (double) inputStackTemp) / 30.0), 0.0f, 1.6f)) * dt;
      if ((double) num >= (double) meltingPoint)
        dt1 /= 11f;
      float val2 = ChangeTemperature(inputStackTemp, FurnaceTemperature, dt1);
      int val1 = Math.Max(this.inputStack.Collectible.CombustibleProps == null ? 0 : this.inputStack.Collectible.CombustibleProps.MaxTemperature, this.inputStack.ItemAttributes?["maxTemperature"] == null ? 0 : this.inputStack.ItemAttributes["maxTemperature"].AsInt());
      if (val1 > 0)
        val2 = Math.Min((float) val1, val2);
      if ((double) inputStackTemp != (double) val2)
      {
        this.InputStackTemp = val2;
        num = val2;
      }
    }
    if ((double) num >= (double) meltingPoint)
    {
      this.InputStackCookingTime += (float) GameMath.Clamp((int) (num / meltingPoint), 1, 30) * dt;
    }
    else
    {
      if ((double) this.InputStackCookingTime <= 0.0)
        return;
      --this.InputStackCookingTime;
    }
  }

  private void OnBurnTick(float dt)
  {
    if (this.Api is ICoreClientAPI)
    {
      this.renderer?.contentStackRenderer?.OnUpdate(this.InputStackTemp);
    }

    string inventoryItems = "";
    int i = 0;
    foreach (ItemSlot inventorySlot in this.inventory)
    {
      if (inventorySlot.Itemstack != null)
        inventoryItems += i +": " + inventorySlot.Itemstack.Collectible.Code.ToString() + " ";
      
      i++;
    }
    if (this.CanHeatInput())
      this.HeatInput(dt);
    else
      this.InputStackCookingTime = 0.0f;

    if (this.CanSmeltInput() && (double)this.InputStackCookingTime > (double)this.MaxCookingTime())
    {
      this.SmeltItems();
    }else if (MaxTemp < 20f)
    { // Unable to Heat because Electricity is Off
      if (InputStackTemp > 20f)
      {
        InputStackTemp -= dt;
      }
    }
  }


  public float ChangeTemperature(float fromTemp, float toTemp, float dt)
  {
    float num = Math.Abs(fromTemp - toTemp);
    dt += dt * (num / 28f);
    if ((double)num < (double)dt)
      return toTemp;
    if ((double)fromTemp > (double)toTemp)
      dt = -dt;
    return (double)Math.Abs(fromTemp - toTemp) < 1.0 ? toTemp : fromTemp + dt;
  }

  public void HeatOutput(float dt)
  {
    float outputStackTemp = this.OutputStackTemp;
    if ((double)outputStackTemp >= (double)this.FurnaceTemperature)
      return;
    float val2 = this.ChangeTemperature(outputStackTemp, this.FurnaceTemperature, 2f * dt);
    int val1 = Math.Max(
      this.outputStack.Collectible.CombustibleProps == null
        ? 0
        : this.outputStack.Collectible.CombustibleProps.MaxTemperature,
      this.outputStack.ItemAttributes?["maxTemperature"] == null
        ? 0
        : this.outputStack.ItemAttributes["maxTemperature"].AsInt());
    if (val1 > 0)
      val2 = Math.Min((float)val1, val2);
    if ((double)outputStackTemp == (double)val2)
      return;
  }


  public float InputStackTemp
  {
    get => this.GetTemp(this.inputStack);
    set => this.SetTemp(this.inputStack, value);
  }


  public float OutputStackTemp
  {
    get => 1100f;
  }

  private float GetTemp(ItemStack stack)
  {
    if (stack == null)
      return (float)this.EnviromentTemperature();
    if (this.inventory.CookingSlots.Length == 0)
      return stack.Collectible.GetTemperature(this.Api.World, stack);
    bool flag = false;
    float val1 = 0.0f;
    for (int index = 0; index < this.inventory.CookingSlots.Length; ++index)
    {
      ItemStack itemstack = this.inventory.CookingSlots[index].Itemstack;
      if (itemstack != null)
      {
        float temperature = itemstack.Collectible.GetTemperature(this.Api.World, itemstack);
        val1 = flag ? Math.Min(val1, temperature) : temperature;
        flag = true;
      }
    }

    return val1;
  }

  private void SetTemp(ItemStack stack, float value)
  {
    if (stack == null)
      return;
    if (this.inventory.CookingSlots.Length != 0)
    {
      for (int index = 0; index < this.inventory.CookingSlots.Length; ++index)
        this.inventory.CookingSlots[index].Itemstack?.Collectible.SetTemperature(this.Api.World,
          this.inventory.CookingSlots[index].Itemstack, value);
    }
    else
      stack.Collectible.SetTemperature(this.Api.World, stack, value);
  }


  public void SetBlockState()
  {
    string burnstate = IsBurning ? "on" : "off";
    
    Block block = this.Api.World.GetBlock(this.Block.CodeWithVariant("burnstate", burnstate));
    if (block == null)
      return;
    this.Api.World.BlockAccessor.ExchangeBlock(block.Id, this.Pos);
    this.Block = block;
  }

  public bool CanHeatOutput() => this.outputStack?.ItemAttributes?["allowHeating"] != null &&
                                 this.outputStack.ItemAttributes["allowHeating"].AsBool();

  public bool CanSmeltInput()
  {
    if (this.inputStack == null || !this.inputStack.Collectible.CanSmelt(this.Api.World,
          (ISlotProvider)this.inventory, this.inputSlot.Itemstack, this.outputSlot.Itemstack))
      return false;
    return this.inputStack.Collectible.CombustibleProps == null ||
           !this.inputStack.Collectible.CombustibleProps.RequiresContainer;
  }

  public void SmeltItems()
  {
    this.inputStack.Collectible.DoSmelt(this.Api.World, (ISlotProvider)this.inventory, this.inputSlot,
      this.outputSlot);
    this.InputStackCookingTime = 0.0f;
    this.MarkDirty(true);
    this.inputSlot.MarkDirty();
  }

  public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
  {
    if (this.Api.Side == EnumAppSide.Client)
    {
      this.toggleInventoryDialogClient(byPlayer, (CreateDialogDelegate)(() =>
      {
        SyncedTreeAttribute syncedTreeAttribute = new SyncedTreeAttribute();
        this.SetDialogValues((ITreeAttribute)syncedTreeAttribute);
        this.clientDialog = new GuiDialogOven(this.DialogTitle, this.Inventory, this.Pos,
          syncedTreeAttribute, this.Api as ICoreClientAPI);

        return (GuiDialogBlockEntity)this.clientDialog;
      }));
    }

    return true;
  }

  public override void OnReceivedServerPacket(int packetid, byte[] data)
  {
    if (packetid != 1001)
      return;
    (this.Api.World as IClientWorldAccessor)?.Player.InventoryManager.CloseInventory((IInventory)this.Inventory);
    this.invDialog?.TryClose();
    this.invDialog?.Dispose();
    this.invDialog = null;
  }

  public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
  {
    base.FromTreeAttributes(tree, worldForResolving);
    this.Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
    if (this.Api != null)
      this.Inventory.AfterBlocksLoaded(this.Api.World);
    this.MaxTemperature = tree.GetInt("maxTemperature");
    this.InputStackCookingTime = tree.GetFloat("oreCookingTime");
    ICoreAPI api1 = this.Api;
    if ((api1 != null ? (api1.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
    {
      this.UpdateRenderer();
      if (this.clientDialog != null)
        this.SetDialogValues(this.clientDialog.Attributes);
    }

    ICoreAPI api2 = this.Api;
    if ((api2 != null ? (api2.Side == EnumAppSide.Client ? 1 : 0) : 0) == 0 ||
        this.clientSidePrevBurning == this.IsBurning && !this.shouldRedraw)
      return;
    this.GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(this.IsBurning);
    this.clientSidePrevBurning = this.IsBurning;
    this.MarkDirty(true);
    this.shouldRedraw = false;
  }

  private void UpdateRenderer()
  {
    if (this.renderer == null)
      return;
    ItemStack itemStack = this.inputStack == null ? this.outputStack : this.inputStack;
    if ((this.renderer.ContentStack == null || this.renderer.contentStackRenderer == null ||
         !(itemStack?.Collectible is IInFirepitRendererSupplier)
          ? 0
          : (this.renderer.ContentStack.Equals(this.Api.World, itemStack, GlobalConstants.IgnoredStackAttributes)
            ? 1
            : 0)) != 0)
      return;
    this.renderer.contentStackRenderer?.Dispose();
    this.renderer.contentStackRenderer = (IInFirepitRenderer)null;
    if (itemStack?.Collectible is IInFirepitRendererSupplier)
    {
      return;
    }

    InFirePitProps renderProps = this.GetRenderProps(itemStack);
    if (itemStack?.Collectible != null && !(itemStack?.Collectible is IInFirepitMeshSupplier) && renderProps != null)
      this.renderer.SetContents(itemStack, renderProps.Transform);
    else
      this.renderer.SetContents((ItemStack)null, (ModelTransform)null);
  }

  private void SetDialogValues(ITreeAttribute dialogTree)
  {
    dialogTree.SetInt("maxTemperature", this.MaxTemperature);
    dialogTree.SetFloat("oreCookingTime", this.InputStackCookingTime);
    if (this.inputSlot.Itemstack != null)
    {
      float meltingDuration =
        this.inputSlot.Itemstack.Collectible.GetMeltingDuration(this.Api.World, (ISlotProvider)this.inventory,
          this.inputSlot);
      dialogTree.SetFloat("oreTemperature", this.InputStackTemp);
      dialogTree.SetFloat("maxOreCookingTime", meltingDuration);
    }
    else
      dialogTree.RemoveAttribute("oreTemperature");

    dialogTree.SetString("outputText", this.inventory.GetOutputText());
    dialogTree.SetInt("haveCookingContainer", this.inventory.HaveCookingContainer ? 1 : 0);
    dialogTree.SetInt("quantityCookingSlots", this.inventory.CookingSlots.Length);
  }

  public override void ToTreeAttributes(ITreeAttribute tree)
  {
    base.ToTreeAttributes(tree);
    ITreeAttribute tree1 = (ITreeAttribute)new TreeAttribute();
    this.Inventory.ToTreeAttributes(tree1);
    tree["inventory"] = (IAttribute)tree1;
    tree.SetFloat("furnaceTemperature", this.FurnaceTemperature);
    tree.SetInt("maxTemperature", this.MaxTemperature);
    tree.SetFloat("oreCookingTime", this.InputStackCookingTime);
  }

  public override void OnBlockRemoved()
  {
    base.OnBlockRemoved();
    this.renderer?.Dispose();
    this.renderer = (FirepitContentsRenderer)null;
    if (this.clientDialog == null)
      return;
    this.clientDialog.TryClose();
    this.clientDialog?.Dispose();
    this.clientDialog = (GuiDialogOven)null;
  }

  public override void OnBlockBroken(IPlayer byPlayer = null) => base.OnBlockBroken((IPlayer)null);



  public ItemSlot inputSlot => this.inventory[1];

  public ItemSlot outputSlot => this.inventory[2];


  public ItemStack inputStack
  {
    get => this.inventory[1].Itemstack;
    set
    {
      this.inventory[1].Itemstack = value;
      this.inventory[1].MarkDirty();
    }
  }

  public ItemStack outputStack
  {
    get => this.inventory[2].Itemstack;
    set
    {
      this.inventory[2].Itemstack = value;
      this.inventory[2].MarkDirty();
    }
  }

  public override void OnStoreCollectibleMappings(
    Dictionary<int, AssetLocation> blockIdMapping,
    Dictionary<int, AssetLocation> itemIdMapping)
  {
    foreach (ItemSlot inSlot in this.Inventory)
    {
      if (inSlot.Itemstack != null)
      {
        if (inSlot.Itemstack.Class == EnumItemClass.Item)
          itemIdMapping[inSlot.Itemstack.Item.Id] = inSlot.Itemstack.Item.Code;
        else
          blockIdMapping[inSlot.Itemstack.Block.BlockId] = inSlot.Itemstack.Block.Code;
        inSlot.Itemstack.Collectible.OnStoreCollectibleMappings(this.Api.World, inSlot, blockIdMapping,
          itemIdMapping);
      }
    }

    foreach (ItemSlot cookingSlot in this.inventory.CookingSlots)
    {
      if (cookingSlot.Itemstack != null)
      {
        if (cookingSlot.Itemstack.Class == EnumItemClass.Item)
          itemIdMapping[cookingSlot.Itemstack.Item.Id] = cookingSlot.Itemstack.Item.Code;
        else
          blockIdMapping[cookingSlot.Itemstack.Block.BlockId] = cookingSlot.Itemstack.Block.Code;
        cookingSlot.Itemstack.Collectible.OnStoreCollectibleMappings(this.Api.World, cookingSlot, blockIdMapping,
          itemIdMapping);
      }
    }
  }

  public override void OnLoadCollectibleMappings(
    IWorldAccessor worldForResolve,
    Dictionary<int, AssetLocation> oldBlockIdMapping,
    Dictionary<int, AssetLocation> oldItemIdMapping,
    int schematicSeed)
  {
    foreach (ItemSlot inSlot in this.Inventory)
    {
      if (inSlot.Itemstack != null)
      {
        if (!inSlot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
          inSlot.Itemstack = (ItemStack)null;
        else
          inSlot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, inSlot, oldBlockIdMapping,
            oldItemIdMapping);
      }
    }

    foreach (ItemSlot cookingSlot in this.inventory.CookingSlots)
    {
      if (cookingSlot.Itemstack != null)
      {
        if (!cookingSlot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, this.Api.World))
          cookingSlot.Itemstack = (ItemStack)null;
        else
          cookingSlot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, cookingSlot, oldBlockIdMapping,
            oldItemIdMapping);
      }
    }
  }


  public override void OnBlockUnloaded()
  {
    base.OnBlockUnloaded();
    this.renderer?.Dispose();
  }

  private InFirePitProps? GetRenderProps(ItemStack? contentStack)
  {
    if (contentStack != null)
    {
      bool? nullable = contentStack.ItemAttributes?.KeyExists("inFirePitProps");
      bool flag = true;
      if (nullable.GetValueOrDefault() == flag & nullable.HasValue)
      {
        InFirePitProps renderProps = contentStack.ItemAttributes["inFirePitProps"].AsObject<InFirePitProps>();
        renderProps.Transform.EnsureDefaultValues();
        return renderProps;
      }
    }

    return null;
  }

  public float GetHeatStrength(
    IWorldAccessor world,
    BlockPos heatSourcePos,
    BlockPos heatReceiverPos)
  {
    if (this.IsBurning)
      return 10f;
    return 0;
  }
}