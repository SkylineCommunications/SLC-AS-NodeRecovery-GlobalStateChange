namespace NodeRecoveryGlobalStateChange
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.NodeRecovery;
	using Skyline.DataMiner.Net.Swarming;

	/// <summary>
	/// Script that is triggered by NodeRecovery on global state changes.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The script entry point for NodeRecovery.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		/// <param name="input">Input from NodeRecovery.</param>
		/// <returns>Output returned to NodeRecovery.</returns>
		[AutomationEntryPoint(AutomationEntryPointType.Types.OnNodeRecoveryGlobalStateChange)]
		public GlobalStateChangeOutput OnNodeRecoveryGlobalStateChange(IEngine engine, GlobalStateChangeInput input)
		{
			try
			{
				return RunSafe(engine, input);
			}
			catch (ScriptAbortException)
			{
				// Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
				throw; // Comment if it should be treated as a normal exit of the script.
			}
			catch (ScriptForceAbortException)
			{
				// Catch forced abort exceptions, caused via external maintenance messages.
				throw;
			}
			catch (ScriptTimeoutException)
			{
				// Catch timeout exceptions for when a script has been running for too long.
				throw;
			}
			catch (InteractiveUserDetachedException)
			{
				// Catch a user detaching from the interactive script by closing the window.
				// Only applicable for interactive scripts, can be removed for non-interactive scripts.
				throw;
			}
			catch (Exception e)
			{
				engine.ExitFail("Run|Something went wrong: " + e);
				return default;
			}
		}

		private GlobalStateChangeOutput RunSafe(IEngine engine, GlobalStateChangeInput input)
		{
			engine.Timeout = TimeSpan.FromMinutes(30);

			var connection = engine.GetUserConnection();

			// Dynamically fetch discovery messages from handlers
			var discoveryMessages = SwarmingContext
				.Handlers
				.Values
				.Select(h => h.DiscoveryMessage)
				.Where(m => m != null)
				.ToArray();

			var infoEvents = connection.HandleMessages(discoveryMessages);

			var swarmingObjects = SwarmingContext.ConvertInfoEvents(infoEvents);

			var (healhtyTargets, outageSources) = SwarmingTargets.Calculate(input);

			var swarmingRequests = SwarmingCalculator.CalculateSwarmingRequests(
				healhtyTargets,
				outageSources,
				swarmingObjects);

			if (swarmingRequests.Count == 0)
			{
				engine.GenerateInformation("NodeRecovery: Nothing needs to be swarmed.");
				return default;
			}

			int totalObjects = swarmingRequests.Values.Sum(reqs => reqs.Sum(req => req.DmaObjectRefs.Length));
			engine.GenerateInformation($"NodeRecovery: Swarming {totalObjects} object(s) to {swarmingRequests.Count} agents.");

			// Create a nested ExecuteArrayMessage to run in parallel per agent
			// However, per agent we then run sequentially per object type to avoid overloading the agent
			// and prioritizing the more important object types first (services are not worth much without elements).
			var sequentialWrappersPerAgent = swarmingRequests.Values.Select(arr => new ExecuteArrayMessage(arr)).ToArray<DMSMessage>();
			var parallelWrapper = new ExecuteArrayMessage(sequentialWrappersPerAgent, ExecuteArrayOptions.Parallel);

			var parallelWrapperResponse = connection.HandleSingleResponseMessage(parallelWrapper) as ExecuteArrayResponse;

			if (parallelWrapperResponse == null)
			{
				engine.ExitFail("NodeRecovery: Swarming execution failed, no response for swarming requests");
				return default;
			}

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

			if (failures.Count == 0)
				engine.GenerateInformation("NodeRecovery: All swarming requests succeeded.");
			else
				engine.GenerateInformation($"NodeRecovery: {failures.Count} object(s) failed to swarm.");

			return default;
		}
	}
}
