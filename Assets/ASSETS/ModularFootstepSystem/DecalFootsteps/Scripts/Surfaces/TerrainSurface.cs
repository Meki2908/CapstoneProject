namespace ModularFootstepSystem
{
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Surface with <see cref="Terrain"/> component.
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    public class TerrainSurface : AbstractSurface
    {
        [SerializeField]
        protected TerrainSurfaceType[] terrainSurfaceTypes = default;

        protected Terrain terrain = default;

        protected int layerIndex = 0;
        protected int positionOnTerrainX = 0;
        protected int positionOnTerrainZ = 0;

        protected Vector3 terrainPosition = default;

        protected float[] textureMix = null;
        protected float[] cellMix = null;
        protected float[,,] splatmapData = null;

        protected virtual void Awake()
        {
            terrain = GetComponent<Terrain>();
            terrainPosition = terrain.transform.position;
        }

        public override string GetSurfaceType(Vector3 worldPosition)
        {
            layerIndex = GetMainTexture(worldPosition);

            if (layerIndex >= 0)
            {
                foreach (TerrainSurfaceType terrainSurfaceType in terrainSurfaceTypes)
                {
                    if (terrainSurfaceType.TerrainLayer == terrain.terrainData.terrainLayers[layerIndex])
                    {
                        return terrainSurfaceType.SurfaceType.Id;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates the index of the texture that has maximum effect on the Terrain surface.
        /// </summary>
        /// <param name="worldPos">World position at which contact with the surface occurred.</param>
        /// <returns>Index of the texture.</returns>
        protected virtual int GetMainTexture(Vector3 worldPos)
        {
            textureMix = GetTextureMix(worldPos);

            if(textureMix == null)
            {
                return -1;
            }

            float maxMix = 0;
            int maxIndex = 0;

            for (int textureindex = 0; textureindex < textureMix.Length; textureindex++)
            {
                if (textureMix[textureindex] > maxMix)
                {
                    maxIndex = textureindex;
                    maxMix = textureMix[textureindex];
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Computes a list of textures at the point of contact between the foot and the surface.
        /// </summary>
        /// <param name="worldPos">World position at which contact with the surface occurred.</param>
        /// <returns>List of textures.</returns>
        protected virtual float[] GetTextureMix(Vector3 worldPos)
        {
            positionOnTerrainX = Mathf.RoundToInt((worldPos.x - terrainPosition.x) / terrain.terrainData.size.x * terrain.terrainData.alphamapWidth);
            positionOnTerrainZ = Mathf.RoundToInt((worldPos.z - terrainPosition.z) / terrain.terrainData.size.z * terrain.terrainData.alphamapHeight);

            splatmapData = terrain.terrainData.GetAlphamaps(positionOnTerrainX, positionOnTerrainZ, 1, 1);

            if (splatmapData == null)
            {
                return null;
            }

            cellMix = new float[splatmapData.GetUpperBound(2) + 1];
	
	        for (int n = 0; n < cellMix.Length; n++)
	        {
		        cellMix[n] = splatmapData[0, 0, n];
	        }
	
	        return cellMix;
        }
    }
}