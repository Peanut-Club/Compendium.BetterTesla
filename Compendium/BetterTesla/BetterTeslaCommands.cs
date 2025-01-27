using System.Collections.Generic;
using System.Linq;
using BetterCommands;
using Compendium.Extensions;
using PlayerRoles;
using PluginAPI.Core;
using UnityEngine;

namespace Compendium.BetterTesla;

public static class BetterTeslaCommands
{
	[Command("teslatp", new CommandType[] { CommandType.RemoteAdmin })]
	[Description("Teleports you to the nearest tesla gate.")]
	public static string TeslaTp(Player sender)
	{
		List<TeslaGate> teslaGates = TeslaGate.AllGates.ToList();
		IOrderedEnumerable<TeslaGate> orderedEnumerable = teslaGates.OrderByDescending((TeslaGate tesla) => ((Component)(object)tesla).DistanceSquared(sender.Position));
		TeslaGate teslaGate = teslaGates.First();
		sender.IsGodModeEnabled = true;
		sender.Position = teslaGate.Position;
		Calls.Delay(2f, delegate
		{
			sender.IsGodModeEnabled = false;
		});
		return $"Teleported you to the nearest tesla gate ({teslaGate.Room.Name})";
	}

	[Command("teslarole", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "trole", "teslar" })]
	[Description("Disables/enables Tesla gates for a specific role for the entire round.")]
	public static string SwitchRole(Player sender, RoleTypeId role)
	{
		if (BetterTeslaLogic.RoundDisabledRoles.Contains(role) && BetterTeslaLogic.RoundDisabledRoles.Remove(role))
		{
			return $"Re-enabled Tesla gates for role: {role}";
		}
		BetterTeslaLogic.RoundDisabledRoles.Add(role);
		return $"Disabled Tesla gates for role: {role}";
	}

	[Command("teslastatus", new CommandType[]
	{
		CommandType.RemoteAdmin,
		CommandType.GameConsole
	})]
	[CommandAliases(new object[] { "teslas", "tstatus" })]
	[Description("Disables/enables Tesla gates for the entire round.")]
	public static string SwitchTesla(Player sender)
	{
		BetterTeslaLogic.RoundDisabled = !BetterTeslaLogic.RoundDisabled;
		return BetterTeslaLogic.RoundDisabled ? "Tesla Gates disabled." : "Tesla Gates enabled.";
	}
}
