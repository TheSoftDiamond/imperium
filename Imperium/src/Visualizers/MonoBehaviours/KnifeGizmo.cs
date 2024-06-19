#region

using System.Collections.Generic;
using Imperium.API;
using Imperium.Util;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

#endregion

namespace Imperium.Visualizers.MonoBehaviours;

public class KnifeGizmo : MonoBehaviour
{
    private GameObject capsule;
    private Dictionary<int, LineRenderer> targetRays = [];

    private KnifeItem knife;

    private const float CastLength = 0.75f;
    private const float CastRadius = 0.3f;

    private bool isActivelyHolding;

    private void Awake()
    {
        capsule = ImpGeometry.CreatePrimitive(
            PrimitiveType.Capsule, transform, Materials.WireframePurple
        );
    }

    public void Init(KnifeItem item, bool isHolding)
    {
        knife = item;
        isActivelyHolding = isHolding;
        capsule.SetActive(isActivelyHolding);

        if (!isActivelyHolding)
        {
            foreach (var (_, lineRenderer) in targetRays) Destroy(lineRenderer.gameObject);
            targetRays.Clear();
        }
    }

    private void Update()
    {
        if (!isActivelyHolding || !knife.playerHeldBy) return;

        var playerTransform = knife.playerHeldBy.transform;
        var playerCameraTransform = knife.playerHeldBy.gameplayCamera.transform;

        var position = playerCameraTransform.position;
        var forward = playerCameraTransform.forward;

        var positionStart = position + playerCameraTransform.right * 0.1f;
        var positionEnd = positionStart + forward * CastLength;

        capsule.transform.position = positionStart + forward * CastLength / 2;
        capsule.transform.rotation = Quaternion.LookRotation(playerCameraTransform.up, forward);

        var distance = Vector3.Distance(positionStart, positionEnd) + CastRadius * 2;
        capsule.transform.localScale = new Vector3(CastRadius * 2, distance / 2, CastRadius * 2);

        // ReSharper disable once Unity.PreferNonAllocApi
        // Allocating cast since this is a replication of the actual algorithm
        var hits = Physics.SphereCastAll(
            positionStart, CastRadius, forward, CastLength,
            11012424, QueryTriggerInteraction.Collide
        );

        var collisionIds = new HashSet<int>();
        foreach (var hit in hits)
        {
            var color = Color.green;
            if (hit.collider.gameObject.layer is 8 or 11)
            {
                color = Color.white;
            }
            else if (hit.point != Vector3.zero
                     && Physics.Linecast(position, hit.point, out _,
                         Imperium.StartOfRound.collidersAndRoomMaskAndDefault)
                     || !hit.transform.TryGetComponent<IHittable>(out _)
                     || hit.transform == playerTransform.transform)
            {
                continue;
            }

            var instanceId = hit.collider.gameObject.GetInstanceID();

            if (!targetRays.TryGetValue(instanceId, out var lineRenderer))
            {
                // ReSharper disable Unity.PerformanceCriticalCodeInvocation
                // This is only executed when a new collider is detected
                lineRenderer = ImpGeometry.CreateLine(transform, useWorldSpace: true);
                targetRays[instanceId] = lineRenderer;
            }

            collisionIds.Add(instanceId);

            ImpGeometry.SetLineColor(lineRenderer, color);
            ImpGeometry.SetLinePositions(lineRenderer, position + Vector3.up * 0.2f, hit.point);
        }

        // Destroy old rays that are not drawn anymore
        var newTargetRays = new Dictionary<int, LineRenderer>();
        foreach (var (id, lineRenderer) in targetRays)
        {
            if (collisionIds.Contains(id))
            {
                newTargetRays[id] = lineRenderer;
            }
            else
            {
                Destroy(lineRenderer.gameObject);
            }
        }

        targetRays = newTargetRays;
    }
}