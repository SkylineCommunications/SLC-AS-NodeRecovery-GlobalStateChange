namespace NodeRecoveryGlobalStateChange
{
	/// <summary>
	/// List of types we keep in mind when calculating swarming assignments.
	/// Each type must have a corresponding handler in SwarmingContext.Handlers.
	///
	/// Note: The order of the enum values defines the priority during swarming (Element > Service > RedundancyGroup).
	/// </summary>
	public enum SwarmingObjectType
	{
		// Highest priority
		Element = 0,
		Service = 1, // not swarmable yet but used for load balancing
		RedundancyGroup = 2, // not swarmable yet but used for load balancing

		// Lowest priority
	}
}
