#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Imperium.Core;
using Imperium.Types;
using Imperium.Util;
using Imperium.Util.Binding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Imperium.Interface.Common;

public class ImpSlider : MonoBehaviour
{
    public Slider Slider { get; private set; }
    private TMP_Text indicatorText;

    private string indicatorUnit;
    private Func<float, string> indicatorFormatter;

    private float debounceTime;
    private Coroutine debounceCoroutine;

    /// <summary>
    ///     Factory method to create and bind an ImpSlider to a valid slider object.
    ///     Required UI Layout:
    ///     Root
    ///     "Reset" (Button?) - Optional reset button
    ///     "Slider" (Slider)
    ///     "SliderArea" (Image)
    ///     "SlideArea"
    ///     "Handle" (Image)
    ///     "Text" (TMP_Text) - Optional indicator text
    /// </summary>
    /// <param name="path">The path to the UI element relative to the parent.</param>
    /// <param name="container">The parent object of the UI elmeent.</param>
    /// <param name="valueBinding">The binding that the value of the slider will be bound to</param>
    /// <param name="theme">The theme the slider will use</param>
    /// <param name="useLogarithmicScale">If the slider uses a logarithmic scale</param>
    /// <param name="indicatorUnit">Slider value unit (e.g. % or degrees)</param>
    /// <param name="indicatorDefaultValue">Overwrites the default value on the slider</param>
    /// <param name="indicatorFormatter">Formatter for custom indicator text</param>
    /// <param name="debounceTime">Debounce time for slider updates</param>
    /// <param name="interactableInvert">Whether the interactable binding values should be inverted</param>
    /// <param name="options">Override options with provided labels</param>
    /// <param name="clickAudio">The audio clip that is played when the slider value is changed.</param>
    /// <param name="playClickSound">Whether the click sound playes when the slider value is changed.</param>
    /// <param name="tooltipDefinition">The tooltip definition of the toggle tooltip.</param>
    /// <param name="interactableBindings">List of boolean bindings that decide if the slider is interactable</param>
    internal static ImpSlider Bind(
        string path,
        Transform container,
        IBinding<float> valueBinding,
        IBinding<ImpTheme> theme = null,
        bool useLogarithmicScale = false,
        string indicatorUnit = "",
        float? indicatorDefaultValue = null,
        Func<float, string> indicatorFormatter = null,
        float debounceTime = 0f,
        bool interactableInvert = false,
        IBinding<List<string>> options = null,
        AudioClip clickAudio = null,
        bool playClickSound = true,
        TooltipDefinition tooltipDefinition = null,
        params IBinding<bool>[] interactableBindings
    )
    {
        var sliderObject = container.Find(path);
        if (!sliderObject)
        {
            Imperium.IO.LogInfo($"[UI] Failed to bind slider '{Debugging.GetTransformPath(container)}/{path}'");
            return null;
        }

        var impSlider = sliderObject.gameObject.AddComponent<ImpSlider>();
        impSlider.debounceTime = debounceTime;
        impSlider.indicatorFormatter = indicatorFormatter;
        impSlider.indicatorUnit = indicatorUnit;
        impSlider.Slider = sliderObject.Find("Slider").GetComponent<Slider>();
        impSlider.indicatorText = sliderObject.Find("Slider/SlideArea/Handle/Text").GetComponent<TMP_Text>();

        indicatorFormatter ??= value => $"{Mathf.RoundToInt(value)}";
        clickAudio ??= ImpAssets.GrassClick;

        var currentValue = useLogarithmicScale ? (float)Math.Log10(valueBinding.Value) : valueBinding.Value;
        impSlider.Slider.value = currentValue;

        if (options is { Value: not null, Value.Count: > 0 })
        {
            impSlider.Slider.minValue = 0;
            impSlider.Slider.maxValue = options.Value.Count - 1;

            impSlider.indicatorText.text = $"{options.Value[(int)valueBinding.Value]}{indicatorUnit}";

            options.onUpdate += newOptions =>
            {
                impSlider.Slider.minValue = 0;
                impSlider.Slider.maxValue = newOptions.Count - 1;

                impSlider.indicatorText.text = $"{newOptions[(int)valueBinding.Value]}{indicatorUnit}";
            };
        }
        else
        {
            impSlider.indicatorText.text = $"{indicatorFormatter(valueBinding.Value)}{indicatorUnit}";
        }

        impSlider.Slider.onValueChanged.AddListener(newValue =>
        {
            // Fixes weird null pointer error after respawning UI
            if (!impSlider) return;

            // Use option label if options are used
            impSlider.indicatorText.text = options is { Value: not null, Value.Count: > 0 }
                ? $"{options.Value[(int)newValue]}{indicatorUnit}"
                : $"{indicatorFormatter(newValue)}{indicatorUnit}";

            var bindingValue = useLogarithmicScale ? (float)Math.Pow(10, newValue) : newValue;

            if (debounceTime > 0)
            {
                if (impSlider.debounceCoroutine != null) impSlider.StopCoroutine(impSlider.debounceCoroutine);
                impSlider.debounceCoroutine = impSlider.StartCoroutine(
                    impSlider.DebounceSlider(valueBinding, bindingValue, clickAudio)
                );
            }
            else
            {
                valueBinding.Set(bindingValue);
                if (playClickSound) GameUtils.PlayClip(clickAudio);
            }
        });

        valueBinding.onUpdate += newValue =>
        {
            impSlider.Slider.value = useLogarithmicScale ? (float)Math.Log10(newValue) : newValue;

            // Use option label if options are used
            impSlider.indicatorText.text = options is { Value: not null, Value.Count: > 0 }
                ? $"{options.Value[(int)newValue]}{indicatorUnit}"
                : $"{indicatorFormatter(newValue)}{indicatorUnit}";
        };

        ImpButton.Bind(
            "Reset", sliderObject, () =>
            {
                var defaultValue = indicatorDefaultValue ?? valueBinding.DefaultValue;

                impSlider.Slider.value = useLogarithmicScale ? (float)Math.Log10(defaultValue) : defaultValue;

                // Use option label if options are used
                impSlider.indicatorText.text = options is { Value: not null, Value.Count: > 0 }
                    ? $"{options.Value[(int)defaultValue]}{indicatorUnit}"
                    : $"{indicatorFormatter(defaultValue)}{indicatorUnit}";

                valueBinding.Reset();
            },
            theme: theme,
            playClickSound: false,
            interactableInvert: interactableInvert,
            interactableBindings: interactableBindings
        );

        if (tooltipDefinition != null)
        {
            var interactable = sliderObject.gameObject.AddComponent<ImpInteractable>();

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

        if (interactableBindings.Length > 0)
        {
            var sliderArea = sliderObject.Find("Slider/SliderArea").GetComponent<Image>();

            ToggleInteractable(
                impSlider.Slider, sliderArea,
                interactableBindings.All(entry => entry.Value), interactableInvert
            );
            foreach (var interactableBinding in interactableBindings)
            {
                interactableBinding.onUpdate += value =>
                {
                    ToggleInteractable(impSlider.Slider, sliderArea, value, interactableInvert);
                };
            }
        }

        if (theme != null)
        {
            theme.onUpdate += value => OnThemeUpdate(value, sliderObject);
            OnThemeUpdate(theme.Value, sliderObject);
        }

        return impSlider;
    }

    private static void OnThemeUpdate(ImpTheme theme, Transform container)
    {
        ImpThemeManager.Style(
            theme,
            container,
            new StyleOverride("Slider/SliderArea", Variant.DARKER),
            new StyleOverride("Slider/SlideArea/Handle", Variant.FOREGROUND)
        );
    }

    private IEnumerator DebounceSlider(IBinding<float> binding, float value, AudioClip clickAudio)
    {
        yield return new WaitForSeconds(debounceTime);
        binding.Set(value);
        GameUtils.PlayClip(clickAudio);
    }

    private void SetIndicatorText(float value)
    {
        indicatorText.text = indicatorFormatter != null
            ? indicatorFormatter(value)
            : $"{Mathf.RoundToInt(value)}{indicatorUnit}";
    }

    public void SetValue(float value)
    {
        Slider.value = value;
        SetIndicatorText(value);
    }

    private static void ToggleInteractable(Selectable input, Image sliderArea, bool isOn, bool inverted)
    {
        input.interactable = inverted ? !isOn : isOn;
        ImpUtils.Interface.ToggleImageActive(sliderArea, !isOn);
    }
}