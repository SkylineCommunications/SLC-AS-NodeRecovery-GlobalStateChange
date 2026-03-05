namespace NodeRecoveryGlobalStateChange
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.NodeRecovery;

	public static class SwarmingTargets
	{
		public static HashSet<int> CalculateoutageSources(GlobalStateChangeInput input)
		{
			// Gather all nodes that are in Outage (to swarm from)
			return input.ClusterState
				.Where(kvp => kvp.Value.State == NodeState.Outage && !kvp.Value.InMaintenance)
				.Select(kvp => kvp.Key)
				.ToHashSet();
		}

		public static HashSet<int> CalculateHealhtyTargets(
			GlobalStateChangeInput input,
			GetDataMinerInfoResponseMessage[] dataMinerInfoEvents = null,
			IEngine engine = null)
		{
			// For extra safety check which Agents are disconnected according to local agent
			var disconnectedAgents = dataMinerInfoEvents?
				.Where(info => info.ConnectionState != DataMinerAgentConnectionState.Normal)
				.Select(info => info.ID)
				.ToHashSet() ?? new HashSet<int>();

			if (disconnectedAgents.Count > 0)
			{
				engine?.Log($"NodeRecovery: Detected {disconnectedAgents.Count} disconnected agent(s) according to local SLNet: {string.Join(", ", disconnectedAgents)}. These agents will be excluded from swarming targets and sources.");
			}

			// Gather all nodes that are Healthy (to swarm to)
			// Exclude nodes in Maintenance mode as they are not be touched in any capacity
			// Implicitly ignores nodes in Unknown state as well
			return input.ClusterState
				.Where(kvp => kvp.Value.State == NodeState.Healthy && !kvp.Value.InMaintenance && !disconnectedAgents.Contains(kvp.Key))
				.Select(kvp => kvp.Key)
				.ToHashSet();
		}
	}
}
