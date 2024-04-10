namespace ElectricityExtensions.Content.GUI;

using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Cairo;

public class GuiDialogOven : GuiDialogBlockEntity
{
  private bool haveCookingContainer;
  private string currentOutputText;
  private ElementBounds cookingSlotsSlotBounds;
  private long lastRedrawMs;
  private EnumPosFlag screenPos;

  protected override double FloatyDialogPosition => 0.6;

  protected override double FloatyDialogAlign => 0.8;

  public override double DrawOrder => 0.2;

  public GuiDialogOven(
    string dialogTitle,
    InventoryBase Inventory,
    BlockPos BlockEntityPosition,
    SyncedTreeAttribute tree,
    ICoreClientAPI capi)
    : base(dialogTitle, Inventory, BlockEntityPosition, capi)
  {
    if (this.IsDuplicate)
      return;
    tree.OnModified.Add(new TreeModifiedListener()
    {
      listener = new Action(this.OnAttributesModified)
    });
    this.Attributes = (ITreeAttribute) tree;
  }

  private void OnInventorySlotModified(int slotid) => this.capi.Event.EnqueueMainThreadTask(new Action(this.SetupDialog), "setupfirepitdlg");

  private void SetupDialog()
  {
    ItemSlot itemSlot = this.capi.World.Player.InventoryManager.CurrentHoveredSlot;
    if (itemSlot != null && itemSlot.Inventory?.InventoryID != this.Inventory?.InventoryID)
      itemSlot = (ItemSlot) null;
    string text = this.Attributes.GetString("outputText", "");
    bool flag = this.Attributes.GetInt("haveCookingContainer") > 0;
    if (this.haveCookingContainer == flag && this.SingleComposer != null)
    {
      GuiElementDynamicText dynamicText = this.SingleComposer.GetDynamicText("outputText");
      dynamicText.Font.WithFontSize(14f);
      dynamicText.SetNewText(text, true);
      this.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
      this.haveCookingContainer = flag;
      this.currentOutputText = text;
      dynamicText.Bounds.fixedOffsetY = 0.0;
      if (dynamicText.QuantityTextLines > 2)
      {
        dynamicText.Bounds.fixedOffsetY = -dynamicText.Font.GetFontExtents().Height / (double) RuntimeEnv.GUIScale * 0.65;
        dynamicText.Font.WithFontSize(12f);
        dynamicText.RecomposeText();
      }
      dynamicText.Bounds.CalcWorldBounds();
    }
    else
    {
      this.haveCookingContainer = flag;
      this.currentOutputText = text;
      int length = this.Attributes.GetInt("quantityCookingSlots");
      ElementBounds bounds1 = ElementBounds.Fixed(0.0, 0.0, 210.0, 250.0);
      this.cookingSlotsSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 75.0, 4, length / 4);
      this.cookingSlotsSlotBounds.fixedHeight += 10.0;
      double y = this.cookingSlotsSlotBounds.fixedHeight + this.cookingSlotsSlotBounds.fixedY;
      ElementBounds bounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, y, 1, 1);
      ElementBounds bounds3 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 110.0 + y, 1, 1);
      ElementBounds bounds4 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153.0, y, 1, 1);
      ElementBounds bounds5 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
      bounds5.BothSizing = ElementSizing.FitToChildren;
      bounds5.WithChildren(bounds1);
      ElementBounds bounds6 = ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(this.IsRight(this.screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(this.IsRight(this.screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
      if (!this.capi.Settings.Bool["immersiveMouseMode"])
        bounds6.fixedOffsetY += (bounds1.fixedHeight + 65.0 + (this.haveCookingContainer ? 25.0 : 0.0)) * (double) this.YOffsetMul(this.screenPos);
      int[] selectiveSlots = new int[length];
      for (int index = 0; index < length; ++index)
        selectiveSlots[index] = 3 + index;
      this.SingleComposer = this.capi.Gui.CreateCompo("blockentitystove" + this.BlockEntityPosition?.ToString(), bounds6)
        .AddShadedDialogBG(bounds5).AddDialogTitleBar(this.DialogTitle, new Action(this.OnTitleBarClose)).BeginChildElements(bounds5)
        .AddDynamicCustomDraw(bounds1, new DrawDelegateWithBounds(this.OnBgDraw), "symbolDrawer")
        .AddDynamicText("", CairoFont.WhiteDetailText(), ElementBounds.Fixed(0.0, 30.0, 210.0, 45.0), "outputText")
        .AddIf(this.haveCookingContainer)
        .AddItemSlotGrid((IInventory) this.Inventory, new Action<object>(this.SendInvPacket), 4, selectiveSlots, this.cookingSlotsSlotBounds, "ingredientSlots").EndIf()
        .AddDynamicText("", CairoFont.WhiteDetailText(), bounds3.RightCopy(17.0, 16.0).WithFixedSize(60.0, 30.0), "fueltemp")
        .AddItemSlotGrid((IInventory) this.Inventory, new Action<object>(this.SendInvPacket), 1, new int[1]
      {
        1
      }, bounds2, "oreslot").AddDynamicText("", CairoFont.WhiteDetailText(), bounds2.RightCopy(23.0, 16.0)
        .WithFixedSize(60.0, 30.0), "oretemp")
        .AddItemSlotGrid((IInventory) this.Inventory, new Action<object>(this.SendInvPacket), 1, new int[1]
      {
        2
      }, bounds4, "outputslot").EndChildElements().Compose();
      this.lastRedrawMs = this.capi.ElapsedMilliseconds;
      if (itemSlot != null)
        this.SingleComposer.OnMouseMove(new MouseEvent(this.capi.Input.MouseX, this.capi.Input.MouseY));
      GuiElementDynamicText dynamicText = this.SingleComposer.GetDynamicText("outputText");
      dynamicText.SetNewText(this.currentOutputText, true);
      dynamicText.Bounds.fixedOffsetY = 0.0;
      if (dynamicText.QuantityTextLines > 2)
        dynamicText.Bounds.fixedOffsetY = -dynamicText.Font.GetFontExtents().Height / (double) RuntimeEnv.GUIScale * 0.65;
      dynamicText.Bounds.CalcWorldBounds();
    }
  }

  private void OnAttributesModified()
  {
    if (!this.IsOpened())
      return;
    float num1 = this.Attributes.GetFloat("furnaceTemperature");
    float num2 = this.Attributes.GetFloat("oreTemperature");
    string str1 = num1.ToString("#");
    string str2 = num2.ToString("#");
    string text1 = str1 + (str1.Length > 0 ? "°C" : "");
    string text2 = str2 + (str2.Length > 0 ? "°C" : "");
    if ((double) num1 > 0.0 && (double) num1 <= 20.0)
      text1 = Lang.Get("Cold");
    if ((double) num2 > 0.0 && (double) num2 <= 20.0)
      text2 = Lang.Get("Cold");
    this.SingleComposer.GetDynamicText("fueltemp").SetNewText(text1);
    this.SingleComposer.GetDynamicText("oretemp").SetNewText(text2);
    if (this.capi.ElapsedMilliseconds - this.lastRedrawMs <= 500L)
      return;
    if (this.SingleComposer != null)
      this.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
    this.lastRedrawMs = this.capi.ElapsedMilliseconds;
  }

  private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
  {
    double num1 = this.cookingSlotsSlotBounds.fixedHeight + this.cookingSlotsSlotBounds.fixedY;
    ctx.Save();
    Matrix matrix2 = ctx.Matrix;
    matrix2.Translate(GuiElement.scaled(63.0), GuiElement.scaled(num1 + 2.0));
    matrix2.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
    ctx.Matrix = matrix2;
    this.capi.Gui.Icons.DrawArrowRight(ctx, 2.0);
    double num4 = Attributes.GetFloat("oreCookingTime") / (double) this.Attributes.GetFloat("maxOreCookingTime", 1f);
    ctx.Rectangle(5.0, 0.0, 125.0 * num4, 100.0);
    ctx.Clip();
    LinearGradient source2 = new LinearGradient(0.0, 0.0, 200.0, 0.0);
    source2.AddColorStop(0.0, new Color(0.0, 0.4, 0.0, 1.0));
    source2.AddColorStop(1.0, new Color(0.2, 0.6, 0.2, 1.0));
    ctx.SetSource((Pattern) source2);
    this.capi.Gui.Icons.DrawArrowRight(ctx, 0.0, false, false);
    source2.Dispose();
    ctx.Restore();
  }

  private void SendInvPacket(object packet) => this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, packet);

  private void OnTitleBarClose() => this.TryClose();

  public override void OnGuiOpened()
  {
    base.OnGuiOpened();
    this.Inventory.SlotModified += new Action<int>(this.OnInventorySlotModified);
    this.screenPos = this.GetFreePos("smallblockgui");
    this.OccupyPos("smallblockgui", this.screenPos);
    this.SetupDialog();
    
    // Run Animation "open"
    
  }

  public override void OnGuiClosed()
  {
    this.Inventory.SlotModified -= new Action<int>(this.OnInventorySlotModified);
    this.SingleComposer.GetSlotGrid("oreslot").OnGuiClosed(this.capi);
    this.SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(this.capi);
    this.SingleComposer.GetSlotGrid("ingredientSlots")?.OnGuiClosed(this.capi);
    base.OnGuiClosed();
    this.FreePos("smallblockgui", this.screenPos);
  }
}
