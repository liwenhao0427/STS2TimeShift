using DevDeckTools.Scripts.Commands;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevDeckTools.Scripts;

public partial class DevMenuController : CanvasLayer
{
    private const Key ToggleKey = Key.F8;
    private bool _menuVisible;

    private Control? _root;
    private PanelContainer? _panel;
    private Label? _runStateLabel;
    private Label? _statusLabel;

    private LineEdit? _addCardIdInput;
    private OptionButton? _addCardSuggestion;
    private SpinBox? _addCardCountInput;
    private SpinBox? _addCardUpgradeInput;
    private Button? _addCardButton;

    private LineEdit? _removeCardIdInput;
    private OptionButton? _removeCardSuggestion;
    private SpinBox? _removeCardCountInput;
    private Button? _removeCardButton;

    private LineEdit? _addRelicIdInput;
    private OptionButton? _addRelicSuggestion;
    private Button? _addRelicButton;

    private LineEdit? _removeRelicIdInput;
    private OptionButton? _removeRelicSuggestion;
    private Button? _removeRelicButton;

    private Button? _presetButton;

    public override void _Ready()
    {
        base._Ready();
        BuildUi();
        SetMenuVisible(false);
        ProcessMode = ProcessModeEnum.Always;
        Log.Info("[DevDeckTools] Dev menu controller ready");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == ToggleKey)
        {
            SetMenuVisible(!_menuVisible);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_menuVisible && @event is InputEventKey)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildUi()
    {
        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Stop;

        ColorRect backdrop = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.35f)
        };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(backdrop);

        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _panel.OffsetLeft = -560;
        _panel.OffsetTop = 28;
        _panel.OffsetRight = -24;
        _panel.OffsetBottom = 640;

        VBoxContainer layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 10);
        layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        MarginContainer marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_left", 16);
        marginContainer.AddThemeConstantOverride("margin_right", 16);
        marginContainer.AddThemeConstantOverride("margin_top", 14);
        marginContainer.AddThemeConstantOverride("margin_bottom", 14);
        marginContainer.AddChild(layout);

        Label title = new Label
        {
            Text = "Dev Deck Tools - 开发者菜单",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        layout.AddChild(title);

        _runStateLabel = new Label
        {
            Text = "运行态检测中..."
        };
        layout.AddChild(_runStateLabel);

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "输入 ID 后可直接回车执行；候选框支持快速选择。"
        };
        layout.AddChild(_statusLabel);

        layout.AddChild(BuildAddCardSection());
        layout.AddChild(BuildRemoveCardSection());
        layout.AddChild(BuildAddRelicSection());
        layout.AddChild(BuildRemoveRelicSection());
        layout.AddChild(BuildPresetSection());

        _panel.AddChild(marginContainer);
        _root.AddChild(_panel);
        AddChild(_root);

        RefreshAllSuggestions();
        RefreshRunStateUi();
    }

    private void SetMenuVisible(bool visible)
    {
        _menuVisible = visible;
        if (_root != null)
        {
            _root.Visible = visible;
        }

        if (visible)
        {
            RefreshRunStateUi();
            RefreshAllSuggestions();
            if (_addCardIdInput != null)
            {
                _addCardIdInput.GrabFocus();
            }

            if (_root != null)
            {
                NHotkeyManager.Instance?.AddBlockingScreen(_root);
            }
        }
        else if (_root != null)
        {
            NHotkeyManager.Instance?.RemoveBlockingScreen(_root);
        }

        Log.Info($"[DevDeckTools] Menu visible={visible}");
    }

    private Control BuildAddCardSection()
    {
        VBoxContainer section = BuildSection("添加卡牌");

        _addCardIdInput = new LineEdit
        {
            PlaceholderText = "卡牌 ID（例如 BODY_SLAM）"
        };
        _addCardIdInput.TextChanged += _ => RefreshCardAddSuggestions();
        _addCardIdInput.TextSubmitted += _ => OnAddCardPressed();
        section.AddChild(_addCardIdInput);

        _addCardSuggestion = new OptionButton();
        _addCardSuggestion.ItemSelected += index => OnSuggestionSelected(_addCardSuggestion, _addCardIdInput, index);
        section.AddChild(_addCardSuggestion);

        HBoxContainer controls = new HBoxContainer();
        controls.AddThemeConstantOverride("separation", 8);

        _addCardCountInput = BuildSpinBox(1, 99, 1);
        controls.AddChild(BuildLabeledControl("数量", _addCardCountInput));

        _addCardUpgradeInput = BuildSpinBox(0, 10, 0);
        controls.AddChild(BuildLabeledControl("升级", _addCardUpgradeInput));

        _addCardButton = new Button
        {
            Text = "添加到卡组"
        };
        _addCardButton.Pressed += OnAddCardPressed;
        controls.AddChild(_addCardButton);

        section.AddChild(controls);
        return section;
    }

    private Control BuildRemoveCardSection()
    {
        VBoxContainer section = BuildSection("删除卡牌");

        _removeCardIdInput = new LineEdit
        {
            PlaceholderText = "卡牌 ID（按实例删除）"
        };
        _removeCardIdInput.TextChanged += _ => RefreshCardRemoveSuggestions();
        _removeCardIdInput.TextSubmitted += _ => OnRemoveCardPressed();
        section.AddChild(_removeCardIdInput);

        _removeCardSuggestion = new OptionButton();
        _removeCardSuggestion.ItemSelected += index => OnSuggestionSelected(_removeCardSuggestion, _removeCardIdInput, index);
        section.AddChild(_removeCardSuggestion);

        HBoxContainer controls = new HBoxContainer();
        controls.AddThemeConstantOverride("separation", 8);

        _removeCardCountInput = BuildSpinBox(1, 99, 1);
        controls.AddChild(BuildLabeledControl("数量", _removeCardCountInput));

        _removeCardButton = new Button
        {
            Text = "从卡组删除"
        };
        _removeCardButton.Pressed += OnRemoveCardPressed;
        controls.AddChild(_removeCardButton);

        section.AddChild(controls);
        return section;
    }

    private Control BuildAddRelicSection()
    {
        VBoxContainer section = BuildSection("添加遗物");

        _addRelicIdInput = new LineEdit
        {
            PlaceholderText = "遗物 ID（例如 BURNING_BLOOD）"
        };
        _addRelicIdInput.TextChanged += _ => RefreshRelicAddSuggestions();
        _addRelicIdInput.TextSubmitted += _ => OnAddRelicPressed();
        section.AddChild(_addRelicIdInput);

        _addRelicSuggestion = new OptionButton();
        _addRelicSuggestion.ItemSelected += index => OnSuggestionSelected(_addRelicSuggestion, _addRelicIdInput, index);
        section.AddChild(_addRelicSuggestion);

        _addRelicButton = new Button
        {
            Text = "添加遗物"
        };
        _addRelicButton.Pressed += OnAddRelicPressed;
        section.AddChild(_addRelicButton);
        return section;
    }

    private Control BuildRemoveRelicSection()
    {
        VBoxContainer section = BuildSection("移除遗物");

        _removeRelicIdInput = new LineEdit
        {
            PlaceholderText = "遗物 ID（仅移除已持有）"
        };
        _removeRelicIdInput.TextChanged += _ => RefreshRelicRemoveSuggestions();
        _removeRelicIdInput.TextSubmitted += _ => OnRemoveRelicPressed();
        section.AddChild(_removeRelicIdInput);

        _removeRelicSuggestion = new OptionButton();
        _removeRelicSuggestion.ItemSelected += index => OnSuggestionSelected(_removeRelicSuggestion, _removeRelicIdInput, index);
        section.AddChild(_removeRelicSuggestion);

        _removeRelicButton = new Button
        {
            Text = "移除遗物"
        };
        _removeRelicButton.Pressed += OnRemoveRelicPressed;
        section.AddChild(_removeRelicButton);
        return section;
    }

    private Control BuildPresetSection()
    {
        VBoxContainer section = BuildSection("调试预设");
        _presetButton = new Button
        {
            Text = "应用快速预设"
        };
        _presetButton.Pressed += OnPresetPressed;
        section.AddChild(_presetButton);

        Label hint = new Label
        {
            Text = "快速预设：补充 3 张基础测试卡并尝试添加 1 个可用遗物。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        section.AddChild(hint);
        return section;
    }

    private static VBoxContainer BuildSection(string title)
    {
        VBoxContainer section = new VBoxContainer();
        section.AddThemeConstantOverride("separation", 6);

        Label header = new Label
        {
            Text = $"[{title}]"
        };
        section.AddChild(header);
        return section;
    }

    private static SpinBox BuildSpinBox(double min, double max, double value)
    {
        SpinBox spinBox = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Value = value,
            Rounded = true,
            CustomMinimumSize = new Vector2(96, 0)
        };
        return spinBox;
    }

    private static Control BuildLabeledControl(string labelText, Control control)
    {
        VBoxContainer wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 2);

        Label label = new Label
        {
            Text = labelText
        };
        wrapper.AddChild(label);
        wrapper.AddChild(control);
        return wrapper;
    }

    private void RefreshRunStateUi()
    {
        bool canExecute = DevDeckCommandService.CanExecute(out string reason);
        if (_runStateLabel != null)
        {
            _runStateLabel.Text = canExecute
                ? "运行态：可执行（已连接到本地玩家）"
                : $"运行态：不可执行（{reason}）";
            _runStateLabel.Modulate = canExecute ? Colors.LightGreen : Colors.OrangeRed;
        }

        if (_addCardButton != null)
        {
            _addCardButton.Disabled = !canExecute;
        }

        if (_removeCardButton != null)
        {
            _removeCardButton.Disabled = !canExecute;
        }

        if (_addRelicButton != null)
        {
            _addRelicButton.Disabled = !canExecute;
        }

        if (_removeRelicButton != null)
        {
            _removeRelicButton.Disabled = !canExecute;
        }

        if (_presetButton != null)
        {
            _presetButton.Disabled = !canExecute;
        }
    }

    private void RefreshAllSuggestions()
    {
        RefreshCardAddSuggestions();
        RefreshCardRemoveSuggestions();
        RefreshRelicAddSuggestions();
        RefreshRelicRemoveSuggestions();
    }

    private void RefreshCardAddSuggestions()
    {
        if (_addCardSuggestion == null || _addCardIdInput == null)
        {
            return;
        }

        IReadOnlyList<string> suggestions = DevDeckCommandService.GetCardIdSuggestions(_addCardIdInput.Text, 18);
        ApplySuggestionItems(_addCardSuggestion, suggestions);
    }

    private void RefreshCardRemoveSuggestions()
    {
        if (_removeCardSuggestion == null || _removeCardIdInput == null)
        {
            return;
        }

        IReadOnlyList<string> suggestions = DevDeckCommandService.GetCardIdSuggestions(_removeCardIdInput.Text, 18);
        ApplySuggestionItems(_removeCardSuggestion, suggestions);
    }

    private void RefreshRelicAddSuggestions()
    {
        if (_addRelicSuggestion == null || _addRelicIdInput == null)
        {
            return;
        }

        IReadOnlyList<string> suggestions = DevDeckCommandService.GetRelicIdSuggestions(_addRelicIdInput.Text, 18);
        ApplySuggestionItems(_addRelicSuggestion, suggestions);
    }

    private void RefreshRelicRemoveSuggestions()
    {
        if (_removeRelicSuggestion == null || _removeRelicIdInput == null)
        {
            return;
        }

        IReadOnlyList<string> suggestions = DevDeckCommandService.GetRelicIdSuggestions(_removeRelicIdInput.Text, 18);
        ApplySuggestionItems(_removeRelicSuggestion, suggestions);
    }

    private static void ApplySuggestionItems(OptionButton optionButton, IReadOnlyList<string> items)
    {
        optionButton.Clear();
        foreach (string item in items)
        {
            optionButton.AddItem(item);
        }

        optionButton.Disabled = items.Count == 0;
    }

    private static void OnSuggestionSelected(OptionButton? optionButton, LineEdit? lineEdit, long index)
    {
        if (optionButton == null || lineEdit == null || index < 0 || index >= optionButton.ItemCount)
        {
            return;
        }

        lineEdit.Text = optionButton.GetItemText((int)index);
        lineEdit.CaretColumn = lineEdit.Text.Length;
    }

    private void OnAddCardPressed()
    {
        TaskHelper.RunSafely(ExecuteAddCardAsync());
    }

    private async Task ExecuteAddCardAsync()
    {
        RefreshRunStateUi();
        if (_addCardIdInput == null || _addCardCountInput == null || _addCardUpgradeInput == null)
        {
            return;
        }

        int count = (int)_addCardCountInput.Value;
        int upgrade = (int)_addCardUpgradeInput.Value;
        DevDeckCommandService.DevCommandResult result = await DevDeckCommandService.AddCardToDeckAsync(_addCardIdInput.Text, count, upgrade);
        SetStatus(result.Message, result.Success);
    }

    private void OnRemoveCardPressed()
    {
        TaskHelper.RunSafely(ExecuteRemoveCardAsync());
    }

    private async Task ExecuteRemoveCardAsync()
    {
        RefreshRunStateUi();
        if (_removeCardIdInput == null || _removeCardCountInput == null)
        {
            return;
        }

        int count = (int)_removeCardCountInput.Value;
        DevDeckCommandService.DevCommandResult result = await DevDeckCommandService.RemoveCardsFromDeckAsync(_removeCardIdInput.Text, count);
        SetStatus(result.Message, result.Success);
    }

    private void OnAddRelicPressed()
    {
        TaskHelper.RunSafely(ExecuteAddRelicAsync());
    }

    private async Task ExecuteAddRelicAsync()
    {
        RefreshRunStateUi();
        if (_addRelicIdInput == null)
        {
            return;
        }

        DevDeckCommandService.DevCommandResult result = await DevDeckCommandService.AddRelicAsync(_addRelicIdInput.Text);
        SetStatus(result.Message, result.Success);
    }

    private void OnRemoveRelicPressed()
    {
        TaskHelper.RunSafely(ExecuteRemoveRelicAsync());
    }

    private async Task ExecuteRemoveRelicAsync()
    {
        RefreshRunStateUi();
        if (_removeRelicIdInput == null)
        {
            return;
        }

        DevDeckCommandService.DevCommandResult result = await DevDeckCommandService.RemoveRelicAsync(_removeRelicIdInput.Text);
        SetStatus(result.Message, result.Success);
    }

    private void OnPresetPressed()
    {
        TaskHelper.RunSafely(ExecutePresetAsync());
    }

    private async Task ExecutePresetAsync()
    {
        RefreshRunStateUi();
        DevDeckCommandService.DevCommandResult result = await DevDeckCommandService.ApplyQuickPresetAsync();
        SetStatus(result.Message, result.Success);
    }

    private void SetStatus(string message, bool success)
    {
        if (_statusLabel == null)
        {
            return;
        }

        _statusLabel.Text = message;
        _statusLabel.Modulate = success ? Colors.LightGreen : Colors.OrangeRed;
        Log.Info($"[DevDeckTools] {message}");
    }
}
