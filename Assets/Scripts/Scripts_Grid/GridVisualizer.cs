using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridManager))]
public class GridVisualizer : MonoBehaviour
{
    public static GridVisualizer Instance { get; private set; }

    [SerializeField, Min(0.001f)] float lineWidth = 0.04f;
    [SerializeField] int sortingOrder = 100;
    [SerializeField] bool alwaysVisible = true;

    GameObject lines;

    void Awake()
    {
        Instance = this;
        BuildMesh();
        SetVisible(false);
    }

    public void SetVisible(bool visible) => lines.SetActive(visible || alwaysVisible);

    void BuildMesh()
    {
        var grid = GetComponent<GridManager>();
        Vector3 o = grid.Origin;
        float cs = grid.CellSize;
        int w = grid.GridSize.x, h = grid.GridSize.y;
        float hw = lineWidth * 0.5f;

        var verts = new List<Vector3>();
        var tris = new List<int>();

        void Quad(float x0, float y0, float x1, float y1)
        {
            int i = verts.Count;
            verts.Add(o + new Vector3(x0, y0, 0));
            verts.Add(o + new Vector3(x1, y0, 0));
            verts.Add(o + new Vector3(x1, y1, 0));
            verts.Add(o + new Vector3(x0, y1, 0));
            tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }
        
        for (int j = 0; j <= h; j++)
            Quad(-hw, j * cs - hw, w * cs + hw, j * cs + hw);
        
        for (int i = 0; i <= w; i++)
            for (int r = 0; r < h; r++)
                Quad(i * cs - hw, r * cs + hw, i * cs + hw, (r + 1) * cs - hw);

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        
        lines = new GameObject("GridLines");
        lines.AddComponent<MeshFilter>().mesh = mesh;
        var mr = lines.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default")) { color = grid.LineColor };
        mr.sortingOrder = sortingOrder;
    }

    void OnDestroy()
    {
        if (lines != null) Destroy(lines);
    }
}