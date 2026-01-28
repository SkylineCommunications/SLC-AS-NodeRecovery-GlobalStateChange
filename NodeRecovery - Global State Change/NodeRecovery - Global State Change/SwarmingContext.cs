namespace NodeRecoveryGlobalStateChange
{
	using System.Collections.Generic;
	using System.Linq;
	using NodeRecoveryGlobalStateChange.Handlers;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Context used during message conversion.
	/// </summary>
	public class SwarmingContext
	{
		/// <summary>
		/// Gets the available handlers (<see cref="ISwarmingHandler"/>), one for each <see cref="SwarmingObjectType"/>.
		/// Specifies what info to request from SLNet and how to convert it to generic SwarmingObjects.
		/// </summary>
		public static IReadOnlyDictionary<SwarmingObjectType, ISwarmingHandler> Handlers { get; } = new Dictionary<SwarmingObjectType, ISwarmingHandler>
		{
			{ SwarmingObjectType.Element, new ElementHandler() },
			{ SwarmingObjectType.Service, new ServiceHandler() },
			{ SwarmingObjectType.RedundancyGroup, new RedundancyGroupHandler() },
		};

		/// <summary>
		/// Gets or sets a mapping of ElementID to the number of virtual child elements it has.
		/// Used by DVE/VF parent elements to group them together with their children as these cannot be swarmed individually.
		/// </summary>
		public IReadOnlyDictionary<ElementID, int> ChildCountByParent { get; set; }

		/// <summary>
		/// Helper method that takes in the bulk of SLNet's DMSMessages and converts them to <see cref="SwarmingObject"/> via the specified handlers.
		/// </summary>
		/// <param name="msgs">Bulk messages returned by SLNet.</param>
		/// <returns>Collection of converted <see cref="SwarmingObject"/>.</returns>
		internal static List<SwarmingObject> ConvertInfoEvents(DMSMessage[] msgs)
		{
			var output = new List<SwarmingObject>(msgs.Length);

			var context = new SwarmingContext
			{
				ChildCountByParent = msgs.OfType<LiteElementInfoEvent>()
					.Where(e => e.IsDynamicElement)
					.GroupBy(e => new ElementID(e.DveParentDmaId, e.DveParentElementId))
					.ToDictionary(g => g.Key, g => g.Count()),
			};

			foreach (var msg in msgs)
			{
				foreach (var handler in Handlers.Values)
				{
					if (handler.CanHandle(msg))
					{
						var obj = handler.Convert(msg, context);
						if (obj != null)
							output.Add(obj);
						break;
					}
				}
			}

			return output;
		}
	}
}
