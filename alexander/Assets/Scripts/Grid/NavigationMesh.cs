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

    // Basically, every tile on the grid map is relative to this position.
    // So if we want to transform an index position into a tile position,
    // we add this value. If we want to get an index position from a tile
    // position, we subtract this value.
    private Vector3Int gridMapOrigin;

    // Matrix representation of our level. Positions marked 1 are walls,
    // and positions marked 0 are floors.
    private int[,] wallIndexPositionMap;
    // Given an index position (a row and column), we return the region in
    // that position. This makes finding the region of some location a constant
    // time operation.
    private int[,] regionIndexPositionMap;

    // Start is called before the first frame update
    void Start()
    {
        Transform wallTransform = transform.Find("CollisionArea");
        Tilemap wallTiles = wallTransform.GetComponent<Tilemap>();

        // y is the number of rows, and x is the number of columns
        numRows = wallTiles.size.y;
        numColumns = wallTiles.size.x;
        gridMapOrigin = wallTiles.origin;

        wallIndexPositionMap = new int[numRows, numColumns];
        regionIndexPositionMap = new int[numRows, numColumns];
        for(int i = 0; i < numRows; i++)
        {
            for(int j = 0; j < numColumns; j++)
            {
                // -1 denotes no region in this position
                regionIndexPositionMap[i, j] = -1;

                if (wallTiles.HasTile(new Vector3Int(j, i, 0) + gridMapOrigin))
                    wallIndexPositionMap[i, j] = 1;
                else
                    wallIndexPositionMap[i, j] = 0;
            }
        }

        NavigationMeshBuilder builder = new NavigationMeshBuilder();
        regions = builder.ConstructNavigationMesh(wallIndexPositionMap, numRows, numColumns);

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

        foreach(NavigationRegion region in regions)
        {
            for(int row = region.Row; row < region.Row + region.Height; row++)
            {
                for(int col = region.Column; col < region.Column + region.Width; col++)
                {
                    regionIndexPositionMap[row, col] = region.Id;
                }
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

        if(0 <= id && id < regions.Count)
        {
            // This is safe because the id corresponds to the index
            // in the region list. When we construct the NavigationRegions,
            // we set the id's to be this way.
            NavigationRegion r = regions[id];

            result.x = (r.Column + (r.Column + r.Width)) / 2.0f;
            result.y = (r.Row + (r.Row + r.Height)) / 2.0f;

            result.x += gridMapOrigin.x;
            result.y += gridMapOrigin.y;
        }

        return result;
    }

    public int NavigationRegionIdFromTilePosition(Vector3 pos)
    {
        int row = (int)(pos.y - gridMapOrigin.y);
        int col = (int)(pos.x - gridMapOrigin.x);

        int result = -1;

        if (0 <= row && row < numRows && 0 <= col && col <= numColumns)
            result = regionIndexPositionMap[row, col];

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
