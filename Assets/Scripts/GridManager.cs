using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public int gridSize = 32;
    private bool[,] grid;

    private void Awake()
    {
        InitializeGrid();
    }

    void Start()
    {
        // Initialize or reset the grid
        ResetGrid();
    }

    public void InitializeGrid()
    {
        grid = new bool[gridSize, gridSize];
    }

    public void ResetGrid()
    {
        // Clear the grid
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                grid[i, j] = false;
            }
        }

    }


    public (float x, float y, int rotation) PlaceItem(int width, int height, int rotation)
    {
        int attempts = 0;
        bool placed = false;

        while (!placed && attempts < 100)
        {
            int x = Random.Range(0, gridSize - width);
            int y = Random.Range(0, gridSize - height);

            if (!IsOverlap(x, y, width, height, rotation))
            {
                placed = true;
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        grid[x + i, y + j] = true;
                    }
                }
                float worldX = (x - gridSize / 2 + width / 2) * 0.5f;
                float worldZ = (y - gridSize / 2 + height / 2) * 0.5f;

                return (worldX, worldZ, rotation);
            }
            attempts++;
        }
        return (-1, -1, -1);
    }

    bool IsOverlap(int x, int y, int width, int height, int rotation)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (rotation == 90 || rotation == 270)
                {
                    if (x + j >= gridSize || y + i >= gridSize || grid[x + j, y + i])
                    {
                        return true;
                    }
                }
                else
                {
                    if (x + i >= gridSize || y + j >= gridSize || grid[x + i, y + j])
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }



}
