#region

using Imperium.API.Types.Networking;
using UnityEngine;

#endregion

namespace Imperium.MonoBehaviours.ImpUI.ImperiumUI.ObjectListEntry;

internal class ObjectEntrySpikeTrap : ObjectEntry
{
    protected override bool CanRespawn() => true;
    protected override bool CanDrop() => false;
    protected override bool CanTeleportHere() => true;

    protected override void Respawn()
    {
        Destroy();
        Imperium.ObjectManager.SpawnMapHazard(new MapHazardSpawnRequest
        {
            Name = "Spike Trap",
            SpawnPosition = containerObject.transform.position
        });
    }

    public override void Destroy()
    {
        base.Destroy();
        Imperium.ObjectManager.DespawnObstacle(objectNetId!.Value);
    }

    protected override void TeleportHere()
    {
        var origin = Imperium.Freecam.IsFreecamEnabled.Value ? Imperium.Freecam.transform : null;
        Imperium.ImpPositionIndicator.Activate(position => GetContainerObject().transform.position = position, origin);
    }

    protected override string GetObjectName() => $"Spike Trap <i>{component.GetInstanceID()}</i>";
    protected override GameObject GetContainerObject() => component.transform.parent.parent.parent.gameObject;
}