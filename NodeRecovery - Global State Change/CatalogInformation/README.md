# Node Recovery - Rebalance across healthy agents

## About

Deploys an automation script ("NodeRecovery - Global State Change") used by the [NodeRecovery DxM](http://aka.dataminer.services/NodeRecovery) module that triggers on global state changes in a cluster environment.

This script ensures objects hosted on nodes in outage are swarmed to healthy nodes in the cluster, ensuring high availability and minimizing downtime.
A rudimentary load balancing strategy is implemented to avoid overloading healthy nodes during the swarming process.
The number of elements/services/redundancy groups are counted and kept in mind when selecting target nodes for swarming.
Objects hosted on nodes not in outage are not affected.

Performance optimizations have been made to ensure the script runs efficiently, even in large cluster environments.
Swarming operations happen in parallel per agent, where the swarming per agent happens in batches, per object type.

## Key Features

- Monitors global state changes of agents in a cluster
- Automatically triggers swarming of objects from nodes in outage to healthy nodes
- Implements basic load balancing during the swarming process
- Ignores nodes that are in maintenance mode or in unknown state
- Enhances system reliability and availability

## Prerequisites

- DataMiner version 10.6.3 or higher
- Swarming must be enabled
- DataMiner NodeRecovery DxM module installed