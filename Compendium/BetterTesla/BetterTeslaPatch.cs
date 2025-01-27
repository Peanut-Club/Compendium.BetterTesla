using CentralAuth;
using helpers;
using helpers.Patching;
using PlayerRoles;
using System.Linq;
using UnityEngine;

namespace Compendium.BetterTesla;

public static class BetterTeslaPatch
{
	[Patch(typeof(TeslaGateController), "FixedUpdate")]
	public static bool Prefix(TeslaGateController __instance)
	{
		int num = TeslaGate.AllGates.ToList().RemoveAll((TeslaGate tesla) => tesla == null || (object)((Component)(object)tesla).gameObject == null || (object)((Component)(object)tesla).gameObject.transform == null);
		if (num > 0)
		{
			Plugin.Warn($"Removed {num} invalid Tesla Gate instances!");
		}
		if (BetterTeslaLogic.RoundDisabled)
		{
			return false;
		}
        TeslaGate.AllGates.ForEach(delegate(TeslaGate tesla)
		{
			if (((Behaviour)(object)tesla).isActiveAndEnabled)
			{
				if (BetterTeslaLogic.AllowTeslaDamage)
				{
					TeslaDamageStatus status = BetterTeslaLogic.GetStatus(tesla);
					if (status.IsDisabled())
					{
						if (!tesla.isIdling)
						{
							tesla.ServerSideIdle(shouldIdle: true);
						}
						return;
					}
				}
				if (tesla.InactiveTime > 0f)
				{
					tesla.NetworkInactiveTime = Mathf.Max(0f, tesla.InactiveTime - Time.fixedDeltaTime);
				}
				else
				{
					bool flag = false;
					bool flag2 = false;
					foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
					{
						if (allHub.Mode == ClientInstanceMode.ReadyClient && allHub.IsAlive() && !BetterTeslaLogic.RoundDisabledRoles.Contains(allHub.GetRoleId()) && !BetterTeslaLogic.IgnoredRoles.Contains(allHub.GetRoleId()))
						{
							if (!flag)
							{
								flag = tesla.IsInIdleRange(allHub);
							}
							if (!flag2 && tesla.PlayerInRange(allHub) && !tesla.InProgress)
							{
								flag2 = true;
							}
						}
					}
					if (flag2)
					{
						tesla.ServerSideCode();
					}
					if (flag != tesla.isIdling)
					{
						tesla.ServerSideIdle(flag);
					}
				}
			}
		});
		return false;
	}
}
