#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

#endregion

namespace Imperium.Util;

internal abstract class ImpAssets
{
    /*
     * UI Prefabs
     */
    internal static GameObject ImperiumDockObject;
    internal static GameObject ImperiumTooltipObject;

    internal static GameObject ImperiumUIObject;
    internal static GameObject MapUIObject;
    internal static GameObject OracleUIObject;
    internal static GameObject SpawningUIObject;

    internal static GameObject LayerSelectorObject;
    internal static GameObject ComponentManagerObject;
    internal static GameObject MinimapOverlayObject;
    internal static GameObject MinimapSettingsObject;

    internal static GameObject ControlCenterWindowObject;
    internal static GameObject ObjectExplorerWindowObject;
    internal static GameObject ObjectControlWindowObject;
    internal static GameObject ConfirmationWindowObject;
    internal static GameObject InfoWindowObject;
    internal static GameObject EventLogWindowObject;
    internal static GameObject MoonControlWindowObject;
    internal static GameObject ShipControlWindowObject;
    internal static GameObject CruiserControlWindowObject;
    internal static GameObject RenderingWindowObject;
    internal static GameObject SaveEditorWindowObject;
    internal static GameObject TeleportationWindowObject;
    internal static GameObject VisualizationWindowObject;
    internal static GameObject PreferencesWindowObject;

    /*
     * Materials
     */
    internal static Material HologramOkay;
    internal static Material HologramOkayDark;
    internal static Material HologramError;
    internal static Material FresnelWhite;
    internal static Material FresnelBlue;
    internal static Material FresnelYellow;
    internal static Material FresnelGreen;
    internal static Material FresnelRed;
    internal static Material WireframeNavMesh;
    internal static Material WireframePurple;
    internal static Material WireframeCyan;
    internal static Material WireframeAmaranth;
    internal static Material WireframeYellow;
    internal static Material WireframeGreen;
    internal static Material WireframeRed;
    internal static Material XRay;

    /*
     * Other Prefabs
     */
    internal static GameObject PositionIndicatorObject;
    internal static GameObject TapeIndicatorObject;
    internal static GameObject DoorMarkerObject;
    internal static GameObject NoiseOverlay;
    internal static GameObject SpawnTimerObject;
    internal static GameObject SpikeTrapTimerObject;
    internal static GameObject SpawnIndicator;
    internal static GameObject ObjectInsightPanel;
    internal static GameObject BuildInfoPanel;

    /*
     * Audio Clips
     */
    internal static AudioClip ButtonClick;
    internal static AudioClip OpenClick;

    /*
     * Materials
     */
    public static Material ShiggyMaterial;
    public static Material TriggerMaterial;

    /*
     * Other
     */
    public static IReadOnlyCollection<string> EntityNames;

    internal static AssetBundle ImperiumAssets;

    internal static bool Load()
    {
        if (!LoadEntityNames())
        {
            Imperium.IO.LogError("[INIT] Failed to load entity names from assembly, aborting!");
            return false;
        }

        if (!LoadAssets())
        {
            Imperium.IO.LogInfo("[INIT] Failed to load one or more assets from assembly, aborting!");
            return false;
        }

        return true;
    }

    private static bool LoadEntityNames()
    {
        using (var entityNamesStream = LoadResource("Imperium.resources.entityNames.txt"))
        {
            EntityNames = new StreamReader(entityNamesStream).ReadToEnd().Split("\n").Select(name => name.Trim()).ToList();
        }

        return EntityNames != null && EntityNames.Count != 0;
    }

