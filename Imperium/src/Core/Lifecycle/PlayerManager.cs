#region

using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Imperium.API.Types.Networking;
using Imperium.Netcode;
using Imperium.Util;
using Imperium.Util.Binding;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

#endregion

namespace Imperium.Core.Lifecycle;

internal class PlayerManager : ImpLifecycleObject
{
    internal readonly ImpBinaryBinding IsFlying = new(false);

    internal readonly ImpNetworkBinding<HashSet<ulong>> untargetablePlayers = new(
        "UntargetablePlayers", Imperium.Networking, []
    );

    internal readonly ImpNetworkBinding<HashSet<ulong>> invisiblePlayers = new(
        "InvisiblePlayers", Imperium.Networking, []
    );

    internal readonly IBinding<bool> PlayerInCruiser = new ImpBinding<bool>(false);

    private readonly ImpNetMessage<ulong> killPlayerMessage = new("KillPlayer", Imperium.Networking);
    private readonly ImpNetMessage<ulong> revivePlayerMessage = new("RevivePlayer", Imperium.Networking);
    private readonly ImpNetMessage<DropItemRequest> dropItemMessage = new("Dropitem", Imperium.Networking);

    private readonly ImpNetMessage<TeleportPlayerRequest> teleportPlayerMessage = new(
        "TeleportPlayer", Imperium.Networking
    );

    private static readonly Dictionary<int, Vector2> CameraOriginalResolutions = [];

    internal readonly ImpExternalBinding<Vector3?, bool> ShipTPAnchor = new(
        () => GameObject.Find("CatwalkShip")?.transform.position
    );

    internal readonly ImpExternalBinding<Vector3?, bool> MainEntranceTPAnchor = new(
        () => GameObject.Find("EntranceTeleportA")?.transform.position
    );

    internal readonly ImpExternalBinding<Vector3?, bool> ApparatusTPAnchor = new(
        () => GameObject.Find("LungApparatus(Clone)")?.transform.position
    );

    internal bool AllowPlayerDeathOverride;
    internal bool FlyIsAscending;
    internal bool FlyIsDescending;

    private static readonly int GasEmitting = Animator.StringToHash("gasEmitting");

    protected override void Init()
    {
        dropItemMessage.OnClientRecive += OnDropitemClient;
        killPlayerMessage.OnClientRecive += OnKillPlayerClient;
        revivePlayerMessage.OnClientRecive += OnRevivePlayerClient;
        teleportPlayerMessage.OnClientRecive += OnTeleportPlayerClient;

        if (NetworkManager.Singleton.IsHost)
        {
            dropItemMessage.OnServerReceive += OnDropItemServer;
            killPlayerMessage.OnServerReceive += OnKillPlayerServer;
            revivePlayerMessage.OnServerReceive += OnRevivePlayerServer;
            teleportPlayerMessage.OnServerReceive += OnTeleportPlayerServer;
        }

        Imperium.Settings.Player.Invisibility.onUpdate += isInvisible =>
        {
            ImpUtils.Bindings.ToggleSet(invisiblePlayers, Imperium.Player.actualClientId, isInvisible);
        };

        Imperium.Settings.Player.Untargetable.onUpdate += isUntargetable =>
        {
            ImpUtils.Bindings.ToggleSet(untargetablePlayers, Imperium.Player.actualClientId, isUntargetable);
        };
    }

    protected override void OnSceneLoad()
    {
        ShipTPAnchor.Refresh();
        MainEntranceTPAnchor.Refresh();
        ApparatusTPAnchor.Refresh();
    }

    [ImpAttributes.RemoteMethod]
    internal void KillPlayer(ulong playerId) => killPlayerMessage.DispatchToServer(playerId);

    [ImpAttributes.RemoteMethod]
    internal void RevivePlayer(ulong playerId) => revivePlayerMessage.DispatchToServer(playerId);

    [ImpAttributes.RemoteMethod]
    internal void TeleportPlayer(TeleportPlayerRequest request) => teleportPlayerMessage.DispatchToServer(request);

    [ImpAttributes.RemoteMethod]
    internal void TeleportLocalPlayer(Vector3 position) => TeleportPlayer(new TeleportPlayerRequest
    {
        PlayerId = NetworkManager.Singleton.LocalClientId,
        Destination = position
    });

    [ImpAttributes.RemoteMethod]
    internal void DropItem(DropItemRequest request) => dropItemMessage.DispatchToClients(request);

    [ImpAttributes.LocalMethod]
    internal static void GrabObject(GrabbableObject grabbableItem, PlayerControllerB player)
    {
        NetworkObjectReference networkObject = grabbableItem.NetworkObject;

        player.carryWeight += Mathf.Clamp(grabbableItem.itemProperties.weight - 1f, 0f, 10f);
        player.GrabObjectServerRpc(networkObject);

        grabbableItem.parentObject = player.localItemHolder;
        grabbableItem.GrabItemOnClient();
    }

