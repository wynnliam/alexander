using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdNavigationHandler : MonoBehaviour
{
    private FlocksHandler flocks;
    private NavigationMesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        flocks = GetComponent<FlocksHandler>();
        mesh = GetComponent<NavigationMesh>();
    }

    // Returns a flow field of size [mesh.numRows, mesh.numColumns]
    // where it would be the flock's flow field, except that the regions
    // that have agents that aren't in my flock will account for crowds.
    /*
     * - Basically compute a density map of all agents not in my flock
     * - Then do Eikonal fill, starting with the goal, where the cost
     * - is determined by number of agents. We only do the tiles that
     * - have agents on them.
     */
    public Vector3[,] GetCrowdFlowfield(int flockId, Vector3 goal)
    {
        if (mesh == null || flocks == null || mesh.NumRows < 1 || mesh.NumCols < 1 ||
           mesh.NumRegions() < 1 || flocks.NumFlocks() <= 1)
            return null;

        // The number of agents at each tile.
        int[,] densityField = new int[mesh.NumRows, mesh.NumCols];
        // If regionHasCrowds[x] == true, then region x has at least
        // one agent in a flock that is not ours. Thus, we need to do
        // eikonal search on the tiles in that region.
        bool[] regionHasCrowds = new bool[mesh.NumRegions()];
        List<Flock> crowds = flocks.GetAllExcept(flockId);
        Flock currFlock = flocks.GetFlock(flockId);

        if (crowds.Count < 1 || currFlock == null)
            return null;

        // Step 1: Compute the density field and regionHasCrowds.
        foreach(Flock crowd in crowds)
        {
            List<Vector3> agentPositions = crowd.GetAllPositions();
            foreach(Vector3 pos in agentPositions)
            {
                int regionIndex = mesh.NavigationRegionIdFromGridSpacePosition(pos);
                int row = -1, col = -1;
                // If the regionIndex != -1, then the agent is standing at a valid region.
                // So we can safely compute the row and col of the agent and update the density
                // value accordingly.
                if(regionIndex != -1)
                {
                    regionHasCrowds[regionIndex] = true;
                    mesh.GetRowAndColFromGridSpacePosition(ref row, ref col, pos);
                    densityField[row, col] += 1;
                }
            }
        }

        // Step 2: Using the densityField and regionHasCrowds,
        // compute a flow field from the goal to every index.
        Vector3[,] result = new Vector3[mesh.NumRows, mesh.NumCols];
        // Stores all of the unvisited (result[row, col] == [-1, -1, -1])
        // tiles. Tiles are organized according to the density of the parent
        // (the tile we want to travel to). This way, it will tell us how much
        // it costs to go from where I am at to where I want to go. We only
        // put something in this search queue if it is either the goal or
        // part of a region with crowds in it. Otherwise the result[row, col]
        // will be as defined by the regular region flow field. 
        SortedList<int, Vector2Int> searchQueue = new SortedList<int, Vector2Int>(new DuplicateKeyComparer<int>());
        List<Vector2Int> visited = new List<Vector2Int>();
        Vector2Int[,] parent = new Vector2Int[mesh.NumRows, mesh.NumCols];
        int currRow = -1, currCol = -1;
        int goalRow = -1, goalCol = -1;

        mesh.GetRowAndColFromGridSpacePosition(ref goalRow, ref goalCol, goal);
        if (goalRow == -1 || goalCol == -1)
            return null;

        // Begin by initializing all vectors to (-1, -1, -1) to denote
        // that vector has not been searched.
        for(int i = 0; i < mesh.NumRows; i++)
        {
            for (int j = 0; j < mesh.NumCols; j++)
            {
                // Regions are not defined for walls.
                if (mesh.IsWall(i, j))
                {
                    result[i, j] = Vector3.zero;
                    continue;
                }

                result[i, j] = new Vector3(-1, -1, -1);

                /*int region = mesh.NavigationRegionIdFromRowColPair(i, j);

                // Only consider the tiles whose region has crowds. Otherwise,
                // it will be according to the currFlock's flowfield.
                if (regionHasCrowds[region])
                    result[i, j] = new Vector3(-1, -1, -1);
                else
                    result[i, j] = mesh.GetRegionCenterInGridSpace(currFlock.Flowfield[region]);*/
            }
        }

        // Adds the goal
        searchQueue.Add(0, new Vector2Int(goalRow, goalCol));
        parent[goalRow, goalCol] = new Vector2Int(goalCol, goalRow);

        while(searchQueue.Count > 0)
        {
            currRow = searchQueue.Values[0].x;
            currCol = searchQueue.Values[0].y;
            visited.Add(searchQueue.Values[0]);
            searchQueue.RemoveAt(0);

            if (mesh.IsWall(currRow, currCol))
                continue;

            int cost = densityField[currRow, currCol];
            int region = mesh.NavigationRegionIdFromRowColPair(currRow, currCol);
            int parentRow = parent[currRow, currCol].y;
            int parentCol = parent[currRow, currCol].x;

            if (regionHasCrowds[region])
                result[currRow, currCol] = mesh.TransformRowColToGridSpace(parentRow, parentCol);
            else
                result[currRow, currCol] = mesh.GetRegionCenterInGridSpace(currFlock.Flowfield[region]);

            // Neighbor above
            int mag;
            if(currRow - 1 >= 0 && result[currRow - 1, currCol] == new Vector3(-1, -1, -1) &&
                !searchQueue.ContainsValue(new Vector2Int(currRow - 1, currCol)) &&
                !visited.Contains(new Vector2Int(currRow - 1, currCol)))
            {
                parent[currRow - 1, currCol] = new Vector2Int(currCol, currRow);
                //mag = Mathf.Abs(currRow - 1 - goalRow) + Mathf.Abs(currCol - goalCol);
                mag = 0;
                searchQueue.Add(cost + mag, new Vector2Int(currRow - 1, currCol));
            }

            // Neighbor below
            if (currRow + 1 < mesh.NumRows && result[currRow + 1, currCol] == new Vector3(-1, -1, -1) &&
                !searchQueue.ContainsValue(new Vector2Int(currRow + 1, currCol)) &&
                !visited.Contains(new Vector2Int(currRow + 1, currCol)))
            {
                parent[currRow + 1, currCol] = new Vector2Int(currCol, currRow);
                //mag = Mathf.Abs(currRow + 1 - goalRow) + Mathf.Abs(currCol - goalCol);
                mag = 0;
                searchQueue.Add(cost + mag, new Vector2Int(currRow + 1, currCol));
            }

            // Neighbor to the left
            if (currCol - 1 >= 0 && result[currRow, currCol - 1] == new Vector3(-1, -1, -1) &&
                !searchQueue.ContainsValue(new Vector2Int(currRow, currCol - 1)) &&
                !visited.Contains(new Vector2Int(currRow, currCol - 1)))
            {
                parent[currRow, currCol - 1] = new Vector2Int(currCol, currRow);
                //mag = Mathf.Abs(currRow - goalRow) + Mathf.Abs(currCol - 1 - goalCol);
                mag = 0;
                searchQueue.Add(cost + mag, new Vector2Int(currRow, currCol - 1));
            }

            // Neighbor to the right
            if (currCol + 1 < mesh.NumCols && result[currRow, currCol + 1] == new Vector3(-1, -1, -1) &&
                !searchQueue.ContainsValue(new Vector2Int(currRow, currCol + 1)) &&
                !visited.Contains(new Vector2Int(currRow, currCol + 1)))
            {
                parent[currRow, currCol + 1] = new Vector2Int(currCol, currRow);
                //mag = Mathf.Abs(currRow - goalRow) + Mathf.Abs(currCol + 1 - goalCol);
                mag = 0;
                searchQueue.Add(cost, new Vector2Int(currRow, currCol + 1));
            }
        }

        return result;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
