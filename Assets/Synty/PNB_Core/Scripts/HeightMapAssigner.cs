using UnityEngine;

[ExecuteAlways]
public class HeightmapAssigner : MonoBehaviour
{
    public Terrain targetTerrain;
    public Material targetMaterial;

    void Update()
    {
        if (targetTerrain == null || targetMaterial == null) return;

        // Get the heightmap texture from the terrain
        Texture heightmap = targetTerrain.terrainData.heightmapTexture;

        // Pass the data to the shader
        targetMaterial.SetTexture("_TerrainHeightMap", heightmap);
        targetMaterial.SetVector("_TerrainPos", targetTerrain.transform.position);
        targetMaterial.SetVector("_TerrainSize", targetTerrain.terrainData.size);
    }
}