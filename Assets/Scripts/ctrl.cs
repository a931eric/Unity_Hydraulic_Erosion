﻿using UnityEngine;
using System.IO;
using System.Net.Http;
using System;

public class ctrl : MonoBehaviour
{
    GameObject chunkParent;
    public Display2d d2;
    public GameObject chunkPrefab;
    public Terrain terrain;

    public float[,] heightMap;
    public float[,] waterMap;
    public Color[,] colorMap = null;
    public Texture2D texture_h, texture_c;
    public int chunkSize = 100;
    public Display3d[,,] chunks;
    public bool water;
    public Rain3 rain;

    public int w, h,lW=-1,lH=-1;
    [System.Serializable]
    public class PerlinNoiseInfo
    {
        public float yScale, rot;
        public Vector2 pos;
        public float scale = 0.2f;
        public AnimationCurve heightCurve;
    }
    public PerlinNoiseInfo perlin;

    public enum DisplayMode
    {
        RealColor, TextureColor
    };
    public DisplayMode displayMode;

    public void GeneratePerlinNoise()
    {
        HeightMapGenerator.Perlin(heightMap,w, h, perlin.pos, perlin.rot, perlin.scale);
        waterMap = new float[w, h];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                heightMap[x, y] = Mathf.Max(0, perlin.heightCurve.Evaluate(heightMap[x, y]) * perlin.yScale / perlin.scale);
            }
        }
        Display();
    }
    public void Flat()
    {
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                heightMap[x, y] = 0;
                waterMap[x, y] = 0;
            }
        }
        Display();
    }
    public void Terrain()
    {

        float[,] heightMap_ = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);

        w = heightMap_.GetLength(0);
        h = heightMap_.GetLength(1);
        heightMap = new float[w, h];
        waterMap = new float[w, h];
        colorMap = new Color[heightMap_.GetLength(0), heightMap_.GetLength(1)];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                heightMap[x, y] = heightMap_[y, x] * perlin.yScale;
            }
        }
        Display();

        Color[] colormap_ = new Color[w * h];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                colormap_[x + y * w] = new Color(heightMap[x, y] * 256, heightMap[x, y] * 256, heightMap[x, y] * 256);
            }
        }
    }
    public void ReadPng()
    {
        heightMap = new float[texture_h.width, texture_h.height];
        colorMap = new Color[texture_h.width, texture_h.height];
        w = heightMap.GetLength(0);
        h = heightMap.GetLength(1);

        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                heightMap[i, j] = texture_h.GetPixel(i, j).r * perlin.yScale;
                colorMap[i, j] = texture_c.GetPixel(i, j);
            }
        }
        waterMap = new float[w, h];
        Display();
    }
    public void ReadPngFromDisk()
    {
        FileStream fs = new FileStream(@".\fromGAN\fromGAN.png", FileMode.Open);
        byte[] png = new byte[fs.Length];
        fs.Read(png, 0, (int)fs.Length);
        fs.Dispose();
        heightMap = PNGToMap(heightMap,png, 0, perlin.yScale);
        waterMap = new float[w, h];
        Display();
    }

    public void ResetChunks()
    {
        if (chunkParent != null)
            DestroyImmediate(chunkParent);
        chunkParent = new GameObject();
        int w, h;
        w = heightMap.GetLength(0);
        h = heightMap.GetLength(1);
        int[] chunk_n = { Mathf.CeilToInt(w / (float)chunkSize), Mathf.CeilToInt(h / (float)chunkSize) };
        chunks = new Display3d[chunk_n[0], chunk_n[1],5];
        for (int i = 0; i < chunk_n[0]; i++)
            for (int j = 0; j < chunk_n[1]; j++)
            {
                chunks[i, j,0] = Instantiate(chunkPrefab, transform.position + new Vector3(i * chunkSize, 0, j * chunkSize), new Quaternion(0, 0, 0, 1), chunkParent.transform).GetComponent<Display3d>();
                chunks[i, j, 0].size = chunkSize+1;
                chunks[i, j, 0].pos = new Vector2Int(i * chunkSize, j * chunkSize);
                chunks[i, j,0].heightmap = heightMap;
                chunks[i, j, 0].material = chunks[i, j, 0].forGeneratedColor;
                chunks[i, j, 1] = Instantiate(chunkPrefab, transform.position + new Vector3(i * chunkSize, 0, j * chunkSize), new Quaternion(0, 0, 0, 1), chunkParent.transform).GetComponent<Display3d>();
                chunks[i, j, 1].size = chunkSize+1;
                chunks[i, j, 1].pos = new Vector2Int(i * chunkSize, j * chunkSize);
                chunks[i, j, 1].heightmap = waterMap;
                chunks[i, j, 1].material = chunks[i, j, 1].water;
            }
    }

    public void Display()
    {
        
        if (lW != w || lH != h)
        {
            ResetChunks();
            lW = w;
            lH = h;
        }
        for (int i = 0; i < chunks.GetLength(0); i++)
            for (int j = 0; j < chunks.GetLength(1); j++)
            {
                chunks[i, j, 0].SetMesh();
                chunks[i, j, 1].SetMesh();
            }
        d2.Display(heightMap);
    }

    public int t = 0;
    public void SaveAsPNG(float[,] map)
    {

        FileStream fs = new FileStream(@".\toGAN\fromCS" + t++ + ".png", FileMode.Create);
        float[] minmax = MinMax(map);
        byte[] png = MapToPng(map, minmax[0], minmax[1]);
        fs.Write(png, 0, png.Length);
        fs.Close();
    }
    float[] MinMax(float[,] map)
    {
        w = map.GetLength(0);
        h = map.GetLength(1);
        float min = map[0, 0];
        float max = map[0, 0];
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                if (map[i, j] > max) max = map[i, j];
                if (map[i, j] < min) min = map[i, j];
            }
        }
        return new float[2] { min, max };
    }
    byte[] MapToPng(float[,] map, float min, float max, string mode = "GRAY")
    {
        w = map.GetLength(0);
        h = map.GetLength(1);
        Texture2D o = new Texture2D(w, h);
        Color[] img = new Color[w * h];
        if (mode == "GRAY")
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    float v = (map[i, j] - min) / (max - min);
                    img[i + j * h] = new Color(v, v, v);
                }
            }
        else if (mode == "RGB")
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    float v = (map[i, j] - min) / (max - min);
                    img[i + j * h] = new Color(v, (v * 256) % 1, (v * 65536) % 1);
                }
            }
        o.SetPixels(img);
        o.Apply();
        return o.EncodeToPNG();
    }
    float[,] PNGToMap(float[,]o,byte[] png, float min, float max, string mode = "GRAY")//uses global variables w and h
    {
        Texture2D texture = new Texture2D(w, h);
        texture.LoadImage(png);
        if (mode == "GRAY")
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    Color p = texture.GetPixel(i, j);
                    o[i, j] = p.r * (max - min) + min;
                }
            }
        else if (mode == "RGB")
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    Color p = texture.GetPixel(i, j);
                    o[i, j] = (p.r + p.g / 256 + p.b / 65536) * (max - min) + min;
                }
            }
        return o;
    }
    Color[,] PNGToColorMap(byte[] png)//uses global variables w and h
    {
        Color[,] o = new Color[w, h];
        Texture2D texture = new Texture2D(w, h);
        texture.LoadImage(png);
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                o[i, j] = texture_h.GetPixel(i, j);
            }
        }
        return o;
    }

    public string postUrl;
    public string getUrl;
    [System.Serializable]
    class ResponseData
    {
        public string file_name = "";
    }
    class PostData
    {
        public int overlay = 128;
        public string file;
        public PostData(int overlay, string file)
        {
            this.overlay = overlay;
            this.file = file;
        }
    }
    public float ganScale = 0.5f;
    public async void HeightmapGAN()
    {
        float[] minmax = MinMax(heightMap);
        string pngBase64 = Convert.ToBase64String(MapToPng(heightMap, minmax[0], minmax[1], mode: "GRAY"));
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        string response;
        using (var content = new StringContent(JsonUtility.ToJson(new PostData(128, pngBase64)), System.Text.Encoding.UTF8, "application/json"))
        {
            var postResult = await client.PostAsync(postUrl, content);
            response = await postResult.Content.ReadAsStringAsync();
        }
        print("response:" + response);
        ResponseData responseData = JsonUtility.FromJson<ResponseData>(response);
        byte[] getData = await client.GetByteArrayAsync(getUrl + responseData.file_name + ".png");
        PNGToMap(heightMap,getData, minmax[0], (minmax[1]- minmax[0])* ganScale+ minmax[0], mode: "GRAY");
        waterMap = new float[w, h];
        Display();
    }

    public string postUrl_sat;
    public string getUrl_sat;
    public async void SatelliteGAN()
    {
        
        float[] minmax = MinMax(heightMap);
        string pngBase64 = Convert.ToBase64String(MapToPng(heightMap, minmax[0], minmax[1], mode: "GRAY"));
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        string response;
        using (var content = new StringContent(JsonUtility.ToJson(new PostData(128, pngBase64)), System.Text.Encoding.UTF8, "application/json"))
        {
            var postResult = await client.PostAsync(postUrl_sat, content);
            response = await postResult.Content.ReadAsStringAsync();
        }
        print("response:" + response);
        ResponseData responseData = JsonUtility.FromJson<ResponseData>(response);
        byte[] getData = await client.GetByteArrayAsync(getUrl_sat + responseData.file_name + ".png");
        texture_c = new Texture2D(w, h);
        texture_c.LoadImage(getData);
        colorMap = new Color[w, h];
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                colorMap[i, j] = texture_c.GetPixel(i, j);
            }
        }
        ResetChunks();
        chunks[0, 0, 0].SetColor(colorMap);
        int[] chunk_n = { Mathf.CeilToInt(w / (float)chunkSize), Mathf.CeilToInt(h / (float)chunkSize) };
        for (int i = 0; i < chunk_n[0]; i++)
            for (int j = 0; j < chunk_n[1]; j++)
            {
                chunks[i, j, 0].material = chunks[i, j, 0].forRealColor;
                chunks[i, j, 0].material.SetTexture("_Texture", texture_c);
            }
        Display();
    }

    private void Start()
    {
        heightMap = new float[w, h];
        waterMap = new float[w, h];
        Flat();
    }

    public float smoothness = .03f;
    public int updateDeltaFrame = 3;
    public float drawWeight = 0.2f;
    public GameObject pointer;
    int frames;
    public void Update()
    {
        int x, z; Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            
            pointer.transform.position = hit.point;
            pointer.transform.localScale =Vector3.one/Mathf.Sqrt( smoothness)*2;
            smoothness *= Mathf.Pow(1.1f,-Input.mouseScrollDelta.y);
            if (Input.GetMouseButton(0))
            {
                x = (int)hit.point.x;
                z = (int)hit.point.z;
                
                for (int i = x - 50; i < x + 50; i++)
                {
                    for (int j = z - 50; j < z + 50; j++)
                    {
                        if (i < w && i >= 0 && j < h && j >= 0)
                            heightMap[i, j]= Mathf.Max(0, heightMap[i, j] + Mathf.Exp(-smoothness * ((i - x) * (i - x) + (j - z) * (j - z))) * (Input.GetKey(KeyCode.LeftShift)|| Input.GetKey(KeyCode.RightShift) ? -1 : 1) * drawWeight);
                    }
                }
                frames++;
            }
            
            if (frames >= updateDeltaFrame)
            {
                Display();
                frames = 0;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            Display();
            frames = 0;
        }

    }
}
