namespace NodeRecoveryGlobalStateChange.Handlers
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public class ElementHandler : ISwarmingHandler
	{
		public DMSMessage DiscoveryMessage
			=> new GetLiteElementInfo
			{
				IncludeHidden = true,
				IncludePaused = true,
				IncludeStopped = true,
				IncludeSpecialHidden = true,
				IncludeAggregatorElements = true,
				IncludeServiceElements = true,
			};

		public bool CanHandle(DMSMessage message)
			=> message is LiteElementInfoEvent ev && !ev.IsDynamicElement;

		public SwarmingObject Convert(DMSMessage message, SwarmingContext context)
		{
			if (!(message is LiteElementInfoEvent infoEvent))
				return null;

			var id = new ElementID(infoEvent.DataMinerID, infoEvent.ElementID);
			var weight = 1;

			// For DVE parent elements, we cannot swarm the children on their own.
			// They swarm together with their parent so we increase the weight of the parent element by its child count.
			// Child count will default to 0 if there are no children.
			if (context.ChildCountByParent.TryGetValue(id, out int childCount))
			{
				weight += childCount;
			}

			return new SwarmingObject
			{
				Id = id,
				Type = SwarmingObjectType.Element,
				HostingAgentId = infoEvent.HostingAgentID,
				IsSwarmable = infoEvent.IsSwarmable,
				Weight = weight,
			};
		}
	}
}
