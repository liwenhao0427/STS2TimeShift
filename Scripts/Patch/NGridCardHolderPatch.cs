using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace TimeShift.Scripts.Patch;

[HarmonyPatch]
internal static class NGridCardHolderPatch
{
    [HarmonyPatch(typeof(NGridCardHolder), nameof(NGridCardHolder._Ready))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void PostfixOnReady(NCardHolder __instance)
    {
        if (__instance is NGridCardHolder gridHolder)
        {
            Log.Info($"[TimeShift] NGridCardHolder._Ready: {gridHolder.Name}");
            var processPatch = new TimeShiftGridPatch();
            processPatch.Holder = gridHolder;
            gridHolder.AddChild(processPatch);
        }
    }
}

[HarmonyPatch]
internal static class NHandCardHolderPatch
{
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder._Ready))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void PostfixOnReady(NCardHolder __instance)
    {
        if (__instance is NHandCardHolder handHolder)
        {
            Log.Info($"[TimeShift] NHandCardHolder._Ready: {handHolder.Name}");
            var processPatch = new TimeShiftHandPatch();
            processPatch.Holder = handHolder;
            handHolder.AddChild(processPatch);
        }
    }
}

internal partial class TimeShiftGridPatch : Control
{
    public NGridCardHolder? Holder { get; set; }
    private bool _wasShiftPressed = false;
    private bool _initialized = false;
    private CardModel? _baseCard;
    private CardModel? _upgradedCard;
    private bool _isShowingPreview = false;

    public override void _Ready()
    {
        base._Ready();
        MouseFilter = MouseFilterEnum.Ignore;
        _initialized = true;
        Log.Info($"[TimeShift] GridPatch._Ready: {Holder?.Name}");
        
        if (Holder != null)
        {
            var field = Holder.GetType().GetField("_baseCard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _baseCard = field?.GetValue(Holder) as CardModel;
            if (_baseCard?.IsUpgradable == true)
            {
                _upgradedCard = (CardModel)_baseCard.MutableClone();
                _upgradedCard.UpgradeInternal();
                Log.Info($"[TimeShift] Grid init: base={_baseCard.Id}, upgraded={_upgradedCard.Id}, baseIsUpgraded={_baseCard.IsUpgraded}");
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!_initialized || Holder == null || Holder.CardNode == null)
            return;

        if (_baseCard == null || !_baseCard.IsUpgradable)
            return;

        bool shiftPressed = Input.IsKeyPressed(Key.Shift);

        if (shiftPressed != _wasShiftPressed)
        {
            _wasShiftPressed = shiftPressed;
            bool isBaseUpgraded = _baseCard.IsUpgraded;
            Log.Info($"[TimeShift] Grid: shift={shiftPressed}, base={_baseCard.Id}, isBaseUpgraded={isBaseUpgraded}");

            if (shiftPressed)
            {
                if (isBaseUpgraded)
                {
                    Log.Info($"[TimeShift] Grid: 显示基础版（原始已升级）");
                    if (_baseCard != null)
                    {
                        Holder.CardNode.Model = _baseCard;
                        Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
                    }
                    _isShowingPreview = true;
                }
                else
                {
                    Log.Info($"[TimeShift] Grid: 显示升级版");
                    if (_upgradedCard != null)
                    {
                        Holder.CardNode.Model = _upgradedCard;
                        Holder.CardNode.ShowUpgradePreview();
                    }
                    _isShowingPreview = true;
                }
            }
            else
            {
                Log.Info($"[TimeShift] Grid: 恢复原状");
                if (_baseCard != null)
                {
                    Holder.CardNode.Model = _baseCard;
                    Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
                }
                _isShowingPreview = false;
            }
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_isShowingPreview && inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            if (Holder?.CardNode != null && _baseCard != null)
            {
                Log.Info($"[TimeShift] Grid: 点击时恢复原始卡牌");
                Holder.CardNode.Model = _baseCard;
                Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
                _isShowingPreview = false;
                _wasShiftPressed = false;
            }
        }
    }

    public override void _ExitTree()
    {
        Log.Info($"[TimeShift] GridPatch._ExitTree: {Holder?.Name}");
        if (Holder != null && _baseCard != null)
        {
            Holder.CardNode.Model = _baseCard;
            Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
        }
        base._ExitTree();
    }
}

internal partial class TimeShiftHandPatch : Control
{
    public NHandCardHolder? Holder { get; set; }
    private bool _wasShiftPressed = false;
    private bool _initialized = false;
    private CardModel? _baseCard;
    private CardModel? _upgradedCard;
    private bool _isShowingPreview = false;

    public override void _Ready()
    {
        base._Ready();
        MouseFilter = MouseFilterEnum.Ignore;
        _initialized = true;
        Log.Info($"[TimeShift] HandPatch._Ready: {Holder?.Name}");
    }

    private void InitializeCards()
    {
        if (Holder?.CardNode?.Model == null)
            return;

        _baseCard = Holder.CardNode.Model;
        if (_baseCard?.IsUpgradable == true)
        {
            _upgradedCard = (CardModel)_baseCard.MutableClone();
            _upgradedCard.UpgradeInternal();
            Log.Info($"[TimeShift] Hand init: base={_baseCard.Id}, upgraded={_upgradedCard.Id}, baseIsUpgraded={_baseCard.IsUpgraded}");
        }
    }

    public override void _Process(double delta)
    {
        if (!_initialized || Holder == null || Holder.CardNode == null)
            return;

        if (_baseCard == null)
        {
            InitializeCards();
            if (_baseCard == null)
                return;
        }

        if (!_baseCard.IsUpgradable)
            return;

        bool shiftPressed = Input.IsKeyPressed(Key.Shift);

        if (shiftPressed != _wasShiftPressed)
        {
            _wasShiftPressed = shiftPressed;
            bool isBaseUpgraded = _baseCard.IsUpgraded;
            Log.Info($"[TimeShift] Hand: shift={shiftPressed}, base={_baseCard.Id}, isBaseUpgraded={isBaseUpgraded}");

            if (shiftPressed)
            {
                if (isBaseUpgraded)
                {
                    Log.Info($"[TimeShift] Hand: 显示基础版（原始已升级）");
                    if (_baseCard != null)
                    {
                        Holder.CardNode.Model = _baseCard;
                        Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
                    }
                    _isShowingPreview = true;
                }
                else
                {
                    Log.Info($"[TimeShift] Hand: 显示升级版");
                    if (_upgradedCard != null)
                    {
                        Holder.CardNode.Model = _upgradedCard;
                        Holder.CardNode.ShowUpgradePreview();
                    }
                    _isShowingPreview = true;
                }
            }
            else
            {
                Log.Info($"[TimeShift] Hand: 恢复原状");
                if (_baseCard != null)
                {
                    Holder.CardNode.Model = _baseCard;
                    Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
                }
                _isShowingPreview = false;
            }
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_isShowingPreview && inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            if (Holder?.CardNode != null && _baseCard != null)
            {
                Log.Info($"[TimeShift] Hand: 点击时恢复原始卡牌");
                Holder.CardNode.Model = _baseCard;
                Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
                _isShowingPreview = false;
                _wasShiftPressed = false;
            }
        }
    }

    public override void _ExitTree()
    {
        Log.Info($"[TimeShift] HandPatch._ExitTree: isPreview={_isShowingPreview}");
        if (_isShowingPreview && Holder?.CardNode != null && _baseCard != null)
        {
            Holder.CardNode.Model = _baseCard;
            Holder.CardNode.UpdateVisuals(Holder.CardNode.DisplayingPile, CardPreviewMode.Normal);
        }
        base._ExitTree();
    }
}
