namespace NodeRecoveryGlobalStateChange
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Swarming;

	internal static class SwarmingExecutor
	{
		internal static void ExecuteWithRetry(
			IEngine engine,
			IConnection connection,
			HashSet<int> healthyTargets,
			Dictionary<int, SwarmingRequestMessage[]> swarmingRequests)
		{
			const int maxRetries = 3;
			var pendingRequests = swarmingRequests;
			var failures = new List<SwarmingResult>();
			var remainingHealthyTargets = healthyTargets.ToList();

			for (int attempt = 1; attempt <= maxRetries && pendingRequests.Count > 0; attempt++)
			{
				if (attempt > 1)
				{
					engine.GenerateInformation($"NodeRecovery: Attempt {attempt}/{maxRetries} ({failures.Count} object(s) left) ...");
					Thread.Sleep(TimeSpan.FromSeconds(5 * attempt));
				}

				failures = Execute(connection, pendingRequests);

				if (failures.Count == 0)
				{
					engine.GenerateInformation("NodeRecovery: All swarming requests succeeded.");
					return;
				}
				else
				{
					foreach (var failure in failures)
						engine.Log($"NodeRecovery: Swarming failed for object {failure.DmaObjectRef} to target agent {failure.TargetDmaId}: {failure.Message}");
				}

				if (attempt < maxRetries)
				{
					engine.GenerateInformation($"NodeRecovery: {failures.Count} object(s) failed to swarm, retrying...");

					// Exclude "healhty" agents that just had a swarm failures, something might be wrong
					// However the failure might also be due to the object in question
					// We do a heuristic here where if we see multiple failures for the same target agent,
					// we consider that agent as unhealthy and exclude it from the retries
					const int failureThreshold = 3;
					var targetsToExclude = failures
						.GroupBy(f => f.TargetDmaId)
						.Where(g => g.Count() >= failureThreshold)
						.ToDictionary(g => g.Key, g => g.Count());

					foreach (var kvp in targetsToExclude)
					{
						engine.Log($"NodeRecovery: Excluding target agent {kvp.Key} from retries due to {kvp.Value} (>={failureThreshold}) failures.");
					}

					remainingHealthyTargets = remainingHealthyTargets
						.Except(targetsToExclude.Keys)
						.ToList();

					if (remainingHealthyTargets.Count == 0)
					{
						engine.GenerateInformation("NodeRecovery: No remaining healthy targets to retry failed swarms.");
						return;
					}

					pendingRequests = RedistributeFailedObjects(failures, remainingHealthyTargets);
				}
				else
				{
					engine.GenerateInformation($"NodeRecovery: {failures.Count} object(s) failed to swarm.");
				}
			}
		}

		private static List<SwarmingResult> Execute(IConnection connection, Dictionary<int, SwarmingRequestMessage[]> swarmingRequests)
		{
			if (swarmingRequests.Count == 0)
				return new List<SwarmingResult>();

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

		private static Dictionary<int, SwarmingRequestMessage[]> RedistributeFailedObjects(
			List<SwarmingResult> failures,
			List<int> healhtyTargets)
		{
			if (failures.Count == 0)
				return new Dictionary<int, SwarmingRequestMessage[]>();

			// Simple round-robin distribution to any healthy node
			var redistributed = new Dictionary<int, List<DMAObjectRef>>();
			int nodeIndex = 0;

			foreach (var failure in failures)
			{
				int targetNode = healhtyTargets[nodeIndex % healhtyTargets.Count];
				nodeIndex++;

				if (!redistributed.TryGetValue(targetNode, out var list))
				{
					list = new List<DMAObjectRef>();
					redistributed[targetNode] = list;
				}

				list.Add(failure.DmaObjectRef);
			}

			return redistributed.ToDictionary(
				kvp => kvp.Key,
				kvp => new[] { new SwarmingRequestMessage { TargetDmaId = kvp.Key, DmaObjectRefs = kvp.Value.ToArray() } });
		}
	}
}
