using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NavigationMesh : MonoBehaviour
{
    private int numRows, numColumns;

    // A list of all the navigable regions in the level.
    private List<NavigationRegion> regions;
    // Given if (x, y) == true, then regions x and y are adjacent.
    // Otherwise they are not adjacent.
    private bool[,] regionAdjacencyMatrix;

    private Tilemap wallTiles;

    // Matrix representation of our level. Positions marked 1 are walls,
    // and positions marked 0 are floors.
    private int[,] map;

    // Start is called before the first frame update
    void Start()
    {
        Transform wallTransform = transform.Find("CollisionArea");
        Transform floorTransform = transform.Find("WalkableArea");

        wallTiles = wallTransform.GetComponent<Tilemap>();

        // y is the number of rows, and x is the number of columns
        numRows = wallTiles.size.y;
        numColumns = wallTiles.size.x;

        map = new int[numRows, numColumns];
        for(int i = 0; i < numRows; i++)
        {
            for(int j = 0; j < numColumns; j++)
            {
                if (i == numRows - 1 && j == numColumns - 1)
                    Debug.Log("here!");

                if (wallTiles.HasTile(new Vector3Int(j, i, 0) + wallTiles.origin))
                    map[i, j] = 1;
                else
                    map[i, j] = 0;
            }
        }

        NavigationMeshBuilder builder = new NavigationMeshBuilder();
        regions = builder.ConstructNavigationMesh(map, numRows, numColumns);

        regionAdjacencyMatrix = new bool[regions.Count, regions.Count];
        for(int i = 0; i < regions.Count; i++)
        {
            for (int j = 0; j < regions.Count; j++)
            {
                if (AreRegionsAdjacent(regions[i], regions[j]))
                    regionAdjacencyMatrix[i, j] = true;
                else
                    regionAdjacencyMatrix[i, j] = false;
            }
        }
    }

    // Creates a graph (represented by an int array).
    // If graph[x] == y, this means that all the tiles in region
    // x have a steering force that goes to the center of region y.
    // We generate this graph by starting with the end goal's region,
    // then doing a form of BFS until all regions have a value.
    public int[] GetFlowfieldGraph(Vector3 goal)
    {
        int currIndex = NavigationRegionIdFromTilePosition(goal);
        int[] graph = new int[regions.Count];
        List<int> toVisit = new List<int>();

        for (int i = 0; i < graph.Length; i++)
            graph[i] = -1;

        if (currIndex == -1)
            return graph;

        graph[currIndex] = currIndex;
        toVisit.Add(currIndex);

        while(toVisit.Count > 0)
        {
            currIndex = toVisit[0];
            toVisit.RemoveAt(0);

            for(int i = 0; i < regions.Count; i++)
            {
                if(regionAdjacencyMatrix[currIndex, i] == true && graph[i] == -1)
                {
                    graph[i] = currIndex;
                    toVisit.Add(i);
                }
            }
        }

        return graph;
    }

    public Vector2 GetRegionCenter(int id)
    {
        Vector2 result = new Vector2();

        foreach(NavigationRegion r in regions)
        {
            if(r.Id == id)
            {
                result.x = (r.Column + (r.Column + r.Width)) / 2.0f;
                result.y = (r.Row + (r.Row + r.Height)) / 2.0f;

                result.x += wallTiles.origin.x;
                result.y += wallTiles.origin.y;
            }
        }

        return result;
    }

    public int NavigationRegionIdFromTilePosition(Vector3 pos)
    {
        int row = (int)(pos.y - wallTiles.origin.y);
        int col = (int)(pos.x - wallTiles.origin.x);

        int result = -1;

        foreach(NavigationRegion region in regions)
        {
            if(region.Contains(row, col))
            {
                result = region.Id;
                break;
            }
        }

        return result;
    }

    public bool AreRegionsAdjacent(NavigationRegion a, NavigationRegion b)
    {
        return a == b ||
               a.Above(b) ||
               a.Below(b) ||
               a.LeftOf(b) ||
               a.RightOf(b);
    }
}
