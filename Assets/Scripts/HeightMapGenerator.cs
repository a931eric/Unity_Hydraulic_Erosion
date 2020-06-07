using UnityEngine;

public static class HeightMapGenerator{
    public static void Perlin(float[,] map, int w, int h, Vector2 pos, float rot, float scale)
    {


        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                map[x, y] = 0;
                for (float f = 1; f <= 4.0f / scale; f *= 2)
                {
                    map[x, y] += 0.5f / f * Mathf.PerlinNoise((x * Mathf.Cos(rot) - y * Mathf.Sin(rot)) * scale * f + pos.x, (x * Mathf.Sin(rot) + y * Mathf.Cos(rot)) * scale * f + pos.y);
                }
            }
        }
    }
}
