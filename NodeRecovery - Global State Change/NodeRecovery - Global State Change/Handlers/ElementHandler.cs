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
			context.ChildCountByParent.TryGetValue(id, out int childCount);
			return new SwarmingObject
			{
				Id = id,
				Type = SwarmingObjectType.Element,
				HostingAgentId = infoEvent.HostingAgentID,
				IsSwarmable = infoEvent.IsSwarmable,
				Weight = 1 + childCount,
			};
		}
	}
}
