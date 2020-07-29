using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager ins;
    [SerializeField] Camera cam;
    [SerializeField] GameObject camAnchor;
    public Material[] valid;
    [SerializeField] bool editorOn;
    [SerializeField] bool loadFromFile;
    public Transform parentNode;
    public static float gridSize { private set; get; } = 0.5f;
    public static TileNode[,] grids;
    public static bool PlacingFurniture;

    [Header("Changing Floor")] [SerializeField] GameObject[] floorTiles;
    [SerializeField] GameObject[] floorTilePrefabs;
    [SerializeField] Material[] floorTileMaterials;
    int floorTypeIdx = 0;

    private void OnValidate()
    {
        cam = GetComponent<Camera>();
    }
    private void Awake()
    {
        if (ins == null) ins = this; else Destroy(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        if (loadFromFile) LoadGridsFromFile();
        else
        {
            grids = new TileNode[101, 101];
            for (int i = -50; i <= 50; ++i)
            {
                for (int j = -50; j <= 50; ++j)
                {
                    grids[i + 50, j + 50] = new TileNode(new Vector3Int(i, j, 0));
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (editorOn)
        {
            if (Input.GetMouseButtonDown(0))
            {
                TileNode t = MouseAtTile();
                t.wallDir = Dir.Left;
                print("Change tile " + t.coord + " to " + t.wallDir + ".");
            }
            if (Input.GetMouseButtonDown(2))
            {
                TileNode t = MouseAtTile();
                t.tileType = TileNodeType.Wall;
                print("Change tile " + t.coord + " to " + t.tileType + " type");
            }
            if (Input.GetMouseButtonDown(1))
            {
                TileNode t = MouseAtTile();
                t.tileType = TileNodeType.Clear;
                print("Change tile " + t.coord + " to " + t.tileType + " type");
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveGridsToFile();
            }
        }
    }
    private void FixedUpdate()
    {
        // 控制
        // 旋转相机
        var horizontal = Input.GetAxis("Horizontal");
        if (horizontal > float.Epsilon) camAnchor.transform.Rotate(new Vector3(0, 1, 0));
        else if (horizontal < -float.Epsilon) camAnchor.transform.Rotate(new Vector3(0, -1, 0));
    }

    private void OnDrawGizmos()
    {
        if (grids == null)
        {
            for (int i = -50; i <= 50; ++i)
            {
                for (int j = -50; j <= 50; ++j)
                {
                     Gizmos.DrawWireCube(new Vector3(i * gridSize, 0, j * gridSize), new Vector3(gridSize, 0.01f, gridSize));
                }
            }
            return;
        }
        for (int i = -50; i <= 50; ++i)
        {
            for (int j = -50; j <= 50; ++j)
            {
                TileNode t = GetTile(i, j);
                if (t.tileType == TileNodeType.Clear)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(new Vector3(i * gridSize, 0, j * gridSize), new Vector3(gridSize, 0.01f, gridSize));
                }
                else if (t.tileType == TileNodeType.Wall)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(new Vector3(i * gridSize, 0, j * gridSize), new Vector3(gridSize, 0.01f, gridSize));
                }
                else if (t.tileType == TileNodeType.WallSeparator)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(new Vector3(i * gridSize, 0, j * gridSize), new Vector3(gridSize, 0.01f, gridSize));
                }
                else if (t.tileType == TileNodeType.Invalid)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(new Vector3(i * gridSize, 0, j * gridSize), new Vector3(gridSize, 0.01f, gridSize));
                }
            }
        }
    }

    public static Vector2 WorldPointToCanvasPoint(Vector3 worldPoint)
    {
        var CanvasRect = GameObject.Find("Canvas").GetComponent<RectTransform>();
        Vector2 ViewportPosition = ins.cam.WorldToViewportPoint(worldPoint);
        return new Vector2(ViewportPosition.x * CanvasRect.sizeDelta.x - CanvasRect.sizeDelta.x * 0.5f,
          ViewportPosition.y * CanvasRect.sizeDelta.y - CanvasRect.sizeDelta.y * 0.5f);
    }

    // ********************************Function Relates to Tiles********************************
    public static Vector3Int WorldPositionToCoord(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(-worldPos.x / gridSize);
        int y = Mathf.RoundToInt(-worldPos.z / gridSize);
        return new Vector3Int(-x, -y, 0);
    }
    public static Vector3 CoordToWorldPosition(Vector3Int coord)
    {
        return new Vector3(coord.x * gridSize, 0, coord.y * gridSize);
    }
    public static Vector3Int GetMouseCoord()
    {
        Plane p = new Plane(Vector3.up, Vector3.zero);
        float ent;
        var ray = ins.cam.ScreenPointToRay(Input.mousePosition);
        if (p.Raycast(ray, out ent)) return WorldPositionToCoord(ray.GetPoint(ent));
        return Vector3Int.zero;
    }
    public static TileNode GetTile(int x, int y)
    {
        if (x < -50 || x > 50) return null;
        if (y < -50 || y > 50) return null;
        return grids[x + 50, y + 50];
    }
    public static TileNode GetTile(Vector3Int c)
    {
        if (c.x < -50 || c.x > 50) return null;
        if (c.y < -50 || c.y > 50) return null;
        return grids[c.x + 50, c.y + 50];
    }
    public static TileNode MouseAtTile()
    {
        Vector3Int mc = GetMouseCoord();
        return GetTile(mc.x, mc.y);
    }
    void SaveGridsToFile()
    {
        GridFile gf = new GridFile();
        gf.cellCount = 101 * 101;
        gf.coord_x = new int[gf.cellCount];
        gf.coord_y = new int[gf.cellCount];
        gf.coord_z = new int[gf.cellCount];
        gf.typeType = new int[gf.cellCount];
        gf.wallDir = new int[gf.cellCount];
        for (int i = -50; i <= 50; ++i)
        {
            for (int j = -50; j <= 50; ++j)
            {
                TileNode t = GetTile(i, j);
                int idx = (i + 50) * 101 + j + 50;
                gf.coord_x[idx] = t.coord.x;
                gf.coord_y[idx] = t.coord.y;
                gf.coord_z[idx] = t.coord.z;
                gf.typeType[idx] = (int)t.tileType;
                gf.wallDir[idx] = (int)t.wallDir;
            }
        }
        print("Saving");
        GridFile.Save(gf);
    }
    void LoadGridsFromFile()
    {
        GridFile gf = GridFile.Load();
        grids = new TileNode[101, 101];
        for (int i = 0; i < gf.cellCount; ++i)
        {
            int x = gf.coord_x[i] + 50, y = gf.coord_y[i] + 50;
            grids[x, y] = new TileNode(new Vector3Int(gf.coord_x[i], gf.coord_y[i], gf.coord_z[i]));
            grids[x, y].tileType = (TileNodeType)gf.typeType[i];
            grids[x, y].wallDir = (Dir)gf.wallDir[i];
        }
    }

    // ********************************Change Floor********************************
    public static void ChangeFloorPattern(int idx)
    {
        int typeidx = idx / 4;
        int matidx = idx % 4;
        for (int i = 0; i < ins.floorTiles.Length; ++i)
        {
            if (ins.floorTypeIdx != typeidx)
            {
                GameObject newTile = Instantiate(ins.floorTilePrefabs[typeidx]);
                newTile.transform.position = ins.floorTiles[i].transform.position;
                newTile.transform.rotation = ins.floorTiles[i].transform.rotation;
                Destroy(ins.floorTiles[i]);
                ins.floorTiles[i] = newTile;
            }
            ins.floorTiles[i].GetComponent<MeshRenderer>().materials = new Material[] { ins.floorTileMaterials[matidx] };
        }
        ins.floorTypeIdx = typeidx;
    }
}
