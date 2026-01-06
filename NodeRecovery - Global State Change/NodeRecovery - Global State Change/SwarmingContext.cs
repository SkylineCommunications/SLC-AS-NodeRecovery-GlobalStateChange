namespace NodeRecoveryGlobalStateChange
{
	using System.Collections.Generic;
	using NodeRecoveryGlobalStateChange.Handlers;
	using Skyline.DataMiner.Net;

	/// <summary>
	/// Context used during message conversion.
	/// </summary>
	public class SwarmingContext
	{
		public static IReadOnlyDictionary<SwarmingObjectType, ISwarmingHandler> Handlers { get; } = new Dictionary<SwarmingObjectType, ISwarmingHandler>
		{
			{ SwarmingObjectType.Element, new ElementHandler() },
			{ SwarmingObjectType.Service, new ServiceHandler() },
			{ SwarmingObjectType.RedundancyGroup, new RedundancyGroupHandler() },
		};

		public IReadOnlyDictionary<ElementID, int> ChildCountByParent { get; set; }
	}
}
