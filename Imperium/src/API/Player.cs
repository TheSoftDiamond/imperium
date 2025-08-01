#region

using Imperium.API.Types.Networking;
using Imperium.Util.Binding;

#endregion

namespace Imperium.API;

public static class Player
{
    /// <summary>
    ///     Teleports a player to a specified location.
    /// </summary>
    public static void TeleportPlayer(TeleportPlayerRequest request)
    {
        APIHelpers.AssertImperiumReady();

        Imperium.PlayerManager.TeleportPlayer(request);
    }

    /// <summary>
    ///     Whether the player has infinite stamina.
    /// </summary>
    public static IBinding<bool> InfiniteSprint
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.InfiniteSprint;
        }
    }

    /// <summary>
    ///     Whether animation locking is disabled for the player.
    /// </summary>
    public static IBinding<bool> DisableLocking
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.DisableLocking;
        }
    }

    /// <summary>
    ///     Whether the player has infinite battery.
    /// </summary>
    public static IBinding<bool> InfiniteBattery
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.InfiniteBattery;
        }
    }

    /// <summary>
    ///     Whether the player can pick up items in orbit.
    /// </summary>
    public static IBinding<bool> PickupOverride
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.PickupOverride;
        }
    }

    /// <summary>
    ///     Whether the player can enter flying mode.
    /// </summary>
    public static IBinding<bool> EnableFlying
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.EnableFlying;
        }
    }

    /// <summary>
    ///     Whether the player is on god mode.
    /// </summary>
    public static IBinding<bool> GodMode
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.GodMode;
        }
    }

    /// <summary>
    ///     Controls the current night vision of the player.
    ///     Default: 0
    /// </summary>
    public static IBinding<float> NightVision
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.NightVision;
        }
    }

    /// <summary>
    ///     Controls the current movement speed of the player.
    ///     Default: <see cref="ImpConstants.DefaultMovementSpeed" />
    /// </summary>
    public static IBinding<float> MovementSpeed
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.MovementSpeed;
        }
    }

    /// <summary>
    ///     Controls the current jump force of the player.
    ///     Default: <see cref="ImpConstants.DefaultJumpForce" />
    /// </summary>
    public static IBinding<float> JumpForce
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.JumpForce;
        }
    }

    /// <summary>
    ///     Controls the current flying speed of the player.
    ///     Default: <see cref="ImpConstants.DefaultJumpForce" />
    /// </summary>
    public static IBinding<float> FlyingSpeed
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.FlyingSpeed;
        }
    }

    /// <summary>
    ///     Controls the FOV of the player. If set to -1, the game's default FoV will ve applied.
    ///     Default: -1
    /// </summary>
    public static IBinding<float> CustomFieldOfView
    {
        get
        {
            APIHelpers.AssertImperiumReady();

            return Imperium.Settings.Player.CustomFieldOfView;
        }
    }
}