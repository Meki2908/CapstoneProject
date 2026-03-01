using UnityEngine;

public class HeightmapExporter : MonoBehaviour
{
    public Terrain terrain;

    public void Export()
    {
        if (!terrain) terrain = Terrain.activeTerrain;

        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;

        float[,] heights = td.GetHeights(0, 0, res, res);
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGB24, false);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float h = heights[y, x];
                Color c;

                if (h < 0.3f)
                {
                    c = Color.Lerp(new Color(0.1f, 0.4f, 0.1f), new Color(0.2f, 0.6f, 0.2f), h / 0.3f);
                }
                else if (h < 0.6f)
                {
                    c = Color.Lerp(new Color(0.4f, 0.3f, 0.1f), new Color(0.6f, 0.5f, 0.2f), (h - 0.3f) / 0.3f);
                }
                else
                {
                    c = Color.Lerp(new Color(0.6f, 0.6f, 0.6f), new Color(0.9f, 0.9f, 0.9f), (h - 0.6f) / 0.4f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/HeightMap.png";

        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log("Exported Heightmap to: " + path);
    }
}
