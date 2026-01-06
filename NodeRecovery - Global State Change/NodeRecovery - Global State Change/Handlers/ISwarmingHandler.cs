namespace NodeRecoveryGlobalStateChange.Handlers
{
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Defines logic for handling specific swarming types.
	/// </summary>
	public interface ISwarmingHandler
	{
		DMSMessage DiscoveryMessage { get; }

		bool CanHandle(DMSMessage message);

		SwarmingObject Convert(DMSMessage message, SwarmingContext context);
	}
}