    internal static int GetItemHolderSlot(GrabbableObject grabbableObject)
    {
        if (!grabbableObject.playerHeldBy || !grabbableObject.playerHeldBy.currentlyHeldObjectServer) return -1;

        for (var i = 0; i < grabbableObject.playerHeldBy.ItemSlots.Length; i++)
        {
            if (grabbableObject.playerHeldBy.ItemSlots[i] == grabbableObject)
            {
                return i;
            }
        }

        throw new ArgumentOutOfRangeException();
    }

    [ImpAttributes.LocalMethod]
    internal static void RestoreLocalPlayerHealth(PlayerControllerB player)
    {
        player.health = 100;
        HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);
        HUDManager.Instance.gasHelmetAnimator.SetBool(GasEmitting, value: false);

        player.bleedingHeavily = false;
        player.criticallyInjured = false;
    }

    internal static void UpdateCameras(bool _) => UpdateCameras();

    internal static void UpdateCameras()
    {
        foreach (var camera in FindObjectsOfType<Camera>())
        {
            if (camera.gameObject.name == "MapCamera" || !camera.targetTexture) continue;

            var targetTexture = camera.targetTexture;

            if (!CameraOriginalResolutions.TryGetValue(targetTexture.GetInstanceID(), out var originalResolution))
            {
                originalResolution = new Vector2(targetTexture.width, targetTexture.height);
                CameraOriginalResolutions[targetTexture.GetInstanceID()] = originalResolution;
            }

            targetTexture.Release();
            targetTexture.width = Mathf.RoundToInt(
                originalResolution.x * Imperium.Settings.Rendering.ResolutionMultiplier.Value
            );
            targetTexture.height = Mathf.RoundToInt(
                originalResolution.y * Imperium.Settings.Rendering.ResolutionMultiplier.Value
            );
            targetTexture.Create();
        }

        Resources.UnloadUnusedAssets();

        foreach (var camera in FindObjectsByType<HDAdditionalCameraData>(FindObjectsSortMode.None))
        {
            if (camera.gameObject.name == "MapCamera") continue;

            camera.customRenderingSettings = true;

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.DecalLayers] = Imperium.Settings.Rendering.DecalLayers.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.DecalLayers, Imperium.Settings.Rendering.DecalLayers.Value
            );

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.SSGI] = Imperium.Settings.Rendering.SSGI.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.SSGI, Imperium.Settings.Rendering.SSGI.Value
            );

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.RayTracing] = Imperium.Settings.Rendering.RayTracing.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.RayTracing, Imperium.Settings.Rendering.RayTracing.Value
            );

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.VolumetricClouds] = Imperium.Settings.Rendering.VolumetricClouds.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.VolumetricClouds, Imperium.Settings.Rendering.VolumetricClouds.Value
            );

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.SubsurfaceScattering] = Imperium.Settings.Rendering.SSS.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.SubsurfaceScattering, Imperium.Settings.Rendering.SSS.Value
            );

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.ReprojectionForVolumetrics] = Imperium.Settings.Rendering.VolumeReprojection.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.ReprojectionForVolumetrics, Imperium.Settings.Rendering.VolumeReprojection.Value
            );

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.TransparentPrepass] = Imperium.Settings.Rendering.TransparentPrepass.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.TransparentPrepass, Imperium.Settings.Rendering.TransparentPrepass.Value
            );

            camera.renderingPathCustomFrameSettingsOverrideMask.mask
                [(int)FrameSettingsField.TransparentPostpass] = Imperium.Settings.Rendering.TransparentPostpass.Value;
            camera.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.TransparentPostpass, Imperium.Settings.Rendering.TransparentPostpass.Value
            );
        }
    }

    internal static Camera GetActiveCamera()
    {
        return Imperium.Freecam.IsFreecamEnabled.Value
            ? Imperium.Freecam.FreecamCamera
            : Imperium.Player.hasBegunSpectating
                ? Imperium.StartOfRound.spectateCamera
                : Imperium.Player.gameplayCamera;
    }

    #region RPC Handlers

    [ImpAttributes.LocalMethod]
    private static void OnDropitemClient(DropItemRequest request)
    {
        var player =  Imperium.StartOfRound.allPlayerScripts.First(player => player.actualClientId == request.PlayerId);
        var previousSlot = player.currentItemSlot;

        // Switch to item slot, discard item and switch back
        player.SwitchToItemSlot(request.ItemIndex);
        player.DiscardHeldObject();
        player.SwitchToItemSlot(previousSlot);
    }

    [ImpAttributes.HostOnly]
    private void OnDropItemServer(DropItemRequest request, ulong clientId)
    {
        dropItemMessage.DispatchToClients(request);
    }

    [ImpAttributes.HostOnly]
    private void OnTeleportPlayerServer(TeleportPlayerRequest request, ulong clientId)
    {
        teleportPlayerMessage.DispatchToClients(request);
    }

    [ImpAttributes.LocalMethod]
    private static void OnTeleportPlayerClient(TeleportPlayerRequest request)
    {
        var player = Imperium.StartOfRound.allPlayerScripts.First(player => player.actualClientId == request.PlayerId);

        player.TeleportPlayer(request.Destination);
        var isInFactory = request.Destination.y < -100;
        player.isInsideFactory = isInFactory;

        // There is no easy way to check this, so it will just be off by default for now
        var isInElevator = Imperium.StartOfRound.shipBounds.bounds.Contains(request.Destination);
        player.isInElevator = isInElevator;

        var isInShip = Imperium.StartOfRound.shipInnerRoomBounds.bounds.Contains(request.Destination);
        player.isInHangarShipRoom = isInShip;

        foreach (var heldItem in player.ItemSlots)
        {
            if (!heldItem) continue;
            heldItem.isInFactory = isInFactory;
            heldItem.isInShipRoom = isInShip;
            heldItem.isInFactory = isInFactory;
        }

        if (request.PlayerId == NetworkManager.Singleton.LocalClientId) TimeOfDay.Instance.DisableAllWeather();
    }

    [ImpAttributes.HostOnly]
    private void OnKillPlayerServer(ulong playerId, ulong clientId) => killPlayerMessage.DispatchToClients(playerId);

    [ImpAttributes.LocalMethod]
    private void OnKillPlayerClient(ulong playerId)
    {
        if (playerId == NetworkManager.Singleton.LocalClientId)
        {
            AllowPlayerDeathOverride = true;
            Imperium.Player.KillPlayer(Vector3.zero, deathAnimation: 1);
            AllowPlayerDeathOverride = false;
        }
    }

    [ImpAttributes.HostOnly]
    private void OnRevivePlayerServer(ulong playerId, ulong clientId) => revivePlayerMessage.DispatchToClients(playerId);

    [ImpAttributes.LocalMethod]
    private static void OnRevivePlayerClient(ulong playerId)
    {
        var player =Imperium.StartOfRound.allPlayerScripts.First(player => player.actualClientId == playerId);

        Imperium.StartOfRound.livingPlayers++;

        // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
        if (player.playerBodyAnimator) player.playerBodyAnimator.SetBool("Limp", value: false);
        // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
        HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: false);
        // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
        HUDManager.Instance.gameOverAnimator.SetTrigger("revive");

        player.isClimbingLadder = false;
        player.thisController.enabled = true;
        player.health = 100;
        player.carryWeight = 1;
        player.disableLookInput = false;
        player.isPlayerDead = false;
        player.isPlayerControlled = true;
        player.isInElevator = true;
        player.isInHangarShipRoom = true;
        player.isInsideFactory = false;
        player.parentedToElevatorLastFrame = false;
        player.setPositionOfDeadPlayer = false;
        player.criticallyInjured = false;
        player.bleedingHeavily = false;
        player.activatingItem = false;
        player.twoHanded = false;
        player.inSpecialInteractAnimation = false;
        player.disableSyncInAnimation = false;
        player.inAnimationWithEnemy = null;
        player.holdingWalkieTalkie = false;
        player.speakingToWalkieTalkie = false;
        player.isSinking = false;
        player.isUnderwater = false;
        player.sinkingValue = 0f;
        player.hasBegunSpectating = false;
        player.hinderedMultiplier = 1f;
        player.isMovementHindered = 0;
        player.sourcesCausingSinking = 0;
        player.spectatedPlayerScript = null;
        player.helmetLight.enabled = false;

        player.ResetPlayerBloodObjects(player.isPlayerDead);
        player.ResetZAndXRotation();
        player.TeleportPlayer(Imperium.StartOfRound.shipDoorNode.position);
        player.DisablePlayerModel(player.gameObject, enable: true, disableLocalArms: true);
        player.Crouch(crouch: false);
        player.statusEffectAudio.Stop();
        player.DisableJetpackControlsLocally();
        Imperium.StartOfRound.SetPlayerObjectExtrapolate(enable: false);

        HUDManager.Instance.RemoveSpectateUI();
        HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);

        Imperium.StartOfRound.SetSpectateCameraToGameOverMode(enableGameOver: false, player);

        // Close interface if player has revived themselves
        if (playerId == NetworkManager.Singleton.LocalClientId) Imperium.Interface.Close();

        Imperium.StartOfRound.allPlayersDead = false;
        Imperium.StartOfRound.UpdatePlayerVoiceEffects();
        Imperium.StartOfRound.ResetMiscValues();

        // Respawn UI because for some reason this is not happening already
        Imperium.Settings.Rendering.PlayerHUD.Set(false);
        Imperium.Settings.Rendering.PlayerHUD.Set(true);
    }

    #endregion
}