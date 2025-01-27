using System.Collections.Generic;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;
using Compendium.Extensions;
using helpers;
using helpers.Configuration;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using PlayerRoles;
using PluginAPI.Events;
using UnityEngine;

namespace Compendium.BetterTesla;

public static class BetterTeslaLogic
{
	private static bool _isEnabled;

	private static readonly Dictionary<TeslaGate, TeslaDamageStatus> _damage = new Dictionary<TeslaGate, TeslaDamageStatus>();

	public static bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				if (!value)
				{
					_isEnabled = false;
					Log.Info("Better Tesla disabled.");
				}
				else
				{
					_isEnabled = true;
					Log.Info("Better Tesla enabled.");
				}
			}
		}
	}

	public static bool RoundDisabled { get; set; }

	public static List<RoleTypeId> RoundDisabledRoles { get; set; } = new List<RoleTypeId>();


	[Config(Name = "Allow Damage", Description = "Whether or not to allow players to damage Tesla gates, which results in them not working for a bit.")]
	public static bool AllowTeslaDamage { get; set; } = true;


	[Config(Name = "Damaged Hint", Description = "Whether or not to show a hint when a Tesla gets damaged.")]
	public static bool DamagedTeslaHint { get; set; } = true;


	[Config(Name = "Damaged Radius", Description = "The radius of shown hint.")]
	public static float DamagedTeslaRadius { get; set; } = 30f;


	[Config(Name = "Damaged Blackout", Description = "Whether or not to blackout the room when a Tesla gets damaged.")]
	public static bool DamagedBlackout { get; set; } = true;


	[Config(Name = "Damaged Blackout Duration", Description = "The duration of the blackout.")]
	public static float DamagedBlackoutDuration { get; set; } = 5f;


	[Config(Name = "Allow Grenade Damage", Description = "Whether or not to allow grenades to damage Tesla gates.")]
	public static bool AllowTeslaGrenadeDamage { get; set; } = true;


	[Config(Name = "Grenade Radius", Description = "The radius in which a Tesla can be hit from a grenade.")]
	public static float GrenadeRadius { get; set; } = 30f;


	[Config(Name = "Grenade Damage", Description = "Base grenade damage.")]
	public static float GrenadeDamage { get; set; } = 200f;


	[Config(Name = "Grenade Full Damage Distance", Description = "The maximum distance for a full damage hit by a grenade.")]
	public static float FullGrenadeDamageDistance { get; set; } = 10f;


	[Config(Name = "Grenade Damage Falloff", Description = "Grenade damage falloff over distance.")]
	public static float GrenadeDamageFalloff { get; set; } = 10f;


	[Config(Name = "Grenade Time Multiplier", Description = "The time multiplier if a Tesla gets knocked out by a grenade.")]
	public static int GrenadeTimeMultiplier { get; set; } = -1;


	[Config(Name = "Min Tesla Timeout", Description = "The minimal Tesla timeout, in seconds.")]
	public static float MinTeslaTimeout { get; set; } = 5f;


	[Config(Name = "Max Tesla Timeout", Description = "The maximum Tesla timeout, in seconds.")]
	public static float MaxTeslaTimeout { get; set; } = 8f;


	[Config(Name = "Tesla Health", Description = "The health of a Tesla gate.")]
	public static float TeslaHealth { get; set; } = 200f;


	[Config(Name = "Ignored Roles", Description = "A list of roles that will be ignored by the Tesla gate.")]
	public static List<RoleTypeId> IgnoredRoles { get; set; } = new List<RoleTypeId>
	{
		RoleTypeId.CustomRole,
		RoleTypeId.FacilityGuard,
		RoleTypeId.Filmmaker,
		RoleTypeId.NtfCaptain,
		RoleTypeId.NtfPrivate,
		RoleTypeId.NtfSergeant,
		RoleTypeId.NtfSpecialist,
		RoleTypeId.Overwatch,
		RoleTypeId.Scientist,
		RoleTypeId.Tutorial
	};


	public static IReadOnlyDictionary<TeslaGate, TeslaDamageStatus> Damage => _damage;

	public static int TeslaMask { get; } = LayerMask.GetMask("Default", "Viewmodel", "Hitbox", "InvisibleCollider", "Ragdoll", "Water", "UI", "IgnoreRaycast");


	public static TeslaDamageStatus GetStatus(TeslaGate gate)
	{
		if (_damage.TryGetValue(gate, out var value))
		{
			return value;
		}
		TeslaDamageStatus teslaDamageStatus2 = (_damage[gate] = new TeslaDamageStatus(gate));
		return value = teslaDamageStatus2;
	}

	[Event]
	private static void OnShot(PlayerShotWeaponEvent ev)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if (!AllowTeslaDamage)
		{
			return;
		}
		Ray ray = new Ray(ev.Player.Camera.position, ev.Player.Camera.forward);

		if (!ev.Firearm.Modules.TryGetFirst(m => m is HitscanHitregModuleBase, out HitscanHitregModuleBase hitscanModule)) {
			return;
		}

		float maxDistance = hitscanModule.DamageFalloffDistance + hitscanModule.FullDamageDistance;
		if (Physics.Raycast(ray, out var hitInfo, maxDistance, TeslaMask, QueryTriggerInteraction.Ignore) && hitInfo.collider != null && hitInfo.collider.gameObject.TryGet<TeslaGate>(out var result))
		{
			float damage = hitscanModule.DamageAtDistance(hitInfo.distance);
			if (damage > 0f)
			{
				GetStatus(result)?.ProcessDamage(damage);
			}
		}
	}

	[Event]
	private static void OnGrenadeExplode(GrenadeExplodedEvent ev)
	{
		if (TeslaGate.AllGates == null)
		{
			return;
		}
		foreach (TeslaGate teslaGate in TeslaGate.AllGates)
		{
			float num = DamageAtDistance(GrenadeDamage, ((Component)(object)teslaGate).DistanceSquared(ev.Position));
			if (num <= 0f)
			{
				continue;
			}
			GetStatus(teslaGate)?.ProcessDamage(num, isGrenade: true);
			break;
		}
	}

	private static float DamageAtDistance(float baseDamage, float distance)
	{
		if (distance >= GrenadeRadius)
		{
			return 0f;
		}
		if (distance > FullGrenadeDamageDistance)
		{
			float num = 100f - GrenadeDamageFalloff * (distance - FullGrenadeDamageDistance);
			baseDamage *= num / 100f;
		}
		return baseDamage;
	}

	[RoundStateChanged(new RoundState[] { RoundState.Restarting })]
	private static void OnRoundRestart()
	{
		RoundDisabled = false;
		RoundDisabledRoles.Clear();
		_damage.Clear();
		Log.Debug("Cleared round-temporary variables.");
	}
}
