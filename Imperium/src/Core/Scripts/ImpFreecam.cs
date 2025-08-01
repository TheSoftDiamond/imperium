#region

using Imperium.Core.Lifecycle;
using Imperium.Integration;
using Imperium.Interface.LayerSelector;
using Imperium.Util;
using Imperium.Util.Binding;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

#endregion

namespace Imperium.Core.Scripts;

public class ImpFreecam : MonoBehaviour
{
    private Camera gameplayCamera;
    private Vector2 lookInput;
    private LayerSelector layerSelector;

    private static Rect minicamRect => new(100f / Screen.width, 1 - 100f / Screen.height - 0.4f, 0.4f, 0.4f);

    internal Camera FreecamCamera { get; private set; }
    internal readonly ImpBinaryBinding IsFreecamEnabled = new(false);

    private readonly ImpBinaryBinding IsMinicamEnabled = new(false);
    private readonly ImpBinaryBinding IsMinicamFullscreenEnabled = new(false);

    internal static ImpFreecam Create() => new GameObject("ImpFreecam").AddComponent<ImpFreecam>();

    private bool firstTimeOpen = true;

    private void Awake()
    {
        gameplayCamera = Imperium.Player.gameplayCamera;

        FreecamCamera = gameObject.AddComponent<Camera>();
        FreecamCamera.CopyFrom(gameplayCamera);
        FreecamCamera.cullingMask = Imperium.Settings.Freecam.FreecamLayerMask.Value;
        FreecamCamera.farClipPlane = 2000f;
        FreecamCamera.enabled = false;
        CullFactoryIntegration.DisableCulling(FreecamCamera);

        var hdCameraData = FreecamCamera.gameObject.AddComponent<HDAdditionalCameraData>();
        hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Volumetrics] = true;
        hdCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Volumetrics, false);

        var layerSelectorObject = Instantiate(ImpAssets.LayerSelectorObject, transform);
        layerSelector = layerSelectorObject.AddComponent<LayerSelector>();
        layerSelector.InitUI(Imperium.Interface.Theme);
        layerSelector.Bind(Imperium.Settings.Freecam.LayerSelector, Imperium.Settings.Freecam.FreecamLayerMask);

        IsFreecamEnabled.onTrue += OnFreecamEnable;
        IsFreecamEnabled.onFalse += OnFreecamDisable;

        IsMinicamEnabled.onTrue += OnMinicamEnable;
        IsMinicamEnabled.onFalse += OnMinicamDisable;

        IsMinicamFullscreenEnabled.onTrue += OnMinicamFullscreenEnable;
        IsMinicamFullscreenEnabled.onFalse += OnMinicamFullscreenDisable;

        var lightObject = Instantiate(Imperium.Player.nightVision.gameObject, transform, false);
        lightObject.transform.position = Vector3.up;

        Imperium.InputBindings.BaseMap.Freecam.performed += OnFreecamToggle;
        Imperium.InputBindings.BaseMap.Minicam.performed += OnMinicamToggle;
        Imperium.InputBindings.BaseMap.MinicamFullscreen.performed += OnMinicamFullscreenToggle;
        Imperium.InputBindings.BaseMap.Reset.performed += OnFreecamReset;
        Imperium.InputBindings.FreecamMap.LayerSelector.performed += OnToggleLayerSelector;
        Imperium.Settings.Freecam.FreecamLayerMask.onUpdate += value => FreecamCamera.cullingMask = value;
    }

    private void OnDestroy()
    {
        Imperium.InputBindings.BaseMap.Freecam.performed -= OnFreecamToggle;
        Imperium.InputBindings.BaseMap.Minicam.performed -= OnMinicamToggle;
        Imperium.InputBindings.BaseMap.MinicamFullscreen.performed -= OnMinicamFullscreenToggle;
        Imperium.InputBindings.BaseMap.Reset.performed -= OnFreecamReset;
        Imperium.InputBindings.FreecamMap.LayerSelector.performed -= OnToggleLayerSelector;
    }

    private void OnFreecamToggle(InputAction.CallbackContext callbackContext)
    {
        if (Imperium.Player.quickMenuManager.isMenuOpen ||
            Imperium.Player.inTerminalMenu ||
            Imperium.Player.isTypingChat) return;

        IsFreecamEnabled.Toggle();
    }

    private void OnMinicamToggle(InputAction.CallbackContext callbackContext)
    {
        if (Imperium.Player.quickMenuManager.isMenuOpen ||
            Imperium.Player.inTerminalMenu ||
            Imperium.Player.isTypingChat ||
            Imperium.ShipBuildModeManager.InBuildMode) return;

        IsMinicamEnabled.Toggle();
    }

    private void OnMinicamFullscreenToggle(InputAction.CallbackContext callbackContext)
    {
        if (Imperium.Player.quickMenuManager.isMenuOpen ||
            Imperium.Player.inTerminalMenu ||
            Imperium.Player.isTypingChat ||
            Imperium.ShipBuildModeManager.InBuildMode ||
            !IsMinicamEnabled.Value) return;

        IsMinicamFullscreenEnabled.Toggle();
    }

    private void OnMinicamEnable()
    {
        if (IsFreecamEnabled.Value) IsFreecamEnabled.SetFalse();

        PlayerManager.ToggleHUD(true);
        FreecamCamera.enabled = true;
        FreecamCamera.rect = minicamRect;

        IsMinicamFullscreenEnabled.SetFalse();
    }

    private void OnMinicamDisable()
    {
        // Hide UI if view is not switching from minicam to freecam
        if (!IsFreecamEnabled.Value) PlayerManager.ToggleHUD(false);

        FreecamCamera.enabled = false;

        FreecamCamera.rect = new Rect(0, 0, 1, 1);
    }

    private void OnMinicamFullscreenEnable() => FreecamCamera.rect = new Rect(0, 0, 1, 1);
    private void OnMinicamFullscreenDisable() => FreecamCamera.rect = minicamRect;

    private void OnFreecamEnable()
    {
        if (!FreecamCamera) return;

        Imperium.Interface.Close();

        if (IsMinicamEnabled.Value) IsMinicamEnabled.SetFalse();

        PlayerManager.ToggleHUD(true);
        Imperium.InputBindings.FreecamMap.Enable();
        FreecamCamera.enabled = true;
        Imperium.StartOfRound.SwitchCamera(FreecamCamera);
        Imperium.Player.isFreeCamera = true;
        enabled = true;

        if (firstTimeOpen)
        {
            firstTimeOpen = false;
            FreecamCamera.transform.position = Imperium.Player.gameplayCamera.transform.position + Vector3.up * 2;
        }
    }

    private void OnFreecamDisable()
    {
        if (!FreecamCamera) return;

        layerSelector.OnUIClose();

        // Hide UI if view is not switching to minimap state
        if (!IsMinicamEnabled.Value) PlayerManager.ToggleHUD(false);

        Imperium.InputBindings.FreecamMap.Disable();
        FreecamCamera.enabled = false;
        Imperium.StartOfRound.SwitchCamera(
            Imperium.Player.isPlayerDead ? Imperium.StartOfRound.spectateCamera : gameplayCamera
        );
        Imperium.Player.isFreeCamera = false;
        enabled = false;
    }

    private void OnFreecamReset(InputAction.CallbackContext callbackContext)
    {
        if (Imperium.Player.quickMenuManager.isMenuOpen ||
            Imperium.Player.inTerminalMenu ||
            Imperium.Player.isTypingChat) return;

        FreecamCamera.transform.position = Imperium.Player.gameplayCamera.transform.position + Vector3.up * 2;

        Imperium.Settings.Freecam.FreecamFieldOfView.Set(ImpConstants.DefaultFOV);
    }

    private void OnToggleLayerSelector(InputAction.CallbackContext callbackContext)
    {
        if (Imperium.Player.quickMenuManager.isMenuOpen ||
            Imperium.Player.inTerminalMenu ||
            Imperium.Player.isTypingChat) return;

        Imperium.Settings.Freecam.LayerSelector.Set(!layerSelector.IsOpen);
        if (layerSelector.IsOpen)
        {
            layerSelector.Close();
        }
        else
        {
            layerSelector.Open();
        }
    }

    private void Update()
    {
        // The component is only enabled when the freecam is active
        // Stop update of a quick menu an ImpUI is open with freecam 
        if (Imperium.Player.quickMenuManager.isMenuOpen) return;

        var scrollValue = Imperium.IngamePlayerSettings.playerInput.actions
            .FindAction("SwitchItem")
            .ReadValue<float>();

        Imperium.Settings.Freecam.FreecamMovementSpeed.Set(scrollValue switch
        {
            > 0 => Mathf.Min(Imperium.Settings.Freecam.FreecamMovementSpeed.Value + 1f, 100),
            < 0 => Mathf.Max(Imperium.Settings.Freecam.FreecamMovementSpeed.Value - 1f, 1f),
            _ => Imperium.Settings.Freecam.FreecamMovementSpeed.Value
        });

        if (Imperium.InputBindings.FreecamMap.IncreaseFOV.IsPressed())
        {
            Imperium.Settings.Freecam.FreecamFieldOfView.Set(
                Mathf.Min(300, Imperium.Settings.Freecam.FreecamFieldOfView.Value + 1)
            );
        }

        if (Imperium.InputBindings.FreecamMap.DecreaseFOV.IsPressed())
        {
            Imperium.Settings.Freecam.FreecamFieldOfView.Set(
                Mathf.Max(1, Imperium.Settings.Freecam.FreecamFieldOfView.Value - 1)
            );
        }

        FreecamCamera.fieldOfView = Imperium.Settings.Freecam.FreecamFieldOfView.Value;

        var cameraTransform = transform;

        var rotation = Imperium.Player.playerActions.Movement.Look.ReadValue<Vector2>();
        lookInput.x += rotation.x * 0.008f * Imperium.IngamePlayerSettings.settings.lookSensitivity;
        lookInput.y += rotation.y * 0.008f * Imperium.IngamePlayerSettings.settings.lookSensitivity;

        // Clamp the Y rotation to [-90;90] so the camera can't turn on it's head
        lookInput.y = Mathf.Clamp(lookInput.y, -90, 90);

        cameraTransform.rotation = Quaternion.Euler(-lookInput.y, lookInput.x, 0);

        var movement = Imperium.IngamePlayerSettings.playerInput.actions.FindAction("Move").ReadValue<Vector2>();
        var movementY = Imperium.InputBindings.BaseMap.FlyAscend.IsPressed() ? 1 :
            Imperium.InputBindings.BaseMap.FlyDescend.IsPressed() ? -1 : 0;
        var deltaMove = new Vector3(movement.x, movementY, movement.y)
                        * (Imperium.Settings.Freecam.FreecamMovementSpeed.Value * Time.deltaTime);
        cameraTransform.Translate(deltaMove);
    }
}