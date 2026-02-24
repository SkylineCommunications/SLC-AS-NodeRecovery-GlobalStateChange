namespace NodeRecoveryGlobalStateChange.Tests
{
	using System.Collections.Generic;
	using System.Linq;
	using NUnit.Framework;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.NodeRecovery;

	[TestFixture]
	public class SwarmingCalculatorTests
	{
		[Test]
		public void NoHealthyNodes_ReturnsEmpty()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Outage } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void NoOutageNodes_ReturnsEmpty()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Healthy } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void NoObjectsOnOutageNodes_ReturnsEmpty()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 2, isSwarmable: false),
				CreateElement(753, 101, hostingAgentId: 2),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void NoSwarmableObjectsOnOutageNodes_ReturnsEmpty()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1, isSwarmable: false),
				CreateElement(753, 101, hostingAgentId: 2),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void NodeInMaintenance_IsIgnored()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Outage, InMaintenance = true } }, // In maintenance
				{ 3, new NodeStateInfo { State = NodeState.Healthy, InMaintenance = true } }, // In maintenance
				{ 4, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1),
				CreateElement(753, 101, hostingAgentId: 2),
				CreateElement(753, 102, hostingAgentId: 3),
				CreateElement(753, 103, hostingAgentId: 4),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.ContainsKey(4), Is.True); // Should only have requests for node 4
			var swarmingRequests = result[4];
			Assert.That(swarmingRequests, Has.Length.EqualTo(1));
			Assert.That(swarmingRequests[0].DmaObjectRefs, Has.Length.EqualTo(1)); // Only object from node 1 should be moved
			Assert.That(swarmingRequests[0].DmaObjectRefs[0].ToString(), Contains.Substring("753/100")); // Element 753/100
		}

		[Test]
		public void UnknownNode_IsIgnored()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Unknown} },
				{ 3, new NodeStateInfo { State = NodeState.Unknown} },
				{ 4, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1),
				CreateElement(753, 101, hostingAgentId: 2),
				CreateElement(753, 102, hostingAgentId: 3),
				CreateElement(753, 103, hostingAgentId: 4),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.ContainsKey(4), Is.True); // Should only have requests for node 4
			var swarmingRequests = result[4];
			Assert.That(swarmingRequests, Has.Length.EqualTo(1));
			Assert.That(swarmingRequests[0].DmaObjectRefs, Has.Length.EqualTo(1)); // Only object from node 1 should be moved
			Assert.That(swarmingRequests[0].DmaObjectRefs[0].ToString(), Contains.Substring("753/100")); // Element 753/100
		}

		[Test]
		public void SingleObjectToMove_MovesToHealthyNode()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.ContainsKey(2), Is.True);
			var swarmingRequests = result[2];
			Assert.That(swarmingRequests[0].DmaObjectRefs, Has.Length.EqualTo(1));
		}

		[Test]
		public void MultipleObjects_DistributesBasedOnLoad()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
				{ 3, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>();

			// Existing load on node 2 (total weight 10)
			for (int i = 1; i <= 10; i++)
			{
				allObjects.Add(CreateElement(753, i, hostingAgentId: 2));
			}

			// Objects to move from node 1
			allObjects.Add(CreateElement(753, 100, hostingAgentId: 1));
			allObjects.Add(CreateElement(753, 101, hostingAgentId: 1));

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert - both objects should go to node 3 (least loaded)
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.ContainsKey(3), Is.True);
			Assert.That(result[3].Sum(r => r.DmaObjectRefs.Length), Is.EqualTo(2));
		}

		[Test]
		public void LoadBalancing_DistributesEvenly()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
				{ 3, new NodeStateInfo { State = NodeState.Healthy } },
			};

			// 4 objects to move, should be distributed 2-2 between nodes 2 and 3
			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1),
				CreateElement(753, 101, hostingAgentId: 1),
				CreateElement(753, 102, hostingAgentId: 1),
				CreateElement(753, 103, hostingAgentId: 1),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert - should have requests to both nodes
			Assert.That(result, Has.Count.EqualTo(2));

			// Each node should get 2 objects
			foreach (var requests in result.Values)
			{
				Assert.That(requests.Sum(r => r.DmaObjectRefs.Length), Is.EqualTo(2));
			}
		}

		[Test]
		public void LoadBalancing_DistributesEvenly_MultiOutage()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Outage } },
				{ 3, new NodeStateInfo { State = NodeState.Healthy } },
			};

			// 4 objects to move, should all go to node 3
			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1),
				CreateElement(753, 101, hostingAgentId: 1),
				CreateElement(753, 102, hostingAgentId: 1),
				CreateElement(753, 103, hostingAgentId: 2),
				CreateElement(753, 104, hostingAgentId: 3),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert - should have requests to node 3 only
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result.ContainsKey(3), Is.True);
			var requests = result[3];
			Assert.That(requests.Sum(r => r.DmaObjectRefs.Length), Is.EqualTo(4));
		}

		[Test]
		public void WeightedObjects_ConsidersWeight()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
				{ 3, new NodeStateInfo { State = NodeState.Healthy } },
			};

			// First object has weight 10 (e.g., parent with 9 DVE children)
			// Second object has weight 1
			// Third object has weight 1
			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 100, hostingAgentId: 1, weight: 10),
				CreateElement(753, 101, hostingAgentId: 1, weight: 1),
				CreateElement(753, 102, hostingAgentId: 1, weight: 1),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Has.Count.EqualTo(2));

			var heavyRequestNode = result.Values.First(msgs => msgs.Any(m => m.DmaObjectRefs.Any(o => o.ToString().Contains("753/100"))));
			var lightRequestNode = result.Values.First(msgs => msgs != heavyRequestNode);

			Assert.That(heavyRequestNode.Sum(r => r.DmaObjectRefs.Length), Is.EqualTo(1));
			Assert.That(lightRequestNode.Sum(r => r.DmaObjectRefs.Length), Is.EqualTo(2));
		}

		[Test]
		public void NullClusterState_ThrowsArgumentNullException()
		{
			Assert.That(
				() => SwarmingCalculator.CalculateSwarmingRequests(null, new List<SwarmingObject>()),
				Throws.ArgumentNullException);
		}

		[Test]
		public void NullAllObjects_ThrowsArgumentNullException()
		{
			Assert.That(
				() => SwarmingCalculator.CalculateSwarmingRequests(new Dictionary<int, NodeStateInfo>(), null),
				Throws.ArgumentNullException);
		}

		[Test]
		public void OneLargeWeightWithSmallItems_MinimizesLoadDifference()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
				{ 3, new NodeStateInfo { State = NodeState.Healthy } },
			};

			// One large element with weight 50 (e.g., parent with 49 DVE children)
			// Five small elements with weight 1 each
			var allObjects = new List<SwarmingObject>
			{
				CreateElement(753, 101, hostingAgentId: 1, weight: 1),
				CreateElement(753, 102, hostingAgentId: 1, weight: 1),
				CreateElement(753, 103, hostingAgentId: 1, weight: 1),
				CreateElement(753, 104, hostingAgentId: 1, weight: 1),
				CreateElement(753, 105, hostingAgentId: 1, weight: 1),
				CreateElement(753, 106, hostingAgentId: 1, weight: 50),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Has.Count.EqualTo(2));

			int load1 = result.Values.ElementAt(0).Sum(r => r.DmaObjectRefs.Length);
			int load2 = result.Values.ElementAt(1).Sum(r => r.DmaObjectRefs.Length);

			// One node should have the heavy item (1) and the other all small items (5)
			Assert.That(new[] { load1, load2 }, Is.EquivalentTo(new[] { 1, 5 }));

			// Also assert the single one is the large weight item
			var heavyNodeRequests = result.Values.First(r => r.Sum(req => req.DmaObjectRefs.Length) == 1);
			Assert.That(heavyNodeRequests.SelectMany(r => r.DmaObjectRefs).First().ToString(), Does.Contain("106"));
		}

		[Test]
		public void PriorityRespected_SwarmingOrderFollowsEnum()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				CreateRedundancyGroup(753, 300, hostingAgentId: 1),
				CreateService(753, 200, hostingAgentId: 1),
				CreateElement(753, 100, hostingAgentId: 1),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Has.Count.EqualTo(1));
			var requests = result[2];

			Assert.That(requests, Has.Length.EqualTo(3));

			// Verify types order: Element (0), Service (1), RedundancyGroup (2)
			Assert.That(requests[0].DmaObjectRefs.All(r => r is ElementID), Is.True, "First request should be Elements");
			Assert.That(requests[1].DmaObjectRefs.All(r => r is ServiceID), Is.True, "Second request should be Services");
			Assert.That(requests[2].DmaObjectRefs.All(r => r is RedundancyGroupID), Is.True, "Third request should be Redundancy Groups");
		}

		[Test]
		public void MixedObjectTypes_OnlyElementsSwarmable_ConsidersAllLoadForBalancing()
		{
			// Arrange
			var clusterState = new Dictionary<int, NodeStateInfo>
			{
				{ 1, new NodeStateInfo { State = NodeState.Outage } },
				{ 2, new NodeStateInfo { State = NodeState.Healthy } },
				{ 3, new NodeStateInfo { State = NodeState.Healthy } },
			};

			var allObjects = new List<SwarmingObject>
			{
				// Node 1 (Outage) - has elements (swarmable), services and redundancy groups (not swarmable)
				CreateElement(753, 100, hostingAgentId: 1, isSwarmable: true, weight: 2),
				CreateElement(753, 101, hostingAgentId: 1, isSwarmable: true, weight: 2),
				CreateElement(753, 102, hostingAgentId: 1, isSwarmable: true, weight: 2),
				CreateService(753, 200, hostingAgentId: 1, isSwarmable: false, weight: 1),
				CreateRedundancyGroup(753, 300, hostingAgentId: 1, isSwarmable: false, weight: 1),

				// Total Node 2 weight: 4
				CreateElement(753, 20, hostingAgentId: 2, isSwarmable: true, weight: 1),
				CreateService(753, 210, hostingAgentId: 2, isSwarmable: false, weight: 3),

				// Total Node 3 weight: 5
				CreateElement(753, 30, hostingAgentId: 3, isSwarmable: true, weight: 2),
				CreateRedundancyGroup(753, 310, hostingAgentId: 3, isSwarmable: false, weight: 3),
			};

			// Act
			var result = SwarmingCalculator.CalculateSwarmingRequests(clusterState, allObjects);

			// Assert
			Assert.That(result, Is.Not.Empty);

			var totalElementsMoved = result.Values.SelectMany(v => v).Sum(r => r.DmaObjectRefs.Length);
			Assert.That(totalElementsMoved, Is.EqualTo(3));

			// Node 2 (weight 4) should get 2 elements (4 + 2x2 = 8 weight)
			// Node 3 (weight 5) should get 1 element (5 + 1x2 = 7 weight)
			Assert.That(result[2].Sum(r => r.DmaObjectRefs.Length), Is.EqualTo(2));
			Assert.That(result[3].Sum(r => r.DmaObjectRefs.Length), Is.EqualTo(1));
		}

		private static SwarmingObject CreateElement(
			int dataminerId,
			int elementId,
			int hostingAgentId,
			bool isSwarmable = true,
			int weight = 1)
		{
			return new SwarmingObject
			{
				Id = new ElementID(dataminerId, elementId),
				Type = SwarmingObjectType.Element,
				HostingAgentId = hostingAgentId,
				IsSwarmable = isSwarmable,
				Weight = weight,
			};
		}

		private static SwarmingObject CreateService(
			int dataminerId,
			int elementId,
			int hostingAgentId,
			bool isSwarmable = true,
			int weight = 1)
		{
			return new SwarmingObject
			{
				Id = new ServiceID(dataminerId, elementId),
				Type = SwarmingObjectType.Service,
				HostingAgentId = hostingAgentId,
				IsSwarmable = isSwarmable,
				Weight = weight,
			};
		}

		private static SwarmingObject CreateRedundancyGroup(
			int dataminerId,
			int elementId,
			int hostingAgentId,
			bool isSwarmable = true,
			int weight = 1)
		{
			return new SwarmingObject
			{
				Id = new RedundancyGroupID(dataminerId, elementId),
				Type = SwarmingObjectType.RedundancyGroup,
				HostingAgentId = hostingAgentId,
				IsSwarmable = isSwarmable,
				Weight = weight,
			};
		}
	}
}
