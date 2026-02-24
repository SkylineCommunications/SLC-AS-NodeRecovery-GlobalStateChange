namespace NodeRecoveryGlobalStateChange
{
	using Skyline.DataMiner.Net;

	/// <summary>
	/// Represents an object that can be swarmed between nodes.
	/// </summary>
	public class SwarmingObject
	{
		/// <summary>
		/// Gets or sets the unique key for the object in DMS.
		/// </summary>
		public DMAObjectRef Id { get; set; }

		/// <summary>
		/// Gets or sets the type of the object, e.g., Element or Service.
		/// </summary>
		public SwarmingObjectType Type { get; set; }

		/// <summary>
		/// Gets or sets the ID of the agent currently hosting the object.
		/// </summary>
		public int HostingAgentId { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the object can be swarmed.
		/// </summary>
		public bool IsSwarmable { get; set; }

		/// <summary>
		/// Gets or sets the weight of this object for load balancing purposes.
		/// For elements, this includes the parent (1) plus any DVE children.
		/// For services, this is typically 1.
		/// </summary>
		public int Weight { get; set; } = 1;
	}
}
