using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using WeatherRegistry;

namespace Imperium.Integration;

public static class WeatherRegistryIntegration
{
    internal static bool IsEnabled => Chainloader.PluginInfos.ContainsKey("mrov.WeatherRegistry");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static List<LevelWeatherType> GetWeathers()
    {
        if (!IsEnabled)
            return null;

        List<LevelWeatherType> weatherTypes = [];
        foreach (Weather weather in WeatherRegistry.WeatherManager.Weathers)
        {
            weatherTypes.Add(weather.VanillaWeatherType);
        }

        return weatherTypes;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void ChangeWeather(SelectableLevel level, LevelWeatherType weather)
    {
        if (!IsEnabled)
            return;

        WeatherRegistry.WeatherController.ChangeWeather(level, weather);
    }
}