    private static bool LoadAssets()
    {
        using (var assetBundleStream = LoadResource("Imperium.resources.assets.imperium_assets"))
        {
            ImperiumAssets = AssetBundle.LoadFromStream(assetBundleStream);
        }

        if (ImperiumAssets == null)
        {
            Imperium.IO.LogError("[INIT] Failed to load assets from assembly, aborting!");
            return false;
        }

        logBuffer = [];
        List<bool> loadResults =
        [
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/imperium_dock.prefab", out ImperiumDockObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/tooltip.prefab", out ImperiumTooltipObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/imperium_ui.prefab", out ImperiumUIObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/layer_selector.prefab", out LayerSelectorObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/component_manager.prefab", out ComponentManagerObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/map_ui.prefab", out MapUIObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/minimap.prefab", out MinimapOverlayObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/minimap_settings.prefab", out MinimapSettingsObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/oracle_ui.prefab", out OracleUIObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/spawning_ui.prefab", out SpawningUIObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/confirmation.prefab", out ConfirmationWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/control_center.prefab", out ControlCenterWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/info.prefab", out InfoWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/event_log.prefab", out EventLogWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/moon_control.prefab", out MoonControlWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/object_explorer.prefab", out ObjectExplorerWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/object_control.prefab", out ObjectControlWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/preferences.prefab", out PreferencesWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/rendering.prefab", out RenderingWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/save_editor.prefab", out SaveEditorWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/ship_control.prefab", out ShipControlWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/cruiser_control.prefab", out CruiserControlWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/teleportation.prefab", out TeleportationWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/UI/Windows/visualization.prefab", out VisualizationWindowObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/tape_indicator.prefab", out TapeIndicatorObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/position_indicator.prefab", out PositionIndicatorObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/door_marker.prefab", out DoorMarkerObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/spawn_timer.prefab", out SpawnTimerObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/spiketrap_timer.prefab", out SpikeTrapTimerObject),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/insight_panel.prefab", out ObjectInsightPanel),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/prop_info.prefab", out BuildInfoPanel),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/spawn_indicator.prefab", out SpawnIndicator),
            LoadAsset(ImperiumAssets, "Assets/Prefabs/noise_overlay.prefab", out NoiseOverlay),
            LoadAsset(ImperiumAssets, "Assets/Materials/xray.mat", out XRay),
            LoadAsset(ImperiumAssets, "Assets/Materials/hologram_okay.mat", out HologramOkay),
            LoadAsset(ImperiumAssets, "Assets/Materials/hologram_okay_dark.mat", out HologramOkayDark),
            LoadAsset(ImperiumAssets, "Assets/Materials/hologram_error.mat", out HologramError),
            LoadAsset(ImperiumAssets, "Assets/Materials/fresnel_white.mat", out FresnelWhite),
            LoadAsset(ImperiumAssets, "Assets/Materials/fresnel_blue.mat", out FresnelBlue),
            LoadAsset(ImperiumAssets, "Assets/Materials/fresnel_red.mat", out FresnelRed),
            LoadAsset(ImperiumAssets, "Assets/Materials/fresnel_green.mat", out FresnelGreen),
            LoadAsset(ImperiumAssets, "Assets/Materials/fresnel_yellow.mat", out FresnelYellow),
            LoadAsset(ImperiumAssets, "Assets/Materials/wireframe_navmesh.mat", out WireframeNavMesh),
            LoadAsset(ImperiumAssets, "Assets/Materials/wireframe_purple.mat", out WireframePurple),
            LoadAsset(ImperiumAssets, "Assets/Materials/wireframe_cyan.mat", out WireframeCyan),
            LoadAsset(ImperiumAssets, "Assets/Materials/wireframe_amaranth.mat", out WireframeAmaranth),
            LoadAsset(ImperiumAssets, "Assets/Materials/wireframe_yellow.mat", out WireframeYellow),
            LoadAsset(ImperiumAssets, "Assets/Materials/wireframe_green.mat", out WireframeGreen),
            LoadAsset(ImperiumAssets, "Assets/Materials/wireframe_red.mat", out WireframeRed),
            LoadAsset(ImperiumAssets, "Assets/Materials/shig.mat", out ShiggyMaterial),
            LoadAsset(ImperiumAssets, "Assets/Audio/ButtonClick.wav", out ButtonClick),
            LoadAsset(ImperiumAssets, "Assets/Audio/OpenClick.ogg", out OpenClick)
        ];

        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (material.name == "testTriggerRed") TriggerMaterial = material;
        }

        Imperium.IO.LogBlock(logBuffer, "Imperium Resource Loader");

        return loadResults.All(result => result);
    }

    private static readonly Dictionary<string, Sprite> spriteCache = new();

    internal static Sprite LoadSpriteFromFiles(string spriteName)
    {
        var spritePath = new[]
            {
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "images",
                $"{spriteName}.png"
            }
            .Aggregate(Path.Combine);

        if (spriteCache.TryGetValue(spritePath, out var sprite)) return sprite;

        if (File.Exists(spritePath))
        {
            var fileData = File.ReadAllBytes(spritePath);
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                spriteCache[spriteName] = sprite;
                return sprite;
            }
        }

        return null;
    }

    private static List<string> logBuffer = [];

    private static bool LoadAsset<T>(AssetBundle assets, string path, out T loadedObject) where T : Object
    {
        loadedObject = assets.LoadAsset<T>(path);
        if (!loadedObject)
        {
            Imperium.IO.LogError($"[INIT] Object '{path}' missing from the embedded Imperium asset bundle.");
            return false;
        }

        logBuffer.Add($"> Successfully loaded {path.Split("/").Last()} from asset bundle.");

        return true;
    }

    private static Stream LoadResource(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
}