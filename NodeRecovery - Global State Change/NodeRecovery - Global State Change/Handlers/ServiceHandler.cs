namespace NodeRecoveryGlobalStateChange.Handlers
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public class ServiceHandler : ISwarmingHandler
	{
		public DMSMessage DiscoveryMessage
			=> new GetLiteServiceInfo();

		public bool CanHandle(DMSMessage message)
			=> message is LiteServiceInfoEvent;

		public SwarmingObject Convert(DMSMessage message, SwarmingContext context)
		{
			if (!(message is LiteServiceInfoEvent infoEvent))
				return null;

			return new SwarmingObject
			{
				Id = new ServiceID(infoEvent.DataMinerID, infoEvent.ElementID),
				Type = SwarmingObjectType.Service,
				HostingAgentId = infoEvent.HostingAgentID,
				IsSwarmable = false, // not swarmable yet
				Weight = 1,
			};
		}
	}
}
