using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap,float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertIdx = 0;
        int borderVertIdx = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVert = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVert)
                {
                    vertexIndicesMap[x, y] = borderVertIdx;
                    borderVertIdx--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertIdx;
                    meshVertIdx++;
                }
            }
        }
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertIdx = vertexIndicesMap[x, y];
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertIdx);

                if(x<borderedSize-1&&y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x+meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y+meshSimplificationIncrement];
                    int d = vertexIndicesMap[x+meshSimplificationIncrement, y+meshSimplificationIncrement];
                    meshData.AddTriangle(a,d,c);
                    meshData.AddTriangle(d,a,b);
                }
                vertIdx++;
            }
        }
        meshData.BakeNormals();
        return meshData;
    }
}
public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    Vector3[] bakedNormals;
    Vector3[] borderVerts;
    int[] borderTris;

    int triIdx;
    int borderTriIdx;
    public MeshData(int vertsPerLine)
    {
        vertices = new Vector3[vertsPerLine * vertsPerLine];
        uvs = new Vector2[vertsPerLine * vertsPerLine];
        triangles = new int[(vertsPerLine - 1) * (vertsPerLine - 1) * 6];

        borderVerts = new Vector3[vertsPerLine * 4 + 4];
        borderTris = new int[24 * vertsPerLine];
    }
    public void AddVertex(Vector3 vertPos, Vector2 uv, int vertIdx)
    {
        if (vertIdx < 0)
        {
            borderVerts[-vertIdx - 1] = vertPos;
        }
        else
        {
            vertices[vertIdx] = vertPos;
            uvs[vertIdx] = uv;
        }
    }
    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTris[borderTriIdx] = a;
            borderTris[borderTriIdx + 1] = b;
            borderTris[borderTriIdx + 2] = c;
            borderTriIdx += 3;
        }
        else
        {
            triangles[triIdx] = a;
            triangles[triIdx + 1] = b;
            triangles[triIdx + 2] = c;
            triIdx += 3;
        }
    }
    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIdx = i * 3;
            int vertexIdxA = triangles[normalTriangleIdx];
            int vertexIdxB = triangles[normalTriangleIdx + 1];
            int vertexIdxC = triangles[normalTriangleIdx + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIdxA, vertexIdxB, vertexIdxC);
            vertexNormals[vertexIdxA] += triangleNormal;
            vertexNormals[vertexIdxB] += triangleNormal;
            vertexNormals[vertexIdxC] += triangleNormal;
        }
        int borderTriCount = borderTris.Length / 3;
        for (int i = 0; i < borderTriCount; i++)
        {
            int normalTriangleIdx = i * 3;
            int vertexIdxA = borderTris[normalTriangleIdx];
            int vertexIdxB = borderTris[normalTriangleIdx + 1];
            int vertexIdxC = borderTris[normalTriangleIdx + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIdxA, vertexIdxB, vertexIdxC);
            if (vertexIdxA >= 0) vertexNormals[vertexIdxA] += triangleNormal;
            if (vertexIdxB >= 0) vertexNormals[vertexIdxB] += triangleNormal;
            if (vertexIdxC >= 0) vertexNormals[vertexIdxC] += triangleNormal;
        }
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }
    Vector3 SurfaceNormalFromIndices(int idxA, int idxB, int idxC)
    {
        Vector3 pointA = (idxA < 0) ? borderVerts[-idxA - 1] : vertices[idxA];
        Vector3 pointB = (idxB < 0) ? borderVerts[-idxB - 1] : vertices[idxB];
        Vector3 pointC = (idxC < 0) ? borderVerts[-idxC - 1] : vertices[idxC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = bakedNormals;
        return mesh;
    }
}
