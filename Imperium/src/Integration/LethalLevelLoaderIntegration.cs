#region

using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;

#endregion

namespace Imperium.Integration;

public static class LethalLevelLoaderIntegration
{
    private static bool IsEnabled => Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static bool TryGetFlowDisplayName(string flowName, out string flowDisplayName)
    {
        if (!IsEnabled)
        {
            flowDisplayName = null;
            return false;
        }

        flowDisplayName = GetLevelNameInternal(flowName);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static string GetLevelNameInternal(string flowName)
    {
        // TODO(giosuel): Implement proper display name lookup
        return flowName;
    }
}