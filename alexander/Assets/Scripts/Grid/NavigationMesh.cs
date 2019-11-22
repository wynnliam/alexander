using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NavigationMesh : MonoBehaviour
{
    /*
     * RULE: We define two coordinate spaces that this class facilitates the
     * translation of.
     * 
     * 1. Grid Space: This is the coordinate space of the 2D world. For example,
     * that actor is located at (24.7, 32.5). A word to the wise: this space can
     * be represented with both Vector2s and Vector3s. Why does this happen? Despite
     * being a 2D game, the transforms of actors are Vector3s.
     * 
     * 2. Index Space: This is the space of specifying a row and column. Our world
     * is a tile grid. So if I give a location (3, 4) in index space this says
     * "the tile at row 3, column 4". Note that the axis are flipped in this case.
     * a Y position (a row) is specified before an X position (a column). Why did I
     * do this? I've always found it more intuitive to specify a row and then a column.
     * Sorry, but if you have to maintain this you're gonna get used to my quirks.
     * 
     * Why not just have one space? Well Grids, the things tiles exist in in the Unity
     * Engine, don't have to have an origin at (0, 0), but index space does. One scene
     * that I've worked with had tiles that were located in negative regions. Since I didn't
     * want to have to think about this translation of spaces when designing levels, I 
     * simply automate it here. I also found it easier to reason about regions in index space.
     */

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

        InitializeIndexPositionMaps(wallTiles);

        NavigationMeshBuilder builder = new NavigationMeshBuilder();
        regions = builder.ConstructNavigationMesh(wallIndexPositionMap, numRows, numColumns);

        InitializeRegionAdjacencyMatrix();

        FillRegionIndexPositionMap();
    }

    private void InitializeIndexPositionMaps(Tilemap wallTiles)
    {
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

    }

    private void InitializeRegionAdjacencyMatrix()
    {
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

    private bool AreRegionsAdjacent(NavigationRegion a, NavigationRegion b)
    {
        return a == b ||
               a.Above(b) ||
               a.Below(b) ||
               a.LeftOf(b) ||
               a.RightOf(b);
    }

    private void FillRegionIndexPositionMap()
    {
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
    // Note that the goal is in grid space.
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
}
