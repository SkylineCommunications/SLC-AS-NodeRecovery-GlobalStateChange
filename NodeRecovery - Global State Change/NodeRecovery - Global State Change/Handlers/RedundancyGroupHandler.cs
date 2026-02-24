namespace NodeRecoveryGlobalStateChange.Handlers
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public class RedundancyGroupHandler : ISwarmingHandler
	{
		public DMSMessage DiscoveryMessage
			=> new GetLiteRedundancyGroupInfo();

		public bool CanHandle(DMSMessage message)
			=> message is LiteRedundancyGroupInfoEvent;

		public SwarmingObject Convert(DMSMessage message, SwarmingContext context)
		{
			if (!(message is LiteRedundancyGroupInfoEvent infoEvent))
				return null;

			return new SwarmingObject
			{
				Id = new RedundancyGroupID(infoEvent.DataMinerID, infoEvent.ElementID),
				Type = SwarmingObjectType.RedundancyGroup,
				HostingAgentId = infoEvent.HostingAgentID,
				IsSwarmable = false, // not swarmable yet
				Weight = 1,
			};
		}
	}
}
