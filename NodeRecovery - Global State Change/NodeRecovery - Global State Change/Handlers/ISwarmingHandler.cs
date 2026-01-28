namespace NodeRecoveryGlobalStateChange.Handlers
{
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Defines logic for handling specific swarming types.
	/// One handler is expected per object type.
	/// </summary>
	public interface ISwarmingHandler
	{
		/// <summary>
		/// Gets the SLNet message that fetches the info events for this handler's type.
		/// These events are returned in bulk by DMS during discovery.
		/// </summary>
		DMSMessage DiscoveryMessage { get; }

		/// <summary>
		/// Determines whether the specified message (passed as generic DMSMessage) can be handled by the current handler.
		/// As the information is requested in bulk. The responses cannot be linked back to an original handler.
		/// This filters out the desired messages.
		/// </summary>
		/// <param name="message">The message to evaluate for handling. Cannot be null.</param>
		/// <returns>true if the handler can process the specified message.</returns>
		bool CanHandle(DMSMessage message);

		/// <summary>
		/// Converts the info event message of this type to a generic <see cref="SwarmingObject"/> used by the load balancer.
		/// </summary>
		/// <param name="message">the message to convert.</param>
		/// <param name="context">the general context of the action.</param>
		/// <returns>the generic object used to calculate later.</returns>
		SwarmingObject Convert(DMSMessage message, SwarmingContext context);
	}
}
