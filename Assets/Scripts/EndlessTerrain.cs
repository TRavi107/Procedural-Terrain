using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const int scale=5;
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrviewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    public LODInfo[] detailLevels;
    public static float maxViewDist;
    public Transform viewer;
    public Material mapMaterial;
    public static Vector2 viewerPosition;
    Vector2 oldViewerPos;
    public static MapGenerator mapGenerator;
    int chunkSize;
    int visibleChunkDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunkVisibleLastUpdate = new List<TerrainChunk>();
    private void Start()
    {
        maxViewDist = detailLevels[detailLevels.Length-1].lodDetailDistance;
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.MAPCHUNKSIZE-1;
        visibleChunkDistance = Mathf.RoundToInt(maxViewDist / chunkSize);
        UpdateVisibleChunk();
    }
    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/scale;
        if ((viewerPosition - oldViewerPos).sqrMagnitude > sqrviewerMoveThresholdForChunkUpdate)
        {
            oldViewerPos = viewerPosition;
            UpdateVisibleChunk();
        }
    }
    void UpdateVisibleChunk()
    {
        foreach (var item in terrainChunkVisibleLastUpdate)
        {
            item.SetVisible(false);
        }
        terrainChunkVisibleLastUpdate.Clear();
        int currentChunkCordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int offsetY = -visibleChunkDistance; offsetY <= visibleChunkDistance; offsetY++)
        {
            for (int offsetX = -visibleChunkDistance; offsetX <= visibleChunkDistance; offsetX++)
            {
                Vector2 viewedChunkCoordinate = new Vector2(currentChunkCordX + offsetX, currentChunkCordY + offsetY);
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoordinate))
                {
                    terrainChunkDictionary[viewedChunkCoordinate].UpdateTerrainChunk();
                    
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate,chunkSize,detailLevels,transform, mapMaterial));
                }
            }
        }
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bound;
        MeshRenderer meshRender;
        MeshFilter meshFilter;
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        MapData mapData;
        bool mapDataReceived;
        int previousLodIndex = -1;
        public TerrainChunk(Vector2 pos, int size,LODInfo[] lodInfos, Transform parent,Material material)
        {
            this.detailLevels = lodInfos;
            position = pos * size;
            bound = new Bounds(position, Vector2.one * size);
            Vector3 posV3 = new Vector3(position.x, 0, position.y);
            meshObject = new GameObject("TerrainChunk");
            meshRender = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRender.material = material;
            meshObject.transform.position = posV3*scale;
            meshObject.transform.localScale = Vector3.one * scale;
            meshObject.transform.SetParent(parent);
            lodMeshes = new LODMesh[lodInfos.Length];

            for (int i = 0; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(lodInfos[i].lod,UpdateTerrainChunk);
            }
            //meshObject.transform.localScale = Vector3.one * size / 10f;
            SetVisible(false);
            mapGenerator.RequestMapData(position,OnMapDataReceived);
        }
        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.MAPCHUNKSIZE, MapGenerator.MAPCHUNKSIZE);
            meshRender.material.mainTexture = texture;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewDistanceFromNearestEdge = Mathf.Sqrt(bound.SqrDistance(viewerPosition));
                bool isVisible = viewDistanceFromNearestEdge < maxViewDist;
                if (isVisible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewDistanceFromNearestEdge > detailLevels[i].lodDetailDistance)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (lodIndex != previousLodIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLodIndex = lodIndex;

                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunkVisibleLastUpdate.Add(this);
                }
                SetVisible(isVisible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
         int lod;
        Action updateCallback;

        public LODMesh(int lod,Action updateCallBack)
        {
            this.lod = lod;
            this.updateCallback = updateCallBack;
        }
        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }
        public void RequestMesh(MapData mapdata)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapdata, lod, OnMeshDataReceived);
        }
    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float lodDetailDistance;
    }
}


