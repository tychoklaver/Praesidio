using UnityEngine;

[RequireComponent(typeof(GridManager))]
public class GridVisualizer : MonoBehaviour
{
    public static GridVisualizer Instance { get; private set; }

    [SerializeField, Min(0.001f)] float lineWidth = 0.04f;
    [SerializeField] int textureSize = 128;

    [SerializeField] bool mipmaps = true;
    [SerializeField] int sortingOrder = 100;
    [SerializeField] bool alwaysVisible = true;
    
    GameObject lines;
    Mesh mesh;
    Texture2D texture;
    Material material;
    bool visible;

    void Awake()
    {
        Instance = this;
        Build();
        SetVisible(false);
    }

    public void SetVisible(bool value)
    {
        visible = value;
        if (lines != null) lines.SetActive(visible || alwaysVisible);
    }
    
    public void Rebuild()
    {
        Cleanup();
        Build();
        lines.SetActive(visible || alwaysVisible);
    }

    void Build()
    {
        var grid = GetComponent<GridManager>();
        Vector3 o = grid.Origin;
        float cs = grid.CellSize;
        Vector2Int size = grid.GridSize;
        
        int halfBand = Mathf.Clamp(Mathf.RoundToInt(lineWidth / cs * textureSize * 0.5f), 1, textureSize / 4);
        texture = CreateCellTexture(textureSize, halfBand);
        
        float padCells = (float)halfBand / textureSize;
        float pad = padCells * cs;

        mesh = new Mesh { name = "GridQuad" };
        mesh.vertices = new[]
        {
            o + new Vector3(-pad, -pad, 0f),
            o + new Vector3(size.x * cs + pad, -pad, 0f),
            o + new Vector3(size.x * cs + pad, size.y * cs + pad, 0f),
            o + new Vector3(-pad, size.y * cs + pad, 0f)
        };

        mesh.uv = new[]
        {
            new Vector2(-padCells, -padCells),
            new Vector2(size.x + padCells, -padCells),
            new Vector2(size.x + padCells, size.y + padCells),
            new Vector2(-padCells, size.y + padCells)
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };

        material = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = texture,
            color = grid.LineColor
        };
        
        lines = new GameObject("GridLines");
        lines.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = lines.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.sortingOrder = sortingOrder;
    }

    Texture2D CreateCellTexture(int size, int halfBand)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipmaps)
        {
            name = "GridCell",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear,
            anisoLevel = 4
        };

        var pixels = new Color32[size * size];
        var line = new Color32(255, 255, 255, 255);
        var empty = new Color32(255, 255, 255, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isLine = x < halfBand || x >= size - halfBand
                           || y < halfBand || y >= size - halfBand;
                pixels[y * size + x] = isLine ? line : empty;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(mipmaps);
        return tex;
    }

    void Cleanup()
    {
        if (lines != null) Destroy(lines);
        if (mesh != null) Destroy(mesh);
        if (texture != null) Destroy(texture);
        if (material != null) Destroy(material);
    }

    void OnDestroy() => Cleanup();
}