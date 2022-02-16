using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForUpdate = 25f;
    const float sqrviewerMoveThresholdForUpdate = viewerMoveThresholdForUpdate * viewerMoveThresholdForUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDistance;

    public Transform viewer;
    public Material mapMaterial;
    public static Vector2 viewerPos;
    Vector2 viewerPosOld;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(maxViewDistance / chunkSize);
        UpdateVisibleChunks();
    }
    private void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;
        if ((viewerPosOld - viewerPos).sqrMagnitude > sqrviewerMoveThresholdForUpdate)
        {
            viewerPosOld = viewerPos;
            UpdateVisibleChunks();
        }
    }
    void UpdateVisibleChunks()
    {
        if (terrainChunksVisibleLastUpdate != null)
        {
            foreach (TerrainChunk chunk in terrainChunksVisibleLastUpdate)
            {
                chunk.SetVisible(false);
            }
            terrainChunksVisibleLastUpdate.Clear();
        }
        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);
        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
        {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIdx = -1;
        public TerrainChunk(Vector2 coordinate, int size,LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);

            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("TerrainChunk");

            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }
            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }
        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }
        public void UpdateTerrainChunk()
        {
            if (!mapDataReceived) return;
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool visible = viewerDstFromNearestEdge <= maxViewDistance;

            if (visible)
            {
                int lodIdx = 0;
                for (int i = 0; i < detailLevels.Length-1; i++)
                {
                    if(viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                    {
                        lodIdx = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                if(lodIdx!= previousLODIdx)
                {
                    LODMesh lodMesh = lodMeshes[lodIdx];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIdx = lodIdx;
                        meshFilter.mesh = lodMesh.mesh;
                    }else if (!lodMesh.hasReqeuestedMesh)
                    {
                        lodMesh.RequestMesh(mapData);
                    }
                }
                if (lodIdx == 0)
                {
                    if (collisionLODMesh.hasMesh)
                    {
                        meshCollider.sharedMesh = collisionLODMesh.mesh;
                    }else if (!collisionLODMesh.hasReqeuestedMesh)
                    {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                }
                terrainChunksVisibleLastUpdate.Add(this);
            }
            SetVisible(visible);
        }
        public void SetVisible (bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool isVisible()
        {
            return meshObject.activeSelf;
        }
    }
    class LODMesh
    {
        public Mesh mesh;
        public bool hasReqeuestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;
        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }
        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }
        public void RequestMesh(MapData mapData)
        {
            hasReqeuestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }
    }
    [System.Serializable] public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold;
        public bool useForCollider;
    }
}
