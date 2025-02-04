#region

using System.Linq;
using Imperium.Core;
using Imperium.Types;
using Imperium.Util;
using Imperium.Util.Binding;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Imperium.Interface.Common;

/// <summary>
///     Represents a toggle in the Imperium UI, supports two types of structures
///     Toggle (Toggle)
///     - Background (Image)
///     - Checkmark (Image)
///     - Text (TMP_Text)
///     Toggle (Toggle, Image)
///     - Checkmark (Image)
/// </summary>
public abstract class ImpToggle
{
    /// <summary>
    ///     Binds a unity toggle with an ImpBinding and interactiveBindings
    /// </summary>
    /// <param name="path"></param>
    /// <param name="container"></param>
    /// <param name="valueBinding">Binding that decides on the state of the toggle</param>
    /// <param name="theme">The theme the button will use</param>
    /// <param name="playClickSound">Whether the click sound playes when the button is clicked.</param>
    /// <param name="tooltipDefinition">The tooltip definition of the toggle tooltip.</param>
    /// <param name="interactableBindings">List of bindings that decide if the button is interactable</param>
    internal static Toggle Bind(
        string path,
        Transform container,
        IBinding<bool> valueBinding,
        IBinding<ImpTheme> theme = null,
        bool playClickSound = true,
        TooltipDefinition tooltipDefinition = null,
        params ImpBinding<bool>[] interactableBindings
    )
    {
        var toggleObject = container.Find(path);
        if (!toggleObject)
        {
            Imperium.IO.LogInfo($"[UI] Failed to bind toggle '{Debugging.GetTransformPath(container)}/{path}'");
            return null;
        }

        var toggle = toggleObject.GetComponent<Toggle>();
        var checkmark = toggleObject.Find("Background/Checkmark")?.GetComponent<Image>()
                        ?? toggleObject.Find("Checkmark").GetComponent<Image>();
        var text = (toggleObject.Find("Text") ?? toggleObject.Find("Text (TMP)"))?.GetComponent<TMP_Text>();

        toggle.isOn = valueBinding.Value;

        var interactable = toggleObject.gameObject.AddComponent<ImpInteractable>();
        interactable.onClick += () =>
        {
            if (!toggle.interactable) return;
            valueBinding.Set(!valueBinding.Value);
        };
        valueBinding.onUpdate += value => toggle.isOn = value;

        // Only play the click sound when the update was invoked by the local client
        valueBinding.onUpdateFromLocal += _ =>
        {
            if (Imperium.Settings.Preferences.PlaySounds.Value && playClickSound)
            {
                GameUtils.PlayClip(ImpAssets.ButtonClick);
            }
        };

        if (interactableBindings.Length > 0)
        {
            var isOn = interactableBindings.All(entry => entry.Value);
            ToggleInteractable(checkmark, text, toggle, isOn);

            foreach (var interactableBinding in interactableBindings)
            {
                interactableBinding.onUpdate += value => ToggleInteractable(checkmark, text, toggle, value);
            }
        }

        if (tooltipDefinition != null)
        {
            if (!tooltipDefinition.Tooltip)
            {
                var togglePath = $"{Debugging.GetTransformPath(container)}/{path}";
                Imperium.IO.LogWarning(
                    $"[UI] Failed to initialize tooltip for '{togglePath}'. No tooltip provided."
                );
            }

            interactable.onOver += position => tooltipDefinition.Tooltip.SetPosition(
                tooltipDefinition.Title,
                tooltipDefinition.Description,
                position,
                tooltipDefinition.HasAccess
            );
            interactable.onExit += () => tooltipDefinition.Tooltip.Deactivate();
        }

        if (theme != null)
        {
            theme.onUpdate += value => OnThemeUpdate(value, toggleObject);
            OnThemeUpdate(theme.Value, toggleObject);
        }

        return toggle;
    }

    private static void OnThemeUpdate(ImpTheme theme, Transform container)
    {
        if (!container) return;

        ImpThemeManager.Style(
            theme,
            container,
            new StyleOverride("", Variant.FOREGROUND),
            new StyleOverride("Background", Variant.FOREGROUND),
            new StyleOverride("Checkmark", Variant.LIGHTER),
            new StyleOverride("Background/Checkmark", Variant.LIGHTER)
        );
    }

    private static void ToggleInteractable(
        Image checkmark,
        [CanBeNull] TMP_Text text,
        Selectable toggle,
        bool isOn
    )
    {
        toggle.interactable = isOn;
        if (checkmark) ImpUtils.Interface.ToggleImageActive(checkmark, isOn);
        if (text) ImpUtils.Interface.ToggleTextActive(text, isOn);
    }
}