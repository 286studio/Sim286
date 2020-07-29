using cakeslice;
using System.Collections.Generic;
using UnityEngine;

public enum FurnitureState
{
    Idle,
    Placing
}
public enum FurnitureType
{
    Furniture,
    Wall,
    Desk,
    WallDecoration,
    DeskDecoration
}
public enum Dir
{
    Up, Down, Left, Right
}
[System.Serializable]
class EdgesOverlapWithWallSeparator
{
    public bool Up, Down, Left, Right;
}
public class Furniture : MonoBehaviour
{
    public static bool AllowAction;
    public static Vector3[] DirEulers = new Vector3[]
    {
        new Vector3(0,180,0),
        Vector3.zero,
        new Vector3(0,90,0),
        new Vector3(0,-90,0)
    };
    public string furnitureName;
    [SerializeField] protected Vector2Int foundation;
    [SerializeField] protected float height;
    public FurnitureType furnitureType;
    [SerializeField] protected GameObject transMesh;
    [SerializeField] protected GameObject opaMesh;
    [SerializeField] protected Vector3 pivotOffset;
    [SerializeField] EdgesOverlapWithWallSeparator edgesOverlapWithWallSeparator;
    [SerializeField] GameObject arrow;
    [HideInInspector] public FurnitureState state;
    [HideInInspector] public PlacingFurniture pfButton;
    public Vector3Int coord;
    public Transform[] decorationTransforms;
    public List<DesktopDecoration> decorations = new List<DesktopDecoration>();
    Vector3Int offset;
    Vector3Int rotateAroundCoord;
    protected GameObject[,] cells;
    protected bool canPlaceHere;
    Outline[] outlines;
    Vector3[] pivotOffsets = new Vector3[4];
    public bool OutlineEnabled
    {
        set
        {
            foreach (var o in outlines) o.enabled = value;
            Vector3 a, b;
            a = b = UIPosition;
            a.y = 0;
            lr.SetPositions(new Vector3[] { a, b });
            lr.enabled = value;
            foreach (var c in FindObjectsOfType<BoxCollider>())
            {
                c.enabled = !value;
            }
        }
    }
    static Furniture selected = null;
    RepickFurniture repickUI;

    public delegate void OnPlacingEnd();
    public event OnPlacingEnd onPlacingCancel;
    public event OnPlacingEnd onPlacingSuccess;
    bool isRotating;
    float holdMouseDownTime = 0;
    Dir currentDir;
    LineRenderer lr;
    BoxCollider bCollider;
    protected virtual void Start()
    {
        // 生成Collider供鼠标点击
        bCollider = opaMesh.GetComponent<BoxCollider>();
        if (!bCollider)
        {
            bCollider = opaMesh.AddComponent<BoxCollider>();
            bCollider.center = (new Vector3(0, height / 2, 0) - pivotOffset) * FurnitureManager.gridSize;
            bCollider.size = new Vector3(foundation.x, height, foundation.y) * FurnitureManager.gridSize;
        }
        state = FurnitureState.Placing;
        currentDir = Dir.Down;
        offset = new Vector3Int(foundation.x / 2 - (IsEven(foundation.x) ? 1 : 0), foundation.y / 2 - (IsEven(foundation.y) ? 1 : 0), 0);
        pivotOffsets[0] = new Vector3(-pivotOffset.x, pivotOffset.y, -pivotOffset.z);
        pivotOffsets[1] = pivotOffset;
        pivotOffsets[2] = new Vector3(pivotOffset.z, pivotOffset.y, -pivotOffset.x);
        pivotOffsets[3] = new Vector3(-pivotOffset.z, pivotOffset.y, pivotOffset.x);
        if (arrow)
        {
            arrow.GetComponent<MeshRenderer>().sortingOrder = 999;
            arrow.SetActive(false);
        }
        lr = GetComponent<LineRenderer>();
        lr.enabled = false;
        if (state == FurnitureState.Placing)
        {
            cells = new GameObject[foundation.x, foundation.y];
            for (int i = 0; i < foundation.x; ++i)
            {
                for (int j = 0; j < foundation.y; ++j)
                {
                    var cell = Instantiate(Resources.Load<GameObject>("PlacingFloor"), transform);
                    cell.transform.localPosition = new Vector3(i, 0, j) * FurnitureManager.gridSize;
                    cell.GetComponent<MeshRenderer>().sortingOrder = 998;
                    cells[i, j] = cell;
                }
            }
            opaMesh.transform.localPosition = transMesh.transform.localPosition = (new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) + pivotOffset) * FurnitureManager.gridSize;
            transMesh.SetActive(true);
            opaMesh.SetActive(false);
            outlines = opaMesh.GetComponentsInChildren<Outline>();
            OutlineEnabled = false;
        }
        else if (state == FurnitureState.Idle)
        {
            Destroy(transMesh);
            opaMesh.transform.localPosition = new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) * FurnitureManager.gridSize;
            opaMesh.SetActive(true);
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        UpdatePlacing();
        UpdateIdle();
    }
    protected virtual void UpdateIdle()
    {
        if (state == FurnitureState.Idle)
        {
            if (Input.GetMouseButtonDown(1)) CancelSelect();
        }
    }
    protected virtual void UpdatePlacing()
    {
        if (state == FurnitureState.Placing)
        {
            UpdatePlacing_CheckSubCells();
            UpdatePlacing_Rotate();
            UpdatePlacing_Placement();
            if (Input.GetMouseButtonDown(1)) CancelPlacement();
        }
    }
    protected virtual void UpdatePlacing_CheckSubCells()
    {
        if (!isRotating)
        {
            coord = FurnitureManager.GetMouseCoord() - offset;
            TileNode t = FurnitureManager.GetTile(coord);
            if (t != null) transform.position = FurnitureManager.CoordToWorldPosition(coord);
            canPlaceHere = true;
            for (int i = 0; i < foundation.x; ++i)
            {
                for (int j = 0; j < foundation.y; ++j)
                {
                    TileNode subcell = FurnitureManager.GetTile(coord + new Vector3Int(i, j, 0));
                    bool outlineCell = CheckOutlineCells(currentDir, i, j);
                    bool canPlaceOnThisCell = subcell != null && subcell.CanStandOn(this, outlineCell);
                    canPlaceHere &= canPlaceOnThisCell;
                    cells[i, j].GetComponent<MeshRenderer>().materials = new Material[] { FurnitureManager.ins.valid[canPlaceOnThisCell ? (outlineCell ? 2 : 0) : 1] };
                }
            }
        }
    }
    protected virtual void UpdatePlacing_Rotate()
    {
        if (isRotating) // 正在旋转
        {
            var dir = FurnitureManager.GetMouseCoord() - rotateAroundCoord;
            bool a = dir.y >= dir.x;
            bool b = dir.y >= -dir.x;
            if (a && b) RotateMesh(Dir.Down);
            else if (!a && b) RotateMesh(Dir.Left);
            else if (a && !b) RotateMesh(Dir.Right);
            else RotateMesh(Dir.Up);
        }

        if (Input.GetMouseButton(0) && !isRotating)
        {
            // 按住鼠标不放开始计时
            holdMouseDownTime += Time.deltaTime;
            if (holdMouseDownTime >= 0.3f) // 按住鼠标不放多于0.1秒算作长按
            {
                // 旋转家具操作
                isRotating = true;
                rotateAroundCoord = FurnitureManager.GetMouseCoord();
                arrow.SetActive(isRotating);
                holdMouseDownTime = 0;
            }
        }
        else if (Input.GetMouseButtonUp(0) && isRotating)
        {
            // 旋转完毕，立即放置家具
            isRotating = false;
            arrow.SetActive(isRotating);
        }
    }
    protected virtual void UpdatePlacing_Placement()
    {
        if (Input.GetMouseButtonUp(0) && !isRotating && canPlaceHere)
        {
            // 放置家具
            for (int i = 0; i < foundation.x; ++i)
            {
                for (int j = 0; j < foundation.y; ++j)
                {
                    TileNode subcell = FurnitureManager.GetTile(coord + new Vector3Int(i, j, 0));
                    if (subcell != null) subcell.Occupies(this);
                    Destroy(cells[i, j]);
                }
            }
            cells = null;
            Place();
            state = FurnitureState.Idle;
        }
    }
    private void OnDrawGizmos()
    {
        for(int i = 0; i < foundation.x; ++i)
        {
            for (int j = 0; j < foundation.y; ++j)
            {
                Gizmos.color = CheckOutlineCells(Dir.Down, i, j) ? Color.yellow : Color.green;
                Gizmos.DrawCube(new Vector3(i * FurnitureManager.gridSize, 0, j * FurnitureManager.gridSize), new Vector3(FurnitureManager.gridSize, 0.01f, FurnitureManager.gridSize) * .9f);
            }
        }
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(new Vector3(foundation.x / 2f - 0.5f, height / 2, foundation.y / 2f - 0.5f) * FurnitureManager.gridSize, new Vector3(foundation.x, height, foundation.y) * FurnitureManager.gridSize);
    }

    bool CheckOutlineCells(Dir dir, int i, int j)
    {
        bool up = false, down = false, left = false, right = false;
        switch (dir)
        {
            case Dir.Down:
                up = edgesOverlapWithWallSeparator.Up && j == foundation.y - 1;
                down = edgesOverlapWithWallSeparator.Down && j == 0;
                left = edgesOverlapWithWallSeparator.Left && i == foundation.x - 1;
                right = edgesOverlapWithWallSeparator.Right && i == 0;
                break;
            case Dir.Up:
                up = edgesOverlapWithWallSeparator.Down && j == foundation.y - 1;
                down = edgesOverlapWithWallSeparator.Up && j == 0;
                left = edgesOverlapWithWallSeparator.Left && i == foundation.x - 1;
                right = edgesOverlapWithWallSeparator.Right && i == 0;
                break;
            case Dir.Left:
                up = edgesOverlapWithWallSeparator.Left && j == foundation.y - 1;
                down = edgesOverlapWithWallSeparator.Right && j == 0;
                left = edgesOverlapWithWallSeparator.Up && i == foundation.x - 1;
                right = edgesOverlapWithWallSeparator.Down && i == 0;
                break;
            case Dir.Right:
                up = edgesOverlapWithWallSeparator.Left && j == foundation.y - 1;
                down = edgesOverlapWithWallSeparator.Right && j == 0;
                left = edgesOverlapWithWallSeparator.Down && i == foundation.x - 1;
                right = edgesOverlapWithWallSeparator.Up && i == 0;
                break;
        }
        return up || down || left || right;
    }

    protected void Place()
    {
        Destroy(transMesh);
        opaMesh.SetActive(true);
        var emergeAnim = Instantiate(Resources.Load<GameObject>("emerge"));
        emergeAnim.transform.position = opaMesh.transform.position;
        emergeAnim.GetComponent<ParticleSystemRenderer>().sortingOrder = 999;
        CollisionDetect cd = GetComponentInChildren<CollisionDetect>();
        cd.onMouseEnter += OnMouseEnterChildren;
        cd.onMouseExit += OnMouseExitChildren;
        cd.onMouseDown += OnMouseDownChildren;
        onPlacingSuccess?.Invoke();
    }
    protected void CancelPlacement()
    {
        onPlacingCancel?.Invoke();
        Destroy(gameObject);
    }
    public virtual void RemoveFromWorld()
    {
        for (int i = 0; i < foundation.x; ++i)
        {
            for (int j = 0; j < foundation.y; ++j)
            {
                TileNode subcell = FurnitureManager.GetTile(coord + new Vector3Int(i, j, 0));
                if (subcell != null) subcell.Unoccupies(this);
            }
        }
        Destroy(gameObject);
    }
    public static bool IsEven(int num)
    {
        return num % 2 == 0;
    }

    private void OnMouseEnterChildren()
    {
    }
    private void OnMouseDownChildren()
    {
        CancelSelect();
        selected = this;
        OutlineEnabled = true;
        repickUI = Instantiate(Resources.Load<GameObject>("RePick"), GameObject.Find("Canvas").transform).GetComponent<RepickFurniture>().Init(this);
    }
    private void OnMouseExitChildren()
    {
    }
    protected virtual void OnValidate()
    {
        if (opaMesh)
        {
            opaMesh.transform.localPosition = (new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) + pivotOffset) * FurnitureManager.gridSize;
            if (arrow)
            {
                arrow.transform.localPosition = new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) * FurnitureManager.gridSize;
                arrow.transform.rotation = opaMesh.transform.rotation;
            }
        }
    }

    protected void RotateMesh(Dir pointTo)
    {
        if (pointTo == currentDir) return;
        if (foundation.x == foundation.y || IsOpposite(pointTo, currentDir))
        {
            for (int i = 0; i < foundation.x; ++i)
                for (int j = 0; j < foundation.y; ++j)
                {
                    TileNode tile = FurnitureManager.GetTile(coord + new Vector3Int(i, j, 0));
                    bool outlineCell = CheckOutlineCells(pointTo, i, j);
                    bool canPlaceOnThisCell = tile != null && tile.CanStandOn(this, outlineCell);
                    canPlaceHere &= canPlaceOnThisCell;
                    cells[i, j].GetComponent<MeshRenderer>().materials = new Material[] { FurnitureManager.ins.valid[canPlaceOnThisCell ? (outlineCell ? 2 : 0) : 1] };
                }
            opaMesh.transform.localPosition = transMesh.transform.localPosition = (new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) + pivotOffsets[(int)pointTo]) * FurnitureManager.gridSize;
            arrow.transform.localPosition = new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) * FurnitureManager.gridSize;
            opaMesh.transform.rotation = transMesh.transform.rotation = arrow.transform.rotation = Quaternion.Euler(DirEulers[(int)pointTo]);
        }
        else
        {
            for (int i = 0; i < foundation.x; ++i) for (int j = 0; j < foundation.y; ++j) Destroy(cells[i, j]);
            int temp = foundation.x;
            foundation.x = foundation.y;
            foundation.y = temp;
            offset = new Vector3Int(foundation.x / 2 - (IsEven(foundation.x) ? 1 : 0), foundation.y / 2 - (IsEven(foundation.y) ? 1 : 0), 0);
            coord = rotateAroundCoord - offset;
            cells = new GameObject[foundation.x, foundation.y];
            for (int i = 0; i < foundation.x; ++i) 
                for (int j = 0; j < foundation.y; ++j)
                {
                    cells[i, j] = Instantiate(Resources.Load<GameObject>("PlacingFloor"), transform);
                    cells[i, j].transform.localPosition = new Vector3(i, 0, j) * FurnitureManager.gridSize;
                    TileNode tile = FurnitureManager.GetTile(coord + new Vector3Int(i, j, 0));
                    bool outlineCell = CheckOutlineCells(pointTo, i, j);
                    bool canPlaceOnThisCell = tile != null && tile.CanStandOn(this, outlineCell);
                    canPlaceHere &= canPlaceOnThisCell;
                    cells[i, j].GetComponent<MeshRenderer>().materials = new Material[] { FurnitureManager.ins.valid[canPlaceOnThisCell ? (outlineCell ? 2 : 0) : 1] };
                }
            opaMesh.transform.localPosition = transMesh.transform.localPosition = (new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) + pivotOffsets[(int)pointTo]) * FurnitureManager.gridSize;
            arrow.transform.localPosition = new Vector3(foundation.x / 2f - 0.5f, 0, foundation.y / 2f - 0.5f) * FurnitureManager.gridSize;
            opaMesh.transform.rotation = transMesh.transform.rotation = arrow.transform.rotation = Quaternion.Euler(DirEulers[(int)pointTo]);
            TileNode t = FurnitureManager.GetTile(coord);
            if (t != null) transform.position = FurnitureManager.CoordToWorldPosition(coord);
        }
        currentDir = pointTo;
    }
    public static bool IsOpposite(Dir d1, Dir d2)
    {
        if (d1 == Dir.Up && d2 == Dir.Down) return true;
        if (d1 == Dir.Down && d2 == Dir.Up) return true;
        if (d1 == Dir.Left && d2 == Dir.Right) return true;
        if (d1 == Dir.Right && d2 == Dir.Left) return true;
        return false;
    }
    public virtual Vector3 UIPosition
    {
        get
        {
            return transform.position + new Vector3(foundation.x / 2f - 0.5f, (height + (furnitureType == FurnitureType.Desk ? 1 : 0)) * 1.6f, foundation.y / 2f - 0.5f) * FurnitureManager.gridSize;
        }
    }
    public static void CancelSelect()
    {
        if (selected)
        {
            selected.OutlineEnabled = false;
            Destroy(selected.repickUI.gameObject);
            selected = null;
        }
    }
    public Transform GetEmptyDeskSlot(Vector3 pos)
    {
        if (furnitureType != FurnitureType.Desk) return null;
        Transform ret = null;
        float minDist = float.MaxValue;
        for (int i = 0; i < decorationTransforms.Length; ++i)
        {
            Transform t = decorationTransforms[i]; float dist = Vector3.Distance(t.position, pos);
            if (t.childCount == 0)
            {
                if (dist < minDist)
                {
                    minDist = dist;
                    ret = t;
                }
            }
        }
        return ret;
    }
}
