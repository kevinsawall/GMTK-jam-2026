using System.Collections.Generic;
using UnityEngine;

public sealed class GridCell
{
    public Vector3 WorldPosition;
    public bool IsWalkable;
}

[RequireComponent(typeof(Collider))]
public sealed class GridManager : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float cellSize = 0.5f;
    [SerializeField] private bool showGrid = true;
    [SerializeField, Min(0.1f)] private float obstacleCheckHeight = 2f;
    [SerializeField] private LayerMask obstacleLayers = ~0;

    private readonly List<GridCell> cells = new();
    private Collider groundCollider;
    private Bounds gridBounds;
    private int columns;
    private int rows;

    private void Awake()
    {
        BuildGrid();
        RefreshWalkability();
    }

    public bool Raycast(Ray ray, float maxDistance, out RaycastHit hit) =>
        groundCollider.Raycast(ray, out hit, maxDistance);

    public bool TryGetCell(Vector3 worldPosition, out GridCell cell)
    {
        int column = Mathf.FloorToInt((worldPosition.x - gridBounds.min.x) / cellSize);
        int row = Mathf.FloorToInt((worldPosition.z - gridBounds.min.z) / cellSize);
        if (column < 0 || column >= columns || row < 0 || row >= rows)
        {
            cell = null;
            return false;
        }

        cell = cells[ToIndex(column, row)];
        return true;
    }

    public bool TryFindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition, List<GridCell> path)
    {
        path.Clear();
        RefreshWalkability();

        if (!TryGetCell(startWorldPosition, out GridCell start) ||
            !TryGetCell(targetWorldPosition, out GridCell target))
        {
            return false;
        }

        int startIndex = cells.IndexOf(start);
        int targetIndex = cells.IndexOf(target);
        var openSet = new List<int> { startIndex };
        var closedSet = new HashSet<int>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, int> { [startIndex] = 0 };
        int closestReachableIndex = -1;
        float closestTargetDistance = float.PositiveInfinity;

        while (openSet.Count > 0)
        {
            int current = GetLowestCostIndex(openSet, gScore, targetIndex);
            openSet.Remove(current);
            closedSet.Add(current);

            if (cells[current].IsWalkable)
            {
                float targetDistance = GetPlanarSquareDistance(cells[current].WorldPosition, target.WorldPosition);
                if (targetDistance < closestTargetDistance ||
                    (Mathf.Approximately(targetDistance, closestTargetDistance) &&
                     (closestReachableIndex < 0 || gScore[current] < gScore[closestReachableIndex])))
                {
                    closestReachableIndex = current;
                    closestTargetDistance = targetDistance;
                }
            }

            foreach (int neighbor in GetWalkableNeighbors(current))
            {
                if (closedSet.Contains(neighbor)) continue;

                int tentativeGScore = gScore[current] + GetMovementCost(current, neighbor);
                if (!gScore.TryGetValue(neighbor, out int knownGScore) || tentativeGScore < knownGScore)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        if (closestReachableIndex < 0)
        {
            return false;
        }

        RebuildPath(cameFrom, closestReachableIndex, path);
        return true;
    }

    private void BuildGrid()
    {
        groundCollider = GetComponent<Collider>();
        gridBounds = groundCollider.bounds;
        columns = Mathf.FloorToInt(gridBounds.size.x / cellSize);
        rows = Mathf.FloorToInt(gridBounds.size.z / cellSize);
        cells.Clear();

        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            cells.Add(new GridCell
            {
                WorldPosition = new Vector3(
                    gridBounds.min.x + (column + 0.5f) * cellSize,
                    gridBounds.center.y,
                    gridBounds.min.z + (row + 0.5f) * cellSize),
                IsWalkable = true
            });
        }
    }

    private int GetLowestCostIndex(List<int> openSet, Dictionary<int, int> gScore, int targetIndex)
    {
        int bestIndex = openSet[0];
        int bestCost = GetEstimatedTotalCost(bestIndex, gScore, targetIndex);
        for (int i = 1; i < openSet.Count; i++)
        {
            int candidate = openSet[i];
            int candidateCost = GetEstimatedTotalCost(candidate, gScore, targetIndex);
            if (candidateCost < bestCost)
            {
                bestIndex = candidate;
                bestCost = candidateCost;
            }
        }

        return bestIndex;
    }

    private int GetEstimatedTotalCost(int cellIndex, Dictionary<int, int> gScore, int targetIndex) =>
        gScore[cellIndex] + GetDiagonalDistance(cellIndex, targetIndex);

    private IEnumerable<int> GetWalkableNeighbors(int cellIndex)
    {
        int column = cellIndex % columns;
        int row = cellIndex / columns;
        if (column > 0 && cells[cellIndex - 1].IsWalkable) yield return cellIndex - 1;
        if (column < columns - 1 && cells[cellIndex + 1].IsWalkable) yield return cellIndex + 1;
        if (row > 0 && cells[cellIndex - columns].IsWalkable) yield return cellIndex - columns;
        if (row < rows - 1 && cells[cellIndex + columns].IsWalkable) yield return cellIndex + columns;

        // A diagonal is allowed only when both side cells are clear, preventing corner-cutting.
        if (column > 0 && row > 0 &&
            cells[cellIndex - 1].IsWalkable && cells[cellIndex - columns].IsWalkable &&
            cells[cellIndex - columns - 1].IsWalkable)
        {
            yield return cellIndex - columns - 1;
        }

        if (column < columns - 1 && row > 0 &&
            cells[cellIndex + 1].IsWalkable && cells[cellIndex - columns].IsWalkable &&
            cells[cellIndex - columns + 1].IsWalkable)
        {
            yield return cellIndex - columns + 1;
        }

        if (column > 0 && row < rows - 1 &&
            cells[cellIndex - 1].IsWalkable && cells[cellIndex + columns].IsWalkable &&
            cells[cellIndex + columns - 1].IsWalkable)
        {
            yield return cellIndex + columns - 1;
        }

        if (column < columns - 1 && row < rows - 1 &&
            cells[cellIndex + 1].IsWalkable && cells[cellIndex + columns].IsWalkable &&
            cells[cellIndex + columns + 1].IsWalkable)
        {
            yield return cellIndex + columns + 1;
        }
    }

    private void RebuildPath(Dictionary<int, int> cameFrom, int current, List<GridCell> path)
    {
        path.Add(cells[current]);
        while (cameFrom.TryGetValue(current, out int previous))
        {
            current = previous;
            path.Add(cells[current]);
        }

        path.Reverse();
    }

    private int GetDiagonalDistance(int firstIndex, int secondIndex)
    {
        int firstColumn = firstIndex % columns;
        int firstRow = firstIndex / columns;
        int secondColumn = secondIndex % columns;
        int secondRow = secondIndex / columns;
        int columnDistance = Mathf.Abs(firstColumn - secondColumn);
        int rowDistance = Mathf.Abs(firstRow - secondRow);
        int diagonalSteps = Mathf.Min(columnDistance, rowDistance);
        return diagonalSteps * 14 + (Mathf.Max(columnDistance, rowDistance) - diagonalSteps) * 10;
    }

    private int GetMovementCost(int fromIndex, int toIndex)
    {
        int fromColumn = fromIndex % columns;
        int toColumn = toIndex % columns;
        return fromColumn == toColumn ? 10 : (fromIndex / columns == toIndex / columns ? 10 : 14);
    }

    private int ToIndex(int column, int row) => row * columns + column;

    private void RefreshWalkability()
    {
        foreach (GridCell cell in cells)
        {
            cell.IsWalkable = !HasNonPlayerCollider(cell);
        }
    }

    private bool HasNonPlayerCollider(GridCell cell)
    {
        Vector3 halfExtents = new Vector3(cellSize * 0.49f, obstacleCheckHeight * 0.5f, cellSize * 0.49f);
        Vector3 checkCenter = cell.WorldPosition + Vector3.up * (obstacleCheckHeight * 0.5f + 0.01f);
        Collider[] colliders = Physics.OverlapBox(
            checkCenter,
            halfExtents,
            Quaternion.identity,
            obstacleLayers,
            QueryTriggerInteraction.Collide);

        foreach (Collider collider in colliders)
        {
            if (collider == groundCollider || collider.transform.IsChildOf(transform) ||
                collider.GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static float GetPlanarSquareDistance(Vector3 first, Vector3 second)
    {
        Vector3 difference = first - second;
        return difference.x * difference.x + difference.z * difference.z;
    }

    private void OnDrawGizmos()
    {
        if (!showGrid)
        {
            return;
        }

        if (groundCollider == null)
        {
            groundCollider = GetComponent<Collider>();
        }

        if (groundCollider == null)
        {
            return;
        }

        BuildGrid();
        RefreshWalkability();
        Vector3 cellVisualSize = new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f);

        foreach (GridCell cell in cells)
        {
            Color color = cell.IsWalkable ? Color.green : Color.red;
            Vector3 visualPosition = cell.WorldPosition + Vector3.up * 0.01f;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
            Gizmos.DrawCube(visualPosition, cellVisualSize);
            Gizmos.color = color;
            Gizmos.DrawWireCube(visualPosition, cellVisualSize);
        }
    }
}
