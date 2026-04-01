# Node Recovery - Rebalance across healthy agents

## About

This package deploys the *NodeRecovery - Global State Change* automation script, which is used by the [Node Recovery](http://aka.dataminer.services/NodeRecovery) module. It triggers on global state changes in a cluster environment.

This script makes sure that objects hosted on nodes in outage are swarmed to healthy nodes in the cluster, ensuring high availability and minimizing downtime.

A rudimentary load balancing strategy is implemented to avoid overloading healthy nodes during the swarming process. The number of elements, services, and redundancy groups are counted and kept in mind when selecting target nodes for swarming. Objects hosted on nodes not in outage are not affected.

Performance optimizations have been made to ensure the script runs efficiently, even in large cluster environments. Swarming operations happen in parallel per agent, where the swarming per agent happens in batches, per object type.

## Key Features

- Monitors global state changes of agents in a cluster
- Automatically triggers swarming of objects from nodes in outage to healthy nodes
- Implements basic load balancing during the swarming process
- Ignores nodes that are in maintenance mode or in an unknown state
- Enhances system reliability and availability

## Prerequisites

- DataMiner version 10.6.3/10.6.0 or newer
- Swarming must be enabled
- DataMiner Node Recovery DxM module must be installed
