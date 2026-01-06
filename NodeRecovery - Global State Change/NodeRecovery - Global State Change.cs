namespace NodeRecoveryGlobalStateChange
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.NodeRecovery;
	using Skyline.DataMiner.Net.Swarming;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		/// <param name="input">Input from NodeRecovery.</param>
		/// <returns>Output returned to NodeRecovery.</returns>
		[AutomationEntryPoint(AutomationEntryPointType.Types.OnNodeRecoveryLocalStateChange)]
		public LocalStateChangeOutput OnNodeRecoveryLocalStateChange(IEngine engine, LocalStateChangeInput input)
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

		private LocalStateChangeOutput RunSafe(IEngine engine, LocalStateChangeInput input)
		{
			var connection = engine.GetUserConnection();

			// Dynamically fetch discovery messages from handlers
			var discoveryMessages = SwarmingContext
				.Handlers
				.Values
				.Select(h => h.DiscoveryMessage)
				.Where(m => m != null)
				.ToArray();

			var infoEvents = connection.HandleMessages(discoveryMessages);

			var swarmingObjects = ConvertInfoEvents(infoEvents);

			var swarmingRequests = SwarmingCalculator.CalculateSwarmingRequests(input.ClusterState, swarmingObjects);

			if (swarmingRequests.Count == 0)
			{
				engine.GenerateInformation("Node Recovery: Nothing need to be swarmed.");
				return default;
			}

			int totalObjects = swarmingRequests.Values.Sum(reqs => reqs.Sum(req => req.DmaObjectRefs.Length));
			engine.GenerateInformation($"Node Recovery: Swarming {totalObjects} object(s) to {swarmingRequests.Count} agents.");

			// Create a nested ExecuteArrayMessage to run in parallel per agent
			// However, per agent we then run sequentially per object type to avoid overloading the agent
			// and prioritizing the more important object types first (services are not worth much without elements).
			var sequentialWrappersPerAgent = swarmingRequests.Values.Select(arr => new ExecuteArrayMessage(arr)).ToArray();
			var parellelWrapper = new ExecuteArrayMessage(sequentialWrappersPerAgent, ExecuteArrayOptions.Parallel);

			var response = connection.HandleSingleResponseMessage(parellelWrapper) as ExecuteArrayResponse;

			if (response == null)
				return default;

			var failures = response
				.Responses.SelectMany(r => r.Responses)
				.OfType<ExecuteArrayResponse>()
				.SelectMany(r => r.Responses.SelectMany(r2 => r2.Responses))
				.OfType<SwarmingResponseMessage>()
				.SelectMany(resp => resp.SwarmingResults)
				.Where(res => !res.Success)
				.ToList();

			if (failures.Count == 0)
				engine.GenerateInformation("Node Recovery: All swarming requests succeeded.");
			else
				engine.GenerateInformation($"Node Recovery: {failures.Count} object(s) failed to swarm.");

			return default;
		}

		private List<SwarmingObject> ConvertInfoEvents(DMSMessage[] msgs)
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
				foreach (var handler in SwarmingContext.Handlers.Values)
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
