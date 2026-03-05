namespace NodeRecoveryGlobalStateChange
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.NodeRecovery;

	public static class SwarmingTargets
	{
		public static (HashSet<int> HealhtyTargets, HashSet<int> OutageSources) Calculate(GlobalStateChangeInput input)
		{
			// Gather all nodes that are in Outage (to swarm from) and Healthy (to swarm to)
			// Exclude nodes in Maintenance mode as they are not be touched in any capacity
			// Implicitly ignores nodes in Unknown state as well
			var nodesByState = input.ClusterState
				.Where(kvp => !kvp.Value.InMaintenance)
				.ToLookup(kvp => kvp.Value.State, kvp => kvp.Key);

			var healhtyTargets = new HashSet<int>(nodesByState[NodeState.Healthy]);
			var outageSources = new HashSet<int>(nodesByState[NodeState.Outage]);

			return (healhtyTargets, outageSources);
		}
	}
}
