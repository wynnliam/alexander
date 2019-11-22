using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationRegion
{
    private RectInt bound;

    public NavigationRegion(int id)
    {
        Id = id;

        bound = new RectInt(0, 0, 1, 1);
    }

    public NavigationRegion(int id, int row, int col, int w, int h)
    {
        Id = id;

        bound = new RectInt(col, row, w, h);
    }

    public int Id { get; set; }

    public int Row
    {
        get { return bound.y; }
        set { bound.y = value; }
    }

    public int Column
    {
        get { return bound.x; }
        set { bound.x = value; }
    }

    public int Width
    {
        get { return bound.width; }
        set { bound.width = value; }
    }

    public int Height
    {
        get { return bound.height; }
        set { bound.height = value; }
    }

    public override string ToString()
    {
        return "{ id: " + Id + " r: " + bound.y + " c: " + bound.x + " w: " + bound.width + " h: " + bound.height + " }";
    }

    internal bool Contains(int row, int column)
    {
        return this.Row <= row && row < this.Row + this.Height &&
               this.Column <= column && column < this.Column + this.Width;
    }

    internal bool ContainsWall(int[,] gridMap, int numRow, int numColumn)
    {
        for(int i = Row; i < Row + Height; i++)
        {
            for(int j = Column; j < Column + Width; j++)
            {
                if (i < numRow && j < numColumn && gridMap[i, j] == 1)
                    return true;
            }
        }

        return false;
    }

    // Return true if this navigation region is above compareTo.
    // We do so by scanning the bottom row. If any tile position
    // along the bottom row touches one in compareTo, return true.
    public bool Above(NavigationRegion compareTo)
    {
        bool result = false;

        for(int j = Column; j < Column + Width; j++)
        {
            if(compareTo.Contains(Row - 1, j))
            {
                result = true;
                break;
            }
        }

        return result;
    }

    // Return true if this navigation region is below compareTo.
    // If this region is below compareTo, then compareTo is above
    // this region. Thus we simply return if compareTo is above this
    // region
    public bool Below(NavigationRegion compareTo)
    {
        return compareTo.Above(this);
    }

    // Return true if this navigation region is right of compareTo.
    // We do so by scanning the leftmost column. If any tile position
    // along the left most column touches one in compareTo, return true.
    public bool RightOf(NavigationRegion compareTo)
    {
        bool result = false;

        for(int i = Row; i < Row + Height; i++)
        {
            if(compareTo.Contains(i, Column - 1))
            {
                result = true;
                break;
            }
        }

        return result;
    }

    // Return true if this region is left of compareTo.
    // If this is left of compareTo, then compareTo is right
    // of this, hence we call compareTo's RightOf function.
    public bool LeftOf(NavigationRegion compareTo)
    {
        return compareTo.RightOf(this);
    }
}
