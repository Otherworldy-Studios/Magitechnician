/*
Copyright (c) Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#pragma warning disable UDR0001
using UnityEngine;

namespace PluginMaster
{
    public static partial class PWBIO
    {
        
        private static Vector3 _flatteningCenter = Vector3.zero;

        private static void FlatenTerrain()
        {
            var flattenSettings = PinManager.settings.flatteningSettings;
            var paintItem = _paintStroke[0];
            var itemSize = BoundsUtils.GetBoundsRecursive(paintItem.prefab.transform).size * _pinScale;
            flattenSettings.size = new Vector2(itemSize.x, itemSize.z);

            var flatteningAreaCenter = _flatteningCenter;
            var flatteningAreaSize = new Vector3(itemSize.x + flattenSettings.padding * 2, 0, itemSize.z + flattenSettings.padding * 2);
            var flatteningArea = new Bounds(flatteningAreaCenter, flatteningAreaSize);

            float targetHeight = GetTargetHeightUnderPin();

            foreach (var terrain in Terrain.activeTerrains)
            {
                var terrainData = terrain.terrainData;
                var terrainBounds = new Bounds(
                    terrain.transform.position + terrainData.size / 2f,
                    terrainData.size
                );
                if (!terrainBounds.Intersects(flatteningArea)) continue;

                ProcessTerrainFlattening(terrain, terrainData, flattenSettings, targetHeight, flatteningAreaCenter);
            }
        }

        private static float GetTargetHeightUnderPin()
        {
            var terrainUnderPin = _pinHit.collider?.GetComponent<Terrain>();
            if (terrainUnderPin == null) return 0f;

            var terrainData = terrainUnderPin.terrainData;
            var resolution = terrainData.heightmapResolution;
            var transformScale = terrainUnderPin.transform.localScale;
            terrainUnderPin.transform.localScale = Vector3.one;
            var localHit = terrainUnderPin.transform.InverseTransformPoint(_pinHit.point);
            terrainUnderPin.transform.localScale = transformScale;
            var density = new Vector2(1 / terrainData.heightmapScale.x, 1 / terrainData.heightmapScale.z);
            var mapHitX = Mathf.RoundToInt(localHit.x * density.x);
            var mapHitZ = Mathf.RoundToInt(localHit.z * density.y);
            var heighMap = terrainData.GetHeights(0, 0, resolution, resolution);
            var heightOnTerrain = heighMap[Mathf.Clamp(mapHitZ, 0, resolution - 1), Mathf.Clamp(mapHitX, 0, resolution - 1)];
            return heighMap[Mathf.Clamp(mapHitZ, 0, resolution - 1), Mathf.Clamp(mapHitX, 0, resolution - 1)];
        }

        private static void ProcessTerrainFlattening(
            Terrain terrain,
            TerrainData terrainData,
            TerrainFlatteningSettings flattenSettings,
            float targetHeight,
            Vector3 flatteningAreaCenter)
        {
            terrainData.SetTerrainLayersRegisterUndo(terrainData.terrainLayers, "Paint");
            var resolution = terrainData.heightmapResolution;
            var heighMap = terrainData.GetHeights(0, 0, resolution, resolution);

            var transformScale = terrain.transform.localScale;
            terrain.transform.localScale = Vector3.one;
            var localCenter = terrain.transform.InverseTransformPoint(flatteningAreaCenter);
            terrain.transform.localScale = transformScale;

            var density = new Vector2(1 / terrainData.heightmapScale.x, 1 / terrainData.heightmapScale.z);
            var mapCenterX = Mathf.RoundToInt(localCenter.x * density.x);
            var mapCenterZ = Mathf.RoundToInt(localCenter.z * density.y);

            flattenSettings.density = density;
            flattenSettings.angle = -_pinAngle.y;

            var itemHeighmap = flattenSettings.heightmap;
            var itemHeighmapH = itemHeighmap.GetLength(0);
            var itemHeighmapW = itemHeighmap.GetLength(1);

            int halfH = itemHeighmapH / 2;
            int halfW = itemHeighmapW / 2;
            int terrHmapMinX = Mathf.Clamp(mapCenterX - halfH, 0, resolution - 1);
            int terrHmapMinZ = Mathf.Clamp(mapCenterZ - halfW, 0, resolution - 1);
            int terrHmapMaxX = Mathf.Clamp(mapCenterX + halfH, 0, resolution);
            int terrHmapMaxZ = Mathf.Clamp(mapCenterZ + halfW, 0, resolution);

            int w = terrHmapMaxZ - terrHmapMinZ;
            int h = terrHmapMaxX - terrHmapMinX;

            var heights = CalculateFlattenedHeights(
                heighMap, itemHeighmap, targetHeight,
                terrHmapMinX, terrHmapMinZ, mapCenterX, mapCenterZ, halfH, halfW, w, h);

            terrainData.SetHeights(terrHmapMinX, terrHmapMinZ, heights);

            if (flattenSettings.clearDetails)
                ClearTerrainDetails(terrainData, itemHeighmap, terrHmapMinX, terrHmapMinZ,
                    terrHmapMaxX, terrHmapMaxZ, mapCenterX, mapCenterZ, halfH, halfW);

            if (flattenSettings.clearTrees)
                ClearTerrainTrees(terrainData, itemHeighmap, resolution, mapCenterX, mapCenterZ, halfH, halfW);
        }

        private static float[,] CalculateFlattenedHeights(
            float[,] heighMap, float[,] itemHeighmap, float targetHeight,
            int terrHmapMinX, int terrHmapMinZ, int mapCenterX, int mapCenterZ, int halfH, int halfW, int w, int h)
        {
            var heights = new float[w, h];
            for (int x = 0; x < h; ++x)
            {
                for (int z = 0; z < w; ++z)
                {
                    int terrainX = terrHmapMinX + x;
                    int terrainZ = terrHmapMinZ + z;

                    int maskX = terrainX - (mapCenterX - halfH);
                    int maskZ = terrainZ - (mapCenterZ - halfW);

                    maskX = Mathf.Clamp(maskX, 0, itemHeighmap.GetLength(0) - 1);
                    maskZ = Mathf.Clamp(maskZ, 0, itemHeighmap.GetLength(1) - 1);

                    float terrHmapVal = heighMap[terrainZ, terrainX];
                    float itemHmapVal = itemHeighmap[maskX, maskZ];
                    heights[z, x] = Mathf.Lerp(terrHmapVal, targetHeight, itemHmapVal);
                }
            }
            return heights;
        }

        private static void ClearTerrainDetails(
            TerrainData terrainData, float[,] itemHeighmap,
            int terrHmapMinX, int terrHmapMinZ, int terrHmapMaxX, int terrHmapMaxZ,
            int mapCenterX, int mapCenterZ, int halfH, int halfW)
        {
            int detailWidth = terrainData.detailWidth;
            int detailHeight = terrainData.detailHeight;
            int heightmapResolution = terrainData.heightmapResolution;

            float detailToHeightmapX = (float)heightmapResolution / detailWidth;
            float detailToHeightmapZ = (float)heightmapResolution / detailHeight;

            for (int k = 0; k < terrainData.detailPrototypes.Length; ++k)
            {
                var detailLayer = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, k);

                for (int dz = 0; dz < detailHeight; ++dz)
                {
                    for (int dx = 0; dx < detailWidth; ++dx)
                    {
                        int hmapX = Mathf.RoundToInt(dx * detailToHeightmapX);
                        int hmapZ = Mathf.RoundToInt(dz * detailToHeightmapZ);

                        int maskX = hmapX - (mapCenterX - halfH);
                        int maskZ = hmapZ - (mapCenterZ - halfW);

                        if (maskX < 0 || maskX >= itemHeighmap.GetLength(0) ||
                            maskZ < 0 || maskZ >= itemHeighmap.GetLength(1))
                            continue;

                        float itemHmapVal = itemHeighmap[maskX, maskZ];
                        if (itemHmapVal > 0.7f)
                        {
                            detailLayer[dz, dx] = 0;
                        }
                    }
                }
                terrainData.SetDetailLayer(0, 0, k, detailLayer);
            }
        }

        private static void ClearTerrainTrees(
            TerrainData terrainData, float[,] itemHeighmap,
            int resolution, int mapCenterX, int mapCenterZ, int halfH, int halfW)
        {
            var treeInstances = new System.Collections.Generic.List<TreeInstance>();
            foreach (var treeInstance in terrainData.treeInstances)
            {
                int hmapX = Mathf.RoundToInt(treeInstance.position.x * resolution);
                int hmapZ = Mathf.RoundToInt(treeInstance.position.z * resolution);
                int maskX = Mathf.Clamp(hmapX - (mapCenterX - halfH), 0, itemHeighmap.GetLength(0) - 1);
                int maskZ = Mathf.Clamp(hmapZ - (mapCenterZ - halfW), 0, itemHeighmap.GetLength(1) - 1);
                float itemHmapVal = itemHeighmap[maskX, maskZ];
                if (itemHmapVal < 0.9f)
                    treeInstances.Add(treeInstance);
            }
            terrainData.treeInstances = treeInstances.ToArray();
        }
    }
}
#pragma warning restore UDR0001
