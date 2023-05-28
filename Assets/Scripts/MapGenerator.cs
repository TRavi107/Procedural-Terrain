using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { texture, color,mesh,fallOff}
    public Noise.NormalizeMode normalizeMode;
    public DrawMode drawMode;
    public const int MAPCHUNKSIZE = 241;
    [Range(0, 6)]
    public int LODEditorPreview;  
    public float noiseScale;
    public int octaves;
    public float lacunarity;
    [Range(0,1)]
    public float persistant;
    public int seed;
    public Vector2 offset;
    public float heightMultiplier;
    public AnimationCurve heightMultiplierCurve;
    public bool autoUpdate;
    public bool useFallOffMap;
    public TerrainType[] terrainType;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    [SerializeField] MapDisplay mapDisplay;
    float[,] fallOffdata;
    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMap(Vector2.zero);
        if (drawMode == DrawMode.texture)
            mapDisplay.Drawtexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.color)
        {
            mapDisplay.Drawtexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, MAPCHUNKSIZE, MAPCHUNKSIZE));
        }
        else if (drawMode == DrawMode.mesh)
        {
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, heightMultiplierCurve, LODEditorPreview), 
                TextureGenerator.TextureFromColorMap(mapData.colorMap, MAPCHUNKSIZE, MAPCHUNKSIZE));
        }
        else if (drawMode == DrawMode.fallOff)
        {
            mapDisplay.Drawtexture(TextureGenerator.TextureFromHeightMap(fallOffdata));
        }
    }
    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadstart = delegate
        {
            MapdataThread(center,callback);
        };
        new Thread(threadstart).Start();
    }
    void MapdataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMap(center);
        lock(mapDataThreadInfoQueue){
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData,int levelOfDetail, Action<MeshData> callback)
    {
        ThreadStart threadstart = delegate
        {
            MeshdataThread(mapData, levelOfDetail, callback);
        };
        new Thread(threadstart).Start();
    }
    void MeshdataThread(MapData mapData,int levelOfDetail, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,heightMultiplier,heightMultiplierCurve, levelOfDetail);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }
    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
            
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }
    }
    MapData GenerateMap(Vector2 center)
    {
        float[,] noise = Noise.GenerateNoiseMap(MAPCHUNKSIZE, MAPCHUNKSIZE,seed, noiseScale,octaves,lacunarity,persistant, center+offset,normalizeMode);

        Color[] color = new Color[MAPCHUNKSIZE * MAPCHUNKSIZE]; 
        for (int y = 0; y < MAPCHUNKSIZE; y++)
        {
            for (int x = 0; x < MAPCHUNKSIZE; x++)
            {
                if (useFallOffMap)
                {
                    noise[x, y] =Mathf.Clamp01( noise[x, y] - fallOffdata[x, y]);
                }
                float currentHeight = noise[x, y];
                for (int i = 0; i < terrainType.Length; i++)
                {
                    if (currentHeight > terrainType[i].height)
                    {
                        color[MAPCHUNKSIZE * y + x] = terrainType[i].color;
                        
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noise, color);
    }
    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 1)
            octaves = 1;
        fallOffdata = FallOffGenerator.GenerateFallOffMap(MapGenerator.MAPCHUNKSIZE);
    }

    struct MapThreadInfo<T>
    {
        public Action<T> callBack;
        public T parameter;

        public MapThreadInfo(Action<T> callBack, T parameter)
        {
            this.callBack = callBack;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}
public struct MapData
{
    public float[,] heightMap;
    public Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
