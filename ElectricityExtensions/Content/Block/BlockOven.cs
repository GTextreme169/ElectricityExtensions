using Electricity.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using BlockEntityOven = ElectricityExtensions.Content.Block.Entity.BlockEntityOven;

namespace ElectricityExtensions.Content.Block;

public class BlockOven : Vintagestory.API.Common.Block
{
  private Vec3f[] basePos;
  private WorldInteraction[] interactions;
  private Shape? shape;
  private Animation? animation;
  public override void OnLoaded(ICoreAPI api)
  {
    // Call the "open" animation on the model
    shape = Vintagestory.API.Common.Shape.TryGet(api, "electricityextensions:shapes/block/oven.json");
    animation = shape.Animations[0];
    //TODO: Find a way to Run the animation
    
    base.OnLoaded(api);

    this.interactions = ObjectCacheUtil.GetOrCreate<WorldInteraction[]>(api,
      "firepitInteractions-5", (CreateCachableObjectDelegate<WorldInteraction[]>)(() =>
      {
        return new WorldInteraction[1]
        {
          new WorldInteraction()
          {
            ActionLangCode = "blockhelp-firepit-open",
            MouseButton = EnumMouseButton.Right,
            ShouldApply = (InteractionMatcherDelegate)((wi, blockSelection, entitySelection) => true)
          },
        };
      }));
  }
  public override void OnEntityInside(IWorldAccessor world, Vintagestory.API.Common.Entities.Entity entity, BlockPos pos)
  {
    if (world.Rand.NextDouble() < 0.05)
    {
      BlockEntityOven blockEntity = this.GetBlockEntity<BlockEntityOven>(pos);
      // ISSUE: explicit non-virtual call
      if ((blockEntity != null ? (blockEntity.IsBurning? 1 : 0) : 0) != 0)
        entity.ReceiveDamage(new DamageSource()
        {
          Source = EnumDamageSource.Block,
          SourceBlock = (Vintagestory.API.Common.Block)this,
          Type = EnumDamageType.Fire,
          SourcePos = pos.ToVec3d()
        }, 0.5f);
    }

    base.OnEntityInside(world, entity, pos);
  }
  
