namespace NodeRecoveryGlobalStateChange
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Tracks the load (sum of weights of hosted objects) on each healthy node.
	/// </summary>
	public class NodeLoadTracker
	{
		/// <summary>
		/// The current load (sum of weights of hosted objects) per node ID.
		/// </summary>
		private readonly Dictionary<int, int> _loadPerNode;

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeLoadTracker"/> class.
		/// </summary>
		/// <param name="healthyNodes">Set of healthy node IDs.</param>
		/// <param name="allObjects">All swarming objects in the cluster.</param>
		public NodeLoadTracker(HashSet<int> healthyNodes, IEnumerable<SwarmingObject> allObjects)
		{
			_loadPerNode = healthyNodes.ToDictionary(nodeId => nodeId, _ => 0);

			// Count current weighted load on each healthy node (including non-swarmable)
			foreach (var obj in allObjects)
			{
				if (_loadPerNode.ContainsKey(obj.HostingAgentId))
				{
					_loadPerNode[obj.HostingAgentId] += obj.Weight;
				}
			}
		}

		/// <summary>
		/// Gets the node with the lowest current load.
		/// </summary>
		/// <returns>The node ID with the lowest load.</returns>
		public int GetLeastLoadedNode()
		{
			int minNode = -1;
			int minLoad = int.MaxValue;

			foreach (var kvp in _loadPerNode)
			{
				if (kvp.Value < minLoad)
				{
					minLoad = kvp.Value;
					minNode = kvp.Key;
				}
			}

			return minNode;
		}

		/// <summary>
		/// Adds load to a specific node.
		/// </summary>
		/// <param name="nodeId">The node ID.</param>
		/// <param name="amount">The amount to add.</param>
		public void AddLoadToNode(int nodeId, int amount)
		{
			_loadPerNode[nodeId] += amount;
		}
	}
}
