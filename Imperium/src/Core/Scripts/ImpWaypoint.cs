#region

using System;
using System.Collections;
using Imperium.Core.Lifecycle;
using Imperium.Util;
using TMPro;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

#endregion

namespace Imperium.Core.Scripts;

public class ImpWaypoint : MonoBehaviour
{
    private const float fallDuration = 0.2f;
    private const float fallHeight = 1.5f;

    private LineRenderer lineRenderer;
    private GameObject beacon;

    private RectTransform overlayRect;
    private Canvas overlayCanvas;
    private RectTransform overlayCanvasRect;
    private TMP_Text overlayNameText;
    private TMP_Text overlayDistanceText;
    private CanvasGroup overlayInteractiveGroup;

    private Waypoint waypoint;

    private Vector3 overlayWorldPosition;

    private bool hideInteractionLastFrame;
    private bool showInteractionLastFrame;

    private Action onHover;

    private void Awake()
    {
        var indicatorObj = Instantiate(ImpAssets.WaypointOverlay, transform);
        overlayCanvas = indicatorObj.GetComponent<Canvas>();
        overlayCanvasRect = indicatorObj.GetComponent<RectTransform>();
        overlayCanvas.sortingOrder = -1;

        overlayRect = indicatorObj.transform.Find("Overlay").GetComponent<RectTransform>();

        overlayInteractiveGroup = indicatorObj.transform.Find("Overlay/Interaction").GetComponent<CanvasGroup>();
        overlayInteractiveGroup.alpha = 0;

        overlayNameText = indicatorObj.transform.Find("Overlay/Name").GetComponent<TMP_Text>();
        overlayDistanceText = indicatorObj.transform.Find("Overlay/Distance").GetComponent<TMP_Text>();

        var beaconObj = Instantiate(ImpAssets.WaypointBeacon, transform);
        beacon = beaconObj.transform.Find("Beacon").gameObject;

        Imperium.Settings.Waypoint.EnableBeacons.onUpdate += beacon.SetActive;
        Imperium.Settings.Waypoint.EnableOverlay.onUpdate += indicatorObj.SetActive;

        var lineStart = transform.position with { y = -1000 };
        var lineEnd = transform.position with { y = 1000 };
        lineRenderer = ImpGeometry.CreateLine(transform, 0.03f, lineName: "Line", positions: [lineStart, lineEnd]);
    }

    internal void Init(Waypoint waypointData, Action onHoverCallback)
    {
        waypoint = waypointData;
        onHover = onHoverCallback;

        waypointData.IsShown.onUpdate += gameObject.SetActive;

        transform.position = waypointData.BeaconPosition;
        overlayWorldPosition = waypointData.BeaconPosition + Vector3.up * 2.3f;
        StartCoroutine(beaconFallAnimation(CalculateFloorPosition(waypointData.BeaconPosition)));
    }

    private IEnumerator animateInnerOpacityTo(float duration, float targetAlpha)
    {
        var startAlpha = overlayInteractiveGroup.alpha;
        var elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            overlayInteractiveGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        overlayInteractiveGroup.alpha = targetAlpha;
    }

    private IEnumerator beaconFallAnimation(Vector3 endPosition)
    {
        var elapsedTime = 0f;

        while (elapsedTime < fallDuration)
        {
            var t = elapsedTime / fallDuration;
            var height = Mathf.Lerp(endPosition.y + fallHeight, endPosition.y, t * t * t * t);
            beacon.transform.position = endPosition with { y = height };

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        beacon.transform.position = endPosition;
        Imperium.Player.itemAudio.PlayOneShot(Imperium.ObjectManager.BeaconDrop);
    }

    private static Vector3 CalculateFloorPosition(Vector3 basePosition)
    {
        // Start the raycast a bit higher if the ground is higher than the player
        var raycastStart = basePosition + Vector3.up * 4;

        /*
         * Layer mask taken from GrabbableObject.FallToGround().
         */
        var hasFloor = Physics.Raycast(
            raycastStart,
            Vector3.down,
            out var hitInfo,
            80f, 268437760,
            QueryTriggerInteraction.Ignore
        );

        return !hasFloor ? raycastStart : hitInfo.point;
    }

    private void SetOverlayPosition(Camera camera)
    {
        var targetPos = camera.WorldToScreenPoint(overlayWorldPosition);
        if (targetPos.z < 0) targetPos *= -1;

        var activeTexture = camera.activeTexture;
        var scaleFactorX = activeTexture.width / overlayCanvasRect.sizeDelta.x;
        var scaleFactorY = activeTexture.height / overlayCanvasRect.sizeDelta.y;

        var screenWith = activeTexture.width / scaleFactorX;
        var screenHeight = activeTexture.height / scaleFactorY;

        var margin = activeTexture.width / scaleFactorX * 0.05f;
        var positionX = Math.Clamp(targetPos.x / scaleFactorX, margin, screenWith - margin);
        var positionY = Math.Clamp(targetPos.y / scaleFactorY, margin, screenHeight - margin * 2);

        overlayRect.anchoredPosition = new Vector2(positionX, positionY);
        overlayRect.localScale = Vector3.one * Math.Clamp(
            5 / Vector3.Distance(camera.transform.position, overlayWorldPosition),
            0.6f, 1f
        );
    }

    private void LateUpdate()
    {
        var camera = PlayerManager.GetActiveCamera();

        lineRenderer.gameObject.SetActive(
            Vector3.Distance(camera.transform.position, lineRenderer.transform.position) > 20f
        );

        var playerForward2D = camera.transform.forward with { y = 0 };
        var beaconDirection2D = (transform.position - camera.transform.position) with { y = 0 };

        // Animate fade in / out if player started looking at / away from waypoint
        switch (Vector3.Angle(playerForward2D, beaconDirection2D) < 10)
        {
            case true when !hideInteractionLastFrame:
                StartCoroutine(animateInnerOpacityTo(0.2f, 1f));
                onHover.Invoke();
                hideInteractionLastFrame = true;
                showInteractionLastFrame = false;
                break;
            case false when !showInteractionLastFrame:
                StartCoroutine(animateInnerOpacityTo(0.2f, 0f));
                showInteractionLastFrame = true;
                hideInteractionLastFrame = false;
                break;
        }

        overlayNameText.text = waypoint.Name;
        overlayDistanceText.text = $"{Vector3.Distance(transform.position, camera.transform.position):0.0}";

        SetOverlayPosition(camera);
    }
}