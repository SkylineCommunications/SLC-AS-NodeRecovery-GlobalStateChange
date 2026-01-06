namespace NodeRecoveryGlobalStateChange
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.NodeRecovery;
	using Skyline.DataMiner.Net.Swarming;

	/// <summary>
	/// Calculates optimal swarming decisions during node recovery.
	/// </summary>
	public static class SwarmingCalculator
	{
		/// <summary>
		/// Calculates the swarming requests needed to move objects from unhealthy nodes to healthy nodes.
		/// </summary>
		/// <param name="clusterState">The current cluster state mapping node IDs to their state info.</param>
		/// <param name="allObjects">All swarming objects in the cluster.</param>
		/// <returns>Dictionary of Swarming request arrays, 1 array per target agent id.</returns>
		public static Dictionary<int, SwarmingRequestMessage[]> CalculateSwarmingRequests(
			Dictionary<int, NodeStateInfo> clusterState,
			List<SwarmingObject> allObjects)
		{
			if (clusterState == null)
				throw new ArgumentNullException(nameof(clusterState));
			if (allObjects == null)
				throw new ArgumentNullException(nameof(allObjects));

			var nodesByState = clusterState
				.Where(kvp => !kvp.Value.InMaintenance)
				.ToLookup(kvp => kvp.Value.State, kvp => kvp.Key);

			var healthyNodes = new HashSet<int>(nodesByState[NodeState.Healthy]);
			var outageNodes = new HashSet<int>(nodesByState[NodeState.Outage]);

			if (healthyNodes.Count == 0 || outageNodes.Count == 0)
				return new Dictionary<int, SwarmingRequestMessage[]>();

			var objectsToMove = allObjects
				.Where(o => o.IsSwarmable && outageNodes.Contains(o.HostingAgentId))
				.ToList();

			if (objectsToMove.Count == 0)
				return new Dictionary<int, SwarmingRequestMessage[]>();

			var nodeLoadTracker = new NodeLoadTracker(healthyNodes, allObjects);
			var assignments = new Dictionary<int, List<SwarmingObject>>(healthyNodes.Count);

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
				kvp => kvp.Key,
				kvp =>
				{
					var targetDmaId = kvp.Key;
					var objs = kvp.Value;

					return objs
						.GroupBy(obj => obj.Type)
						.OrderBy(grp => grp.Key) // !! Use the order defined in SwarmingObjectType
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