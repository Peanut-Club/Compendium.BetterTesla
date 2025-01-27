using System.Collections.Generic;
using System.Linq;
using CentralAuth;
using Compendium.Extensions;
using helpers;
using helpers.Random;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles;
using UnityEngine;

namespace Compendium.BetterTesla;

public class TeslaDamageStatus
{
	private TeslaGate m_Tesla;

	private float m_RemainingHealth;

	public TeslaDamageStatus(TeslaGate teslaGate)
	{
		m_Tesla = teslaGate;
		m_RemainingHealth = BetterTeslaLogic.TeslaHealth;
	}

	public bool IsDisabled()
	{
		return m_RemainingHealth <= 0f;
	}

	public void ProcessDamage(float damage, bool isGrenade = false)
	{
		if (IsDisabled())
		{
			return;
		}
		m_RemainingHealth -= damage;
		if (!IsDisabled())
		{
			return;
		}
		int num = Mathf.CeilToInt(Random.Range(BetterTeslaLogic.MinTeslaTimeout, BetterTeslaLogic.MaxTeslaTimeout));
		if (isGrenade && BetterTeslaLogic.GrenadeTimeMultiplier != -1)
		{
			num *= BetterTeslaLogic.GrenadeTimeMultiplier;
		}
		if (BetterTeslaLogic.DamagedTeslaHint)
		{
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				if (allHub.Mode == ClientInstanceMode.ReadyClient && allHub.IsAlive() && ((Component)(object)allHub).IsWithinDistance(((Component)(object)m_Tesla).transform.position, BetterTeslaLogic.DamagedTeslaRadius))
				{
					allHub.Hint($"\n\n<b><color=#33FFA5>Tesla Gate <color=#FF0000>disabled</color> for <color=#FF0000>{num}</color> second(s)</color></b>!", 3f);
				}
			}
		}
		if (BetterTeslaLogic.DamagedBlackout && BetterTeslaLogic.DamagedBlackoutDuration > 0f && m_Tesla.Room != null)
		{
			IEnumerable<DoorVariant> doors = DoorVariant.AllDoors.Where((DoorVariant door) => door.Rooms.Contains(m_Tesla.Room));
			IEnumerable<RoomLightController> values = RoomLightController.Instances.Where((RoomLightController light) => (Object)(object)light != null && light.Room == m_Tesla.Room);
			doors.ForEach(delegate(DoorVariant door)
			{
				door.NetworkTargetState = WeightedRandomGeneration.Default.GetBool(30);
				door.ServerChangeLock(DoorLockReason.AdminCommand, newState: true);
			});
			values.ForEach(delegate(RoomLightController light)
			{
				light.ServerFlickerLights(BetterTeslaLogic.DamagedBlackoutDuration);
			});
			Calls.Delay(BetterTeslaLogic.DamagedBlackoutDuration + 0.2f, delegate
			{
				doors.ForEach(delegate(DoorVariant door)
				{
					door.ServerChangeLock(DoorLockReason.AdminCommand, newState: false);
				});
			});
		}
		Calls.Delay(num, delegate
		{
			Reset();
		});
	}

	private void Reset()
	{
		m_RemainingHealth = BetterTeslaLogic.TeslaHealth;
	}
}