  public override bool OnBlockInteractStart(
    IWorldAccessor world,
    IPlayer byPlayer,
    BlockSelection blockSel)
  {
    if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
      return false;
    ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
    if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityOven blockEntity)
    {
      if (blockEntity != null && itemstack != null && byPlayer.Entity.Controls.ShiftKey)
      {
        if (itemstack.Collectible.CombustibleProps != null &&
            itemstack.Collectible.CombustibleProps.MeltingPoint > 0)
        {
          ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, (EnumModifierKey)0,
            EnumMergePriority.DirectMerge, 1);
          byPlayer.InventoryManager.ActiveHotbarSlot?.TryPutInto(blockEntity.inputSlot, ref op);
          if (op.MovedQuantity > 0)
          {
            if (byPlayer is IClientPlayer clientPlayer)
              clientPlayer.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            return true;
          }
        }

        if (itemstack.Collectible.CombustibleProps != null &&
            itemstack.Collectible.CombustibleProps.BurnTemperature > 0)
        {
          ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, (EnumModifierKey)0,
            EnumMergePriority.DirectMerge, 1);
          if (op.MovedQuantity > 0)
          {
            if (byPlayer is IClientPlayer clientPlayer)
              clientPlayer.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            JsonObject itemAttributes = itemstack.ItemAttributes;
            AssetLocation assetLocation =
              (itemAttributes != null ? (itemAttributes["placeSound"].Exists ? 1 : 0) : 0) != 0
                ? AssetLocation.Create(itemstack.ItemAttributes["placeSound"].AsString(),
                  itemstack.Collectible.Code.Domain)
                : (AssetLocation)null;
            if (assetLocation != (AssetLocation)null)
              this.api.World.PlaySoundAt(assetLocation.WithPathPrefixOnce("sounds/"), (double)blockSel.Position.X,
                (double)blockSel.Position.Y, (double)blockSel.Position.Z, byPlayer,
                (float)(0.8799999952316284 + this.api.World.Rand.NextDouble() * 0.23999999463558197), 16f);
            return true;
          }
        }
      }

      if (itemstack != null)
      {
        // ISSUE: explicit non-virtual call
        bool? nullable = itemstack.Collectible.Attributes?.IsTrue("mealContainer");
        bool flag = true;
        if (nullable.GetValueOrDefault() == flag & nullable.HasValue)
        {
          ItemSlot potslot = (ItemSlot)null;
          if (blockEntity?.inputStack?.Collectible is BlockCookedContainer)
            potslot = blockEntity.inputSlot;
          if (blockEntity?.outputStack?.Collectible is BlockCookedContainer)
            potslot = blockEntity.outputSlot;
          if (potslot != null)
          {
            BlockCookedContainer collectible = potslot.Itemstack.Collectible as BlockCookedContainer;
            ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (byPlayer.InventoryManager.ActiveHotbarSlot.StackSize > 1)
            {
              ItemSlot bowlSlot = (ItemSlot)new DummySlot(activeHotbarSlot.TakeOut(1));
              byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
              collectible.ServeIntoStack(bowlSlot, potslot, world);
              if (!byPlayer.InventoryManager.TryGiveItemstack(bowlSlot.Itemstack, true))
                world.SpawnItemEntity(bowlSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
            }
            else
              collectible.ServeIntoStack(activeHotbarSlot, potslot, world);
          }
          else if (!blockEntity.inputSlot.Empty ||
                   byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(this.api.World, blockEntity.inputSlot) ==
                   0)
            blockEntity.OnPlayerRightClick(byPlayer, blockSel);

          return true;
        }
      }
      

      return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    if (itemstack == null)
      return false;
    if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
      byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
    return true;
  }

  public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
    base.OnNeighbourBlockChange(world, pos, neibpos);

    if (world.BlockAccessor.GetBlockEntity(pos) is Electricity.Content.Block.Entity.Cable entity) {
      var blockFacing = BlockFacing.FromVector(neibpos.X - pos.X, neibpos.Y - pos.Y, neibpos.Z - pos.Z);
      var selectedFacing = FacingHelper.FromFace(blockFacing);

      if ((entity.Connection & ~ selectedFacing) == Facing.None) {
        world.BlockAccessor.BreakBlock(pos, null);

        return;
      }
                
      var selectedConnection = entity.Connection & selectedFacing;

      if (selectedConnection != Facing.None) {
        var stackSize = FacingHelper.Count(selectedConnection);

        if (stackSize > 0) {
          entity.Connection &= ~selectedConnection;
        }
      }
    }
  }

  public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSelection, ItemStack byItemStack) {
    var selection = new Selection(blockSelection);
    var facing = FacingHelper.From(selection.Face, selection.Direction);

    /* update existing cable */
    {
      if (world.BlockAccessor.GetBlockEntity(blockSelection.Position) is Electricity.Content.Block.Entity.Cable entity) {
        if ((entity.Connection & facing) != 0) {
          return false;
        }

        entity.Connection |= facing;

        return true;
      }
    }

    if (base.DoPlaceBlock(world, byPlayer, blockSelection, byItemStack)) {
      if (world.BlockAccessor.GetBlockEntity(blockSelection.Position) is Electricity.Content.Block.Entity.Cable entity) {
        entity.Connection = facing;
      }

      return true;
    }

    return false;
  }
    
  public override WorldInteraction[] GetPlacedBlockInteractionHelp(
    IWorldAccessor world,
    BlockSelection selection,
    IPlayer forPlayer)
  {
    return this.interactions.Append<WorldInteraction>(
      base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
  }
}