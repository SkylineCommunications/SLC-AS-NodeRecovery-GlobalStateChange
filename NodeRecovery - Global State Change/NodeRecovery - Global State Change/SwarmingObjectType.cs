namespace NodeRecoveryGlobalStateChange
{
	/// <summary>
	/// List of types we keep in mind when calculating swarming assignments.
	/// Each type must have a corresponding handler implementing ISwarmingHandler.
	///
	/// Note: The order of the enum values defines the priority during swarming (Element > Service > RedundancyGroup).
	/// </summary>
	public enum SwarmingObjectType
	{
		Element,
		Service,
		RedundancyGroup,
	}
}
