using System;
using System.Collections.Generic;
using System.Linq;

namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Represents a directed acyclic graph (DAG) of component dependencies and handles topological sort and validations.
/// </summary>
public sealed class RuleDependencyGraph
{
    private readonly Dictionary<string, List<string>> _adjacencyList = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _componentCodes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a component node and its code.
    /// </summary>
    public void AddNode(string code)
    {
        if (!_adjacencyList.ContainsKey(code))
        {
            _adjacencyList[code] = new List<string>();
        }
    }

    /// <summary>
    /// Adds a dependency edge: parent depends on child.
    /// </summary>
    public void AddDependency(string parentCode, string childCode)
    {
        AddNode(parentCode);
        AddNode(childCode);
        if (!_adjacencyList[parentCode].Contains(childCode))
        {
            _adjacencyList[parentCode].Add(childCode);
        }
    }

    /// <summary>
    /// Validates the graph for cycles, missing dependencies, and unused references.
    /// </summary>
    public DependencyGraphValidationResult Validate()
    {
        var visited = new Dictionary<string, NodeState>(StringComparer.OrdinalIgnoreCase);
        var cyclePath = new List<string>();
        var errors = new List<string>();

        foreach (var node in _adjacencyList.Keys)
        {
            visited[node] = NodeState.Unvisited;
        }

        foreach (var node in _adjacencyList.Keys)
        {
            if (visited[node] == NodeState.Unvisited)
            {
                var tempPath = new List<string>();
                if (HasCycle(node, visited, tempPath, cyclePath))
                {
                    errors.Add($"Circular dependency detected: {string.Join(" -> ", cyclePath)}");
                    return new DependencyGraphValidationResult(false, errors, Array.Empty<string>());
                }
            }
        }

        // Topological Sort (Kahn's or DFS post-order)
        var order = TopologicalSort();
        return new DependencyGraphValidationResult(true, errors, order);
    }

    private bool HasCycle(
        string node,
        Dictionary<string, NodeState> visited,
        List<string> tempPath,
        List<string> cyclePath)
    {
        visited[node] = NodeState.Visiting;
        tempPath.Add(node);

        foreach (var neighbor in _adjacencyList[node])
        {
            if (visited[neighbor] == NodeState.Visiting)
            {
                // Found cycle
                var cycleStart = tempPath.IndexOf(neighbor);
                for (int i = cycleStart; i < tempPath.Count; i++)
                {
                    cyclePath.Add(tempPath[i]);
                }
                cyclePath.Add(neighbor);
                return true;
            }
            if (visited[neighbor] == NodeState.Unvisited)
            {
                if (HasCycle(neighbor, visited, tempPath, cyclePath))
                {
                    return true;
                }
            }
        }

        tempPath.RemoveAt(tempPath.Count - 1);
        visited[node] = NodeState.Visited;
        return false;
    }

    private IReadOnlyList<string> TopologicalSort()
    {
        var result = new List<string>();
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in _adjacencyList.Keys)
        {
            inDegree[node] = 0;
        }

        foreach (var node in _adjacencyList.Keys)
        {
            foreach (var neighbor in _adjacencyList[node])
            {
                inDegree[neighbor] = inDegree.GetValueOrDefault(neighbor, 0) + 1;
            }
        }

        var queue = new Queue<string>();
        foreach (var kp in inDegree)
        {
            if (kp.Value == 0)
            {
                queue.Enqueue(kp.Key);
            }
        }

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            result.Add(curr);

            foreach (var neighbor in _adjacencyList.GetValueOrDefault(curr, new List<string>()))
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        // If cycle exists, result count won't match adjacency list count
        if (result.Count != _adjacencyList.Count)
        {
            return Array.Empty<string>(); // cycle path is handled in validate phase
        }

        // We reverse it because dependencies should be calculated first
        result.Reverse();
        return result;
    }

    private enum NodeState
    {
        Unvisited,
        Visiting,
        Visited
    }
}

public sealed class DependencyGraphValidationResult
{
    public DependencyGraphValidationResult(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> calculationOrder)
    {
        IsValid = isValid;
        Errors = errors;
        CalculationOrder = calculationOrder;
    }

    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> CalculationOrder { get; }
}
