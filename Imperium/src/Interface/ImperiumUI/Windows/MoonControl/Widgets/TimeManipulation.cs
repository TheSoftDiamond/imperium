#region

using Imperium.Interface.Common;
using Imperium.Types;
using Imperium.Util;
using Imperium.Util.Binding;
using UnityEngine.UI;

#endregion

namespace Imperium.Interface.ImperiumUI.Windows.MoonControl.Widgets;

public class TimeManipulation : ImpWidget
{
    private readonly IBinding<float> timeOfDayBinding = new ImpBinding<float>();

    protected override void InitWidget()
    {
        var speedDisabled = ImpBinaryBinding.CreateOr([
            (Imperium.IsSceneLoaded, true),
            (Imperium.MoonManager.TimeIsPaused, false)
        ]);

        ImpSlider.Bind(
            path: "TimeSpeed",
            container: transform,
            valueBinding: Imperium.MoonManager.TimeSpeed,
            indicatorFormatter: Formatting.FormatFloatToThreeDigits,
            useLogarithmicScale: true,
            debounceTime: 0.05f,
            theme: theme,
            interactableInvert: true,
            interactableBindings: speedDisabled
        );

        ImpToggle.Bind(
            "TimeSettings/Pause",
            transform,
            Imperium.MoonManager.TimeIsPaused,
            theme
        );

        ImpToggle.Bind("TimeSettings/RealtimeClock", transform, Imperium.Settings.Time.RealtimeClock, theme);
        ImpToggle.Bind("TimeSettings/PermanentClock", transform, Imperium.Settings.Time.PermanentClock, theme);

        var timeSlider = ImpSlider.Bind(
            path: "TimeContainer/Time",
            container: transform,
            valueBinding: timeOfDayBinding,
            indicatorFormatter: value => Formatting.FormatTime(Formatting.TimeToNormalized(value)),
            debounceTime: 0.02f,
            theme: theme,
            interactableBindings: Imperium.IsSceneLoaded
        );
        timeSlider.Slider.minValue = TimeOfDay.startingGlobalTime;
        timeSlider.Slider.maxValue = Imperium.TimeOfDay.totalTime;

        var timeSliderIcon = timeSlider.Slider.transform.Find("SlideArea/Handle/Icon").GetComponent<Image>();
        timeOfDayBinding.onUpdate += value =>
        {
            Imperium.TimeOfDay.globalTime = value;

            var normalizedTime = Imperium.TimeOfDay.globalTime / Imperium.TimeOfDay.totalTime;
            timeSliderIcon.sprite = Imperium.HUDManager.clockIcons[(int)Imperium.TimeOfDay.GetDayPhase(normalizedTime)];
        };
    }

    protected override void OnOpen()
    {
        timeOfDayBinding.Set(Imperium.TimeOfDay.globalTime, invokeSecondary: false);
    }

    protected override void OnThemeUpdate(ImpTheme themeUpdate)
    {
        ImpThemeManager.Style(
            themeUpdate,
            transform,
            new StyleOverride("TimeContainer/Time", Variant.DARKER)
        );
    }
}