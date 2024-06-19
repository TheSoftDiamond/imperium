#region

using System;
using System.Collections.Generic;
using System.Linq;
using Imperium.Interface.Common;
using Imperium.MonoBehaviours.ImpUI.VisualizationUI.ObjectVisualizerEntries;
using Imperium.Types;
using Imperium.Util.Binding;
using Imperium.Visualizers.MonoBehaviours;
using UnityEngine;

#endregion

namespace Imperium.Interface.ImperiumUI.Windows.Visualization.Widgets;

internal class ObjectVisualizers : ImpWidget
{
    private GameObject insightTemplate;
    private GameObject playerTemplate;
    private GameObject entityTemplate;

    private Transform insightsList;
    private Transform playerList;
    private Transform entityList;

    private readonly Dictionary<string, ObjectVisualizerInsightEntry> insightEntries = [];
    private readonly Dictionary<int, ObjectVisualizerPlayerEntry> playerEntries = [];
    private readonly Dictionary<int, ObjectVisualizerEntityEntry> entityEntries = [];

    protected override void InitWidget()
    {
        insightsList = transform.Find("Insights/Content/Viewport/Content");
        playerList = transform.Find("Players/Content/Viewport/Content");
        entityList = transform.Find("Entities/Content/Viewport/Content");

        insightTemplate = insightsList.Find("Item").gameObject;
        insightTemplate.SetActive(false);

        playerTemplate = playerList.Find("Item").gameObject;
        playerTemplate.SetActive(false);

        entityTemplate = entityList.Find("Item").gameObject;
        entityTemplate.SetActive(false);

        ImpButton.Bind(
            "PlayersHeader/Icons/NoiseRange",
            transform,
            () => TogglePlayerConfigs(config => config.NoiseRange),
            theme: theme,
            isIconButton: true
        );

        ImpButton.Bind(
            "EntitiesHeader/Icons/Pathfinding",
            transform,
            () => ToggleEntityConfigs(config => config.Pathfinding),
            theme: theme,
            isIconButton: true
        );
        ImpButton.Bind(
            "EntitiesHeader/Icons/Targeting",
            transform,
            () => ToggleEntityConfigs(config => config.Targeting),
            theme: theme,
            isIconButton: true
        );
        ImpButton.Bind(
            "EntitiesHeader/Icons/LineOfSight",
            transform,
            () => ToggleEntityConfigs(config => config.LineOfSight),
            theme: theme,
            isIconButton: true
        );
        ImpButton.Bind(
            "EntitiesHeader/Icons/Hearing",
            transform,
            () => ToggleEntityConfigs(config => config.Hearing),
            theme: theme,
            isIconButton: true
        );
        ImpButton.Bind(
            "EntitiesHeader/Icons/Custom",
            transform,
            () => ToggleEntityConfigs(config => config.Custom),
            theme: theme,
            isIconButton: true
        );

        Imperium.Visualization.ObjectInsights.InsightVisibilityBindings.onTrigger += Refresh;
        Imperium.ObjectManager.CurrentPlayers.onTrigger += Refresh;
    }

    protected override void OnThemeUpdate(ImpTheme themeUpdate)
    {
        ImpThemeManager.Style(
            themeUpdate,
            transform,
            new StyleOverride("Insights", Variant.DARKER),
            new StyleOverride("Insights/Content/Scrollbar", Variant.DARKEST),
            new StyleOverride("Insights/Content/Scrollbar/SlidingArea/Handle", Variant.LIGHTER),
            new StyleOverride("Players", Variant.DARKER),
            new StyleOverride("Players/Content/Scrollbar", Variant.DARKEST),
            new StyleOverride("Players/Content/Scrollbar/SlidingArea/Handle", Variant.LIGHTER),
            new StyleOverride("Entities", Variant.DARKER),
            new StyleOverride("Entities/Content/Scrollbar", Variant.DARKEST),
            new StyleOverride("Entities/Content/Scrollbar/SlidingArea/Handle", Variant.LIGHTER)
        );
    }

    private static void TogglePlayerConfigs(Func<PlayerGizmoConfig, ImpBinding<bool>> configGetter)
    {
        var total = Imperium.Visualization.PlayerGizmos.PlayerInfoConfigs.Count;
        var activated = Imperium.Visualization.PlayerGizmos.PlayerInfoConfigs.Values.Count(
            config => configGetter(config).Value
        );

        // Set all active if at least half are inactive and vice-versa
        var setActive = activated < total / 2;
        foreach (var playerInfoConfig in Imperium.Visualization.PlayerGizmos.PlayerInfoConfigs.Values)
        {
            configGetter(playerInfoConfig).Set(setActive);
        }
    }

    private static void ToggleEntityConfigs(Func<EntityGizmoConfig, ImpBinding<bool>> configGetter)
    {
        var total = Imperium.Visualization.EntityGizmos.EntityInfoConfigs.Count;
        var activated = Imperium.Visualization.EntityGizmos.EntityInfoConfigs.Values.Count(
            config => configGetter(config).Value
        );

        // Set all active if at least half are inactive and vice-versa
        var setActive = activated < total / 2;
        foreach (var entityInfoConfig in Imperium.Visualization.EntityGizmos.EntityInfoConfigs.Values)
        {
            configGetter(entityInfoConfig).Set(setActive);
        }
    }

    public void Refresh()
    {
        foreach (var playerEntry in playerEntries.Values) Destroy(playerEntry.gameObject);
        playerEntries.Clear();

        foreach (var entityEntry in entityEntries.Values) Destroy(entityEntry.gameObject);
        entityEntries.Clear();

        foreach (var objectEntry in insightEntries.Values) Destroy(objectEntry.gameObject);
        insightEntries.Clear();

        foreach (var player in Imperium.ObjectManager.CurrentPlayers.Value)
        {
            if (playerEntries.ContainsKey(player.GetInstanceID())) continue;

            var playerEntryObject = Instantiate(playerTemplate, playerList);
            playerEntryObject.SetActive(true);

            var playerEntry = playerEntryObject.AddComponent<ObjectVisualizerPlayerEntry>();
            playerEntry.Init(Imperium.Visualization.PlayerGizmos.PlayerInfoConfigs[player], theme);

            playerEntries[player.GetInstanceID()] = playerEntry;
        }

        foreach (var entity in Resources.FindObjectsOfTypeAll<EnemyType>().OrderBy(enemy => enemy.enemyName))
        {
            if (entityEntries.ContainsKey(entity.GetInstanceID())) continue;

            var entityEntryObject = Instantiate(entityTemplate, entityList);
            entityEntryObject.SetActive(true);

            var entityEntry = entityEntryObject.AddComponent<ObjectVisualizerEntityEntry>();
            entityEntry.Init(Imperium.Visualization.EntityGizmos.EntityInfoConfigs[entity], theme);

            entityEntries[entity.GetInstanceID()] = entityEntry;
        }

        foreach (var (objectType, objectConfig) in Imperium.Visualization.ObjectInsights.InsightVisibilityBindings.Value)
        {
            var insightEntryObject = Instantiate(insightTemplate, insightsList);
            insightEntryObject.SetActive(true);

            var insightEntry = insightEntryObject.AddComponent<ObjectVisualizerInsightEntry>();
            insightEntry.Init(objectType.Name, objectConfig, theme);

            insightEntries[objectType.FullName ?? objectType.Name] = insightEntry;
        }

        // Register custom object entry
        var customInsightEntryObject = Instantiate(insightTemplate, insightsList);
        customInsightEntryObject.SetActive(true);

        var customInsightEntry = customInsightEntryObject.AddComponent<ObjectVisualizerInsightEntry>();
        customInsightEntry.Init("Custom", Imperium.Visualization.ObjectInsights.CustomInsights, theme);

        insightEntries["Special.CustomType"] = customInsightEntry;
    }
}