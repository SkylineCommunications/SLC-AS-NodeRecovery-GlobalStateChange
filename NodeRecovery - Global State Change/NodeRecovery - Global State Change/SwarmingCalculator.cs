namespace NodeRecoveryGlobalStateChange
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.NodeRecovery;
	using Skyline.DataMiner.Net.Swarming;

	/// <summary>
	/// Calculates optimal swarming decisions for NodeRecovery.
	/// </summary>
	public static class SwarmingCalculator
	{
		/// <summary>
		/// Calculates the swarming requests needed to move objects from unhealthy nodes to healthy nodes.
		/// </summary>
		/// <param name="healhtyTargets">DMAID of agents that are valid swarming targets.</param>
		/// <param name="outageSources">DMAID of agents in node recovery global outage that needs recovery.</param>
		/// <param name="allObjects">All swarming objects in the cluster.</param>
		/// <returns>Dictionary of Swarming request arrays, 1 array per target agent id. Each array contains a message per SwarmingObjectType.</returns>
		public static Dictionary<int, SwarmingRequestMessage[]> CalculateSwarmingRequests(
			HashSet<int> healhtyTargets,
			HashSet<int> outageSources,
			List<SwarmingObject> allObjects)
		{
			if (healhtyTargets == null)
				throw new ArgumentNullException(nameof(healhtyTargets));
			if (outageSources == null)
				throw new ArgumentNullException(nameof(outageSources));
			if (allObjects == null)
				throw new ArgumentNullException(nameof(allObjects));

			if (healhtyTargets.Count == 0 || outageSources.Count == 0)
				return new Dictionary<int, SwarmingRequestMessage[]>();

			var objectsToMove = allObjects
				.Where(o => o.IsSwarmable && outageSources.Contains(o.HostingAgentId))
				.ToList();

			if (objectsToMove.Count == 0)
				return new Dictionary<int, SwarmingRequestMessage[]>();

			var nodeLoadTracker = new NodeLoadTracker(healhtyTargets, allObjects);
			var assignments = new Dictionary<int, List<SwarmingObject>>(healhtyTargets.Count);

			// To maximize balancing effectiveness, assign heaviest objects first
			// This avoids a scenario where many small objects are assigned first,
			// leading to suboptimal distribution of heavier objects later on.
			// Resulting in a final distribution that is less balanced.
			foreach (var obj in objectsToMove.OrderByDescending(o => o.Weight))
			{
				int targetNode = nodeLoadTracker.GetLeastLoadedNode();
				if (!assignments.TryGetValue(targetNode, out var list))
				{
					list = new List<SwarmingObject>();
					assignments[targetNode] = list;
				}

				list.Add(obj);
				nodeLoadTracker.AddLoadToNode(targetNode, obj.Weight);
			}

			return assignments.ToDictionary(
				kvp => kvp.Key, // target DMA ID
				kvp =>
				{
					var targetDmaId = kvp.Key;
					var objectsToSwarm = kvp.Value;

					return objectsToSwarm
						.GroupBy(obj => obj.Type) // Swarm requests need to be grouped by object type
						.OrderBy(grp => grp.Key) // Swarm more important objects first (!! Uses the order defined in SwarmingObjectType)
						.Select(grp => new SwarmingRequestMessage
						{
							TargetDmaId = targetDmaId,
							DmaObjectRefs = grp.Select(obj => obj.Id).ToArray(),
						})
						.ToArray();
				});
		}
	}
}