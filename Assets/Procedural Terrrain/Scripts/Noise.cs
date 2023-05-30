using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { Local, Global };
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale,int octaves , float lacunarity , float persistant, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        if (scale <= 0)
            scale = 0.0001f;

        System.Random prng = new System.Random(seed);
        Vector2[] octavesOffSet = new Vector2[octaves];
        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight=0;
        for (int i = 0; i < octaves; i++)
        {
            float octaveX = prng.Next(-100000, 100000)+offset.x;
            float octaveY = prng.Next(-100000, 100000) - offset.y;
            octavesOffSet[i] = new Vector2(octaveX, octaveY);
            maxPossibleHeight += amplitude;
            amplitude *= persistant;
        }

        float minNoiseHeightLocal = float.MaxValue;
        float maxNoiseHeightLocal = float.MinValue;
        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2;
        for (int y = 0; y < mapHeight; y++)
        {
            
            for (int x = 0; x < mapWidth; x++)
            {
                 amplitude = 1;
                 frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                
                    float sampleX = (x-halfWidth + octavesOffSet[i].x) / scale*frequency ;
                    float sampleY = (y-halfHeight + octavesOffSet[i].y) / scale*frequency ;

                    float perlinNoise = Mathf.PerlinNoise(sampleX, sampleY)*2 -1;
                    noiseHeight += perlinNoise * amplitude;
                    frequency *= lacunarity;
                    amplitude *= persistant;
                }

                if (noiseHeight < minNoiseHeightLocal)
                    minNoiseHeightLocal = noiseHeight;
                else if (noiseHeight > maxNoiseHeightLocal)
                    maxNoiseHeightLocal = noiseHeight;
                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if(normalizeMode==NormalizeMode.Local)
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeightLocal, maxNoiseHeightLocal, noiseMap[x, y]);
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight/1.75f);
                    noiseMap[x, y] =Mathf.Clamp( normalizedHeight,0,int.MaxValue);
                }
            }
        }
        return noiseMap;
    }
}
