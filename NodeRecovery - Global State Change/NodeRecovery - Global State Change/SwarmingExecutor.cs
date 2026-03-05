namespace NodeRecoveryGlobalStateChange
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Swarming;

	internal static class SwarmingExecutor
	{
		internal static List<SwarmingResult> Execute(IConnection connection, Dictionary<int, SwarmingRequestMessage[]> swarmingRequests)
		{
			// Create a nested ExecuteArrayMessage to run in parallel per agent
			// However, per agent we then run sequentially per object type to avoid overloading the agent
			// and prioritizing the more important object types first (services are not worth much without elements).
			// Each object type is contained in a single SwarmingRequestMessage (DMAObjectref[])
			// which is then handled by the standard swarming flow for that type (limited parallel for elements)
			var sequentialWrappersPerAgent = swarmingRequests.Values.Select(arr => new ExecuteArrayMessage(arr)).ToArray<DMSMessage>();
			var parallelWrapper = new ExecuteArrayMessage(sequentialWrappersPerAgent, ExecuteArrayOptions.Parallel);

			var parallelWrapperResponse = connection.HandleSingleResponseMessage(parallelWrapper) as ExecuteArrayResponse;

			if (parallelWrapperResponse == null)
				throw new DataMinerException("NodeRecovery: Swarming execution failed, no response for swarming requests");

			// Unwrap the parallel wrapper
			var sequentialWrapperResponses = parallelWrapperResponse
				.Responses
				.SelectMany(executeResponse => executeResponse.Responses)
				.OfType<ExecuteArrayResponse>();

			// Unwrap the sequential wrappers and flatten the list of list to get single collection
			var swarmingResponses = sequentialWrapperResponses
				.SelectMany(executeResponse => executeResponse
					.Responses
					.SelectMany(sequentialWrapperResponse => sequentialWrapperResponse.Responses))
				.OfType<SwarmingResponseMessage>();

			// For each SwarmingResponseMessage, collect the failed results
			var failures = swarmingResponses
				.SelectMany(resp => resp.SwarmingResults)
				.Where(res => !res.Success)
				.ToList();

			return failures;
		}
	}
}
