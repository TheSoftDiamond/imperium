#region

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Imperium.Interface.Common;
using Imperium.Types;
using Imperium.Util;
using Imperium.Util.Binding;
using TMPro;
using Unity.Netcode;

#endregion

namespace Imperium.Interface.ImperiumUI.Windows.MoonControl.Widgets;

public class LevelGeneration : ImpWidget
{
    private TMP_InputField levelSeedInput;
    private TMP_InputField levelMapSizeInput;

    private readonly Dictionary<string, string> dungeonFlowDisplayNames = new()
    {
        { "Level1Flow", "Factory" },
        { "Level1FlowExtraLarge", "Factory (Large)" },
        { "Level1Flow3Exits", "Factory (3 Exits)" },
        { "Level2Flow", "Mansion" },
        { "Level3Flow", "Mineshaft" }
    };

    protected override void InitWidget()
    {
        var disabledBinding = ImpBinaryBinding.CreateOr([
            (Imperium.IsSceneLoaded, false),
            (new ImpBinding<bool>(!NetworkManager.Singleton.IsHost), false)
        ]);

        levelSeedInput = ImpInput.Bind(
            "Seed/Input",
            transform,
            Imperium.GameManager.CustomSeed,
            theme: theme,
            interactableInvert: true,
            negativeIsEmpty: true,
            interactableBindings: disabledBinding
        );
        ImpUtils.Interface.BindInputInteractable(disabledBinding, transform.Find("Seed"), true);

        ImpButton.Bind(
            "Seed/Reset",
            transform,
            () => Imperium.GameManager.CustomSeed.Reset(),
            theme: theme,
            interactableInvert: true,
            interactableBindings: disabledBinding
        );

        levelMapSizeInput = ImpInput.Bind(
            "MapSize/Input",
            transform,
            Imperium.GameManager.CustomMapSize,
            theme: theme,
            interactableInvert: true,
            negativeIsEmpty: true,
            interactableBindings: disabledBinding
        );
        ImpUtils.Interface.BindInputInteractable(disabledBinding, transform.Find("MapSize"), true);

        ImpButton.Bind(
            "MapSize/Reset",
            transform,
            () => Imperium.GameManager.CustomMapSize.Reset(),
            theme: theme,
            interactableInvert: true,
            interactableBindings: disabledBinding
        );

        var options = Imperium.RoundManager.dungeonFlowTypes
            .Select(flow => new TMP_Dropdown.OptionData(
                dungeonFlowDisplayNames.GetValueOrDefault(flow.dungeonFlow.name, flow.dungeonFlow.name)
            ))
            .ToList();

        var dungeonFlowDropdown = transform.Find("DungeonFlow/Dropdown").GetComponent<TMP_Dropdown>();
        dungeonFlowDropdown.options = options;
        dungeonFlowDropdown.value = -1;

        dungeonFlowDropdown.onValueChanged.AddListener(value =>
        {
            Imperium.GameManager.CustomDungeonFlow.Set(value);
        });
        ImpUtils.Interface.BindDropdownInteractable(disabledBinding, transform.Find("DungeonFlow"), true);

        ImpButton.Bind(
            "DungeonFlow/Reset",
            transform,
            () => Imperium.GameManager.CustomDungeonFlow.Reset(),
            theme: theme,
            interactableInvert: true,
            interactableBindings: disabledBinding
        );
    }

    protected override void OnThemeUpdate(ImpTheme themeUpdate)
    {
        ImpThemeManager.Style(
            themeUpdate,
            transform,
            new StyleOverride("DungeonFlow/Dropdown", Variant.FOREGROUND),
            new StyleOverride("DungeonFlow/Dropdown/Arrow", Variant.FOREGROUND),
            new StyleOverride("DungeonFlow/Dropdown/Template", Variant.DARKER),
            new StyleOverride("DungeonFlow/Dropdown/Template/Viewport/Content/Item/Background", Variant.DARKER),
            new StyleOverride("DungeonFlow/Dropdown/Template/Scrollbar", Variant.DARKEST),
            new StyleOverride("DungeonFlow/Dropdown/Template/Scrollbar/SlidingArea/Handle", Variant.LIGHTER)
        );
    }

    protected override void OnOpen()
    {
        levelSeedInput.text = Imperium.IsSceneLoaded.Value
            ? Imperium.StartOfRound.randomMapSeed.ToString()
            : Imperium.GameManager.CustomSeed.Value != -1
                ? Imperium.GameManager.CustomSeed.Value.ToString()
                : "";

        levelMapSizeInput.text = Imperium.GameManager.CustomMapSize.Value > -1
            ? Imperium.GameManager.CustomMapSize.Value.ToString(CultureInfo.InvariantCulture)
            : Imperium.IsSceneLoaded.Value
                ? Imperium.RoundManager.currentLevel.factorySizeMultiplier.ToString(CultureInfo.InvariantCulture)
                : "";
    }
}