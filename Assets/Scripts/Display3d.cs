using UnityEngine;

public class Display3d : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Material forRealColor, forGeneratedColor, water;
    public MeshCollider meshCollider;
    public float[,] heightmap;
    public Material material;
    public Vector2Int pos;
    public int size;
    Vector3[] vertices;
    Mesh mesh;
    bool initialized = false;
    /*
    public void DrawTerrain(float[,] heightmap)
    {
        GetComponent<MeshRenderer>().material = forGeneratedColor;
        SetMesh(heightmap);
    }

    public void DrawTerrainWithRealColor(float[,] heightmap, Color[,] colormap)
    {
        SetMesh(heightmap);
        SetColor(colormap);
    }

    public void DrawWater(float[,] heightmap)
    {
        GetComponent<MeshRenderer>().material = water;
        SetMesh(heightmap);
    }
    */

    void Init()
    {
        vertices = new Vector3[size * size];
        mesh = GetComponent<MeshFilter>().mesh;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                vertices[x + y * size] = new Vector3(x,0, y);
            }
        }
        mesh.vertices = vertices;
        int[] triangles = new int[(size - 1) * (size - 1) * 6];
        int i = 0, j = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x < size - 1 && y < size - 1)
                {
                    triangles[6 * j] = i;
                    triangles[6 * j + 1] = i + size;
                    triangles[6 * j + 2] = i + 1 + size;
                    triangles[6 * j + 3] = i;
                    triangles[6 * j + 4] = i + 1 + size;
                    triangles[6 * j + 5] = i + 1;
                    j++;
                }
                i++;
            }
        }
        mesh.triangles = triangles;
        Vector2[] uvs = new Vector2[size*size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                uvs[x + y * size] = new Vector2(x / (float)size, y / (float)size);
            }
        }
        mesh.uv = uvs;
        initialized = true;
    }
    public void SetColor(Color[,] colormap)
    {
        int w = colormap.GetLength(0), h = colormap.GetLength(1);
        Color[] colormap_ = new Color[w * h];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                colormap_[x + y * w] = colormap[x, y];
            }
        }

        Texture2D texture = new Texture2D(w, h);
        texture.SetPixels(colormap_);
        texture.Apply();
        meshRenderer = GetComponent<MeshRenderer>();
        var tempMaterial = new Material(forRealColor);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        tempMaterial.SetTexture("_Texture", texture);
        meshRenderer.material = tempMaterial;
    }



    public void SetMesh()
    {
        meshRenderer.material = material;
        if (!initialized) Init();
        
        for (int x = 0; x < size; x++)
        {
            if (pos.x + x == heightmap.GetLength(0)) break;
            for (int y = 0; y < size; y++)
            {
                if (pos.y + y == heightmap.GetLength(1)) break;
                vertices[x + y * size] = new Vector3(x, heightmap[pos.x+x, pos.y+y], y);
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        meshCollider.sharedMesh = mesh;
    }
}
