#region

using System.Linq;
using Imperium.API.Types.Networking;
using Imperium.Netcode;
using Imperium.Util;
using Imperium.Util.Binding;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

#endregion

namespace Imperium.Core.Lifecycle;

internal class MoonManager : ImpLifecycleObject
{
    private readonly ImpNetMessage<ChangeWeatherRequest> changeWeatherMessage = new("ChangeWeather", Imperium.Networking);

    internal MoonManager(ImpBinaryBinding sceneLoaded, IBinding<int> playersConnected)
        : base(sceneLoaded, playersConnected)
    {
        changeWeatherMessage.OnClientRecive += OnWeatherChange;
    }

    internal void ChangeWeather(ChangeWeatherRequest request) => changeWeatherMessage.DispatchToClients(request);

    internal readonly ImpNetworkBinding<bool> IndoorSpawningPaused = new(
        "IndoorSpawningPaused",
        Imperium.Networking,
        onUpdateClient: value =>
        {
            Imperium.IO.Send(
                value ? "Indoor spawning has been paused!" : "Indoor spawning has been resumed!",
                type: NotificationType.Confirmation
            );
        }
    );

    internal readonly ImpNetworkBinding<bool> OutdoorSpawningPaused = new(
        "OutdoorSpawningPaused",
        Imperium.Networking,
        onUpdateClient: value =>
        {
            Imperium.IO.Send(
                value ? "Outdoor spawning has been paused!" : "Outdoor spawning has been resumed!",
                type: NotificationType.Confirmation
            );
        }
    );

    internal readonly ImpNetworkBinding<bool> DaytimeSpawningPaused = new(
        "DaytimeSpawningPaused",
        Imperium.Networking,
        onUpdateClient: value =>
        {
            Imperium.IO.Send(
                value ? "Daytime spawning has been paused!" : "Daytime spawning has been resumed!",
                type: NotificationType.Confirmation
            );
        }
    );

    internal readonly ImpNetworkBinding<bool> TimeIsPaused = new(
        "TimeIsPaused",
        Imperium.Networking
    );

    internal readonly ImpNetworkBinding<float> TimeSpeed = new(
        "TimeSpeed",
        Imperium.Networking,
        ImpConstants.DefaultTimeSpeed
    );

    internal readonly ImpNetworkBinding<float> MaxIndoorPower = new(
        "MaxIndoorPower",
        Imperium.Networking,
        Imperium.RoundManager.currentLevel.maxEnemyPowerCount,
        onUpdateClient: value =>
        {
            if (Imperium.IsSceneLoaded.Value) Imperium.RoundManager.currentLevel.maxEnemyPowerCount = (int)value;
            Imperium.RoundManager.currentMaxInsidePower = value;
        });

    internal readonly ImpNetworkBinding<float> MaxOutdoorPower = new(
        "MaxOutdoorPower",
        Imperium.Networking,
        Imperium.RoundManager.currentLevel.maxOutsideEnemyPowerCount,
        onUpdateClient: value =>
        {
            if (Imperium.IsSceneLoaded.Value) Imperium.RoundManager.currentLevel.maxOutsideEnemyPowerCount = (int)value;
            Imperium.RoundManager.currentMaxOutsidePower = value;
        });

    internal readonly ImpNetworkBinding<int> MaxDaytimePower = new(
        "MaxDaytimePower",
        Imperium.Networking,
        Imperium.RoundManager.currentLevel.maxDaytimeEnemyPowerCount,
        onUpdateClient: value => Imperium.RoundManager.currentLevel.maxDaytimeEnemyPowerCount = value
    );

    internal readonly ImpNetworkBinding<float> IndoorDeviation = new(
        "IndoorDeviation",
        Imperium.Networking,
        Imperium.RoundManager.currentLevel.spawnProbabilityRange,
        onUpdateClient: value => Imperium.RoundManager.currentLevel.spawnProbabilityRange = value
    );

    internal readonly ImpNetworkBinding<float> DaytimeDeviation = new(
        "DaytimeDeviation",
        Imperium.Networking,
        Imperium.RoundManager.currentLevel.daytimeEnemiesProbabilityRange,
        onUpdateClient: value => Imperium.RoundManager.currentLevel.daytimeEnemiesProbabilityRange = value
    );

    internal readonly ImpNetworkBinding<int> MinIndoorSpawns = new(
        "MinIndoorSpawns",
        Imperium.Networking,
        Imperium.RoundManager.minEnemiesToSpawn,
        onUpdateClient: value => Imperium.RoundManager.minEnemiesToSpawn = value
    );

    internal readonly ImpNetworkBinding<int> MinOutdoorSpawns = new(
        "MinOutdoorSpawns",
        Imperium.Networking,
        Imperium.RoundManager.minOutsideEnemiesToSpawn,
        onUpdateClient: value => Imperium.RoundManager.minOutsideEnemiesToSpawn = value
    );

    internal readonly ImpNetworkBinding<float> WeatherVariable1 = new(
        "WeatherVariable1",
        Imperium.Networking,
        Imperium.TimeOfDay.currentWeatherVariable,
        onUpdateClient: value =>
        {
            if (Imperium.RoundManager.currentLevel.currentWeather == LevelWeatherType.Eclipsed)
            {
                Imperium.RoundManager.minEnemiesToSpawn = (int)value;
                Imperium.RoundManager.minOutsideEnemiesToSpawn = (int)value;
            }

            Imperium.TimeOfDay.currentWeatherVariable = value;
        }
    );

    internal readonly ImpNetworkBinding<float> WeatherVariable2 = new(
        "WeatherVariable2",
        Imperium.Networking,
        Imperium.TimeOfDay.currentWeatherVariable,
        onUpdateClient: value => Imperium.TimeOfDay.currentWeatherVariable = value
    );

    protected override void OnSceneLoad()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        MaxIndoorPower.Sync(Imperium.RoundManager.currentMaxInsidePower);
        MaxOutdoorPower.Sync(Imperium.RoundManager.currentMaxOutsidePower);
        MaxDaytimePower.Sync(Imperium.RoundManager.currentLevel.maxDaytimeEnemyPowerCount);

        IndoorDeviation.Sync(Imperium.RoundManager.currentLevel.spawnProbabilityRange);
        DaytimeDeviation.Sync(Imperium.RoundManager.currentLevel.daytimeEnemiesProbabilityRange);

        MinIndoorSpawns.Sync(Imperium.RoundManager.minEnemiesToSpawn);
        MinOutdoorSpawns.Sync(Imperium.RoundManager.minOutsideEnemiesToSpawn);

        WeatherVariable1.Sync(Imperium.TimeOfDay.currentWeatherVariable);
        WeatherVariable2.Sync(Imperium.TimeOfDay.currentWeatherVariable2);
    }

    [ImpAttributes.LocalMethod]
    private static void OnWeatherChange(ChangeWeatherRequest request)
    {
        Imperium.StartOfRound.levels[request.LevelIndex].currentWeather = request.WeatherType;

        RefreshWeather();

        var planetName = Imperium.StartOfRound.levels[request.LevelIndex].PlanetName;
        var weatherName = request.WeatherType.ToString();
        Imperium.IO.Send($"Successfully changed the weather on {planetName} to {weatherName}",
            type: NotificationType.Confirmation);
    }

    [ImpAttributes.LocalMethod]
    private static void RefreshWeather()
    {
        Reflection.Invoke(Imperium.RoundManager, "SetToCurrentLevelWeather");
        Imperium.StartOfRound.SetMapScreenInfoToCurrentLevel();
        for (var i = 0; i < Imperium.TimeOfDay.effects.Length; i++)
        {
            var weatherEffect = Imperium.TimeOfDay.effects[i];
            var isEnabled = (int)Imperium.StartOfRound.currentLevel.currentWeather == i;
            weatherEffect.effectEnabled = isEnabled;
            if (weatherEffect.effectPermanentObject)
            {
                weatherEffect.effectPermanentObject.SetActive(value: isEnabled);
            }

            if (weatherEffect.effectObject)
            {
                weatherEffect.effectObject.SetActive(value: isEnabled);
            }

            if (Imperium.TimeOfDay.sunAnimator)
            {
                if (isEnabled && !string.IsNullOrEmpty(weatherEffect.sunAnimatorBool))
                {
                    Imperium.TimeOfDay.sunAnimator.SetBool(weatherEffect.sunAnimatorBool, value: true);
                }
                else
                {
                    Imperium.TimeOfDay.sunAnimator.Rebind();
                    Imperium.TimeOfDay.sunAnimator.Update(0);
                }
            }
        }

        // This prevents the player from permanently getting stuck in the underwater effect when turning
        // off flooded weather while being underwater
        if (Imperium.StartOfRound.currentLevel.currentWeather != LevelWeatherType.Flooded)
        {
            Imperium.Player.isUnderwater = false;
            Imperium.Player.sourcesCausingSinking = Mathf.Clamp(Imperium.Player.sourcesCausingSinking - 1, 0, 100);
            Imperium.Player.isMovementHindered = Mathf.Clamp(Imperium.Player.isMovementHindered - 1, 0, 100);
            Imperium.Player.hinderedMultiplier = 1;
        }
    }

    internal static void NavigateTo(int levelIndex)
    {
        Imperium.StartOfRound.ChangeLevelServerRpc(levelIndex, Imperium.Terminal.groupCredits);

        // Send scene refresh so moon related data is refreshed
        Imperium.IsSceneLoaded.Refresh();
    }

    internal static void PlayClip(AudioClip audioClip, bool randomize = false)
    {
        RoundManager.PlayRandomClip(Imperium.HUDManager.UIAudio, [audioClip], randomize);
    }

    internal static void ToggleDoors(bool isOpen)
    {
        Imperium.ObjectManager.CurrentLevelDoors.Value
            .Where(obj => obj)
            .ToList()
            .ForEach(door =>
            {
                var animation = door.gameObject.GetComponent<AnimatedObjectTrigger>();
                animation.TriggerAnimation(Imperium.Player);

                Reflection.Set(door, "isDoorOpened", isOpen);
                Reflection.Get<DoorLock, NavMeshObstacle>(door, "isDoorOpened").enabled = !isOpen;
            });
    }

    internal static void ToggleDoorLocks(bool isLocked)
    {
        Imperium.ObjectManager.CurrentLevelDoors.Value
            .Where(obj => obj)
            .ToList()
            .ForEach(door =>
            {
                if (isLocked && !door.isLocked)
                {
                    door.LockDoor();
                }
                else if (door.isLocked)
                {
                    door.UnlockDoor();
                }
            });
    }

    internal static void ToggleSecurityDoors(bool isOn)
    {
        Imperium.ObjectManager.CurrentLevelSecurityDoors.Value
            .Where(obj => obj)
            .ToList()
            .ForEach(door => door.OnPowerSwitch(!isOn));
    }

    internal static void ToggleTurrets(bool isOn)
    {
        Imperium.ObjectManager.CurrentLevelTurrets.Value
            .Where(obj => obj)
            .ToList()
            .ForEach(turret => turret.ToggleTurretEnabled(isOn));
    }

    internal static void ToggleLandmines(bool isOn)
    {
        Imperium.ObjectManager.CurrentLevelLandmines.Value
            .Where(obj => obj)
            .ToList()
            .ForEach(mine => mine.ToggleMine(isOn));
    }

    internal static void ToggleBreakers(bool isOn)
    {
        Imperium.ObjectManager.CurrentLevelBreakerBoxes.Value
            .Where(obj => obj)
            .ToList()
            .ForEach(box => ToggleBreaker(box, isOn));
    }

    internal static void ToggleBreaker(BreakerBox box, bool isOn)
    {
        foreach (var breakerSwitch in box.breakerSwitches)
        {
            var animation = breakerSwitch.gameObject.GetComponent<AnimatedObjectTrigger>();
            if (animation.boolValue != isOn)
            {
                animation.boolValue = isOn;
                animation.setInitialState = isOn;
                // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
                breakerSwitch.SetBool("turnedLeft", isOn);
                box.SwitchBreaker(isOn);
            }
        }
    }
}