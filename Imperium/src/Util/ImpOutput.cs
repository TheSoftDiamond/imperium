#region

using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Imperium.API.Types.Networking;
using Imperium.Core;
using Imperium.Util.Binding;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

#endregion

namespace Imperium.Util;

internal class ImpOutput(ManualLogSource logger)
{
    // Notification settings will be bound later on when Imperium is loaded
    private Dictionary<NotificationType, ImpBinding<bool>> NotificationSettings = new();

    internal void BindNotificationSettings(ImpSettings settings)
    {
        NotificationSettings = new Dictionary<NotificationType, ImpBinding<bool>>
        {
            { NotificationType.GodMode, settings.Preferences.NotificationsGodMode },
            { NotificationType.OracleUpdate, settings.Preferences.NotificationsOracle },
            { NotificationType.Confirmation, settings.Preferences.NotificationsConfirmation },
            { NotificationType.SpawnReport, settings.Preferences.NotificationsSpawnReports },
            { NotificationType.Entities, settings.Preferences.NotificationsEntities },
            { NotificationType.Server, settings.Preferences.NotificationsServer },
            { NotificationType.AccessControl, settings.Preferences.NotificationsAccessControl },
            { NotificationType.Spawning, settings.Preferences.NotificationsSpawning },
            { NotificationType.Required, new ImpBinding<bool>(true) },
            { NotificationType.Other, settings.Preferences.NotificationsOther }
        };
    }

    private bool IsNotificationEnabled(NotificationType type)
    {
        return NotificationSettings.GetValueOrDefault(type, null)?.Value ?? false;
    }

    internal void Status(string text) => HUDManager.Instance.DisplayStatusEffect(text);
    internal void Debug(string text) => HUDManager.Instance.SetDebugText(text);

    internal void Send(
        string text,
        string title = "Imperium",
        bool isWarning = false,
        NotificationType type = NotificationType.Other
    )
    {
        if (!HUDManager.Instance)
        {
            LogError($"Failed to send notification, HUDManager is not yet instantiated, message: {text}");
            return;
        }

        // Disable notifications if turned off or during loading of settings
        if (!IsNotificationEnabled(type) || Imperium.Settings.IsLoading) return;

        HUDManager.Instance.DisplayTip(title, text, isWarning);
    }

    internal void LogBlock(List<string> lines, string title = "Imperium Monitoring")
    {
        if (!Imperium.Settings.Preferences.GeneralLogging.Value) return;

        title = "< " + title + " >";
        var width = Mathf.Max(lines.Max(line => line.Length) + 4, 20);
        var fullWidth = string.Concat(Enumerable.Repeat("\u2550", width - 2));
        var titlePaddingCount = (width - title.Length) / 2 - 1;
        if ((width - title.Length) / 2 % 2 == 0) titlePaddingCount++;

        var titlePadding = string.Concat(Enumerable.Repeat(" ", titlePaddingCount));

        var output = "\u2552" + fullWidth + "\u2555\n";
        output += "\u2502" + titlePadding + title + titlePadding + "\u2502\n";
        output += "\u255e" + fullWidth + "\u2561\n";
        output = lines.Aggregate(output,
            (current, line) => current + $"\u2502 {line}".PadRight(width - 2) + " \u2502\n");
        output += "\u2558" + fullWidth + "\u255b";

        foreach (var se in output.Split("\n"))
        {
            Log(LogLevel.Message, se.Trim());
        }
    }

    internal void Log(LogLevel level, string text) => logger.Log(level, text);
    internal void LogInfo(string text) => logger.LogInfo(text);
    internal void LogDebug(string text) => logger.LogDebug(text);
    internal void LogWarning(string text) => logger.LogWarning(text);
    internal void LogError(string text) => logger.LogError(text);
}