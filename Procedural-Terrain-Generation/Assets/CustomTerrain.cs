using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour
{

    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(1, 1, 1);

    public bool resetTerrain = true;
    
    // Perlin Noise
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    public int perlinOffsetX = 0;
    public int perlinOffsetY = 0;
    public int perlinOctaves = 3;
    public float perlinPersistence = 8;
    public float perlinHeightScale = 0.09f;
    
    // Multiple Perlin Noise
    [System.Serializable]
    public class PerlinParameters
    {
        public float mPerlinXScale = 0.01f;
        public float mPerlinYScale = 0.01f;
        public int mPerlinOctaves = 3;
        public float mPerlinPersistence = 8;
        public float mPerlinHeightScale = 0.09f;
        public int mPerlinOffsetX = 0;
        public int mPerlinOffsetY = 0;
        public bool remove = false;
    }

    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters()
    };
    
    // Voronoi
    public float voronoiFalloff = 0.2f;
    public float voronoiDropoff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.5f;
    public int voronoiPeaks = 5;
    public enum VoronoiType
    {
        Linear = 0, Power = 1, Combined = 2, SinPow = 3
    }
    public VoronoiType voronoiType = VoronoiType.Linear;

    public Terrain terrain;
    public TerrainData terrainData;
    
    // MPD
    public float MPDMinHeight = -2f;
    public float MPDMaxHeight = 2f;
    public float MPDDampening = 2.0f;
    public float MPDRoughness = 2.0f;
    
    // Smooth
    public int smoothIteration = 1;
    
    // Splat Maps
    [System.Serializable]
    public class SplatHeights
    {
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 1.5f;
        public Vector2 tileOffset = new Vector2(0, 0);
        public Vector2 tileSize = new Vector2(50, 50);
        public float splatOffset = 0.1f;
        public float splatNoiseXScale = 0.01f;
        public float splatNoiseYScale = 0.01f;
        public float splatNoiseScaler = 0.1f;
        public bool remove = false;
    }

    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };

    
        
    public void MidpointDisplacement()
    {
        float[,] heightMap = GetHeightMap();
        int width = terrainData.heightmapResolution - 1;
        int squareSize = width;
        float heightMin = MPDMinHeight;
        float heightMax = MPDMaxHeight;
        float heightDampener = (float) Mathf.Pow(MPDDampening, -1 * MPDRoughness);

        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD;

        /*heightMap[0, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[0, terrainData.heightmapResolution - 2] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[terrainData.heightmapResolution - 2, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[terrainData.heightmapResolution - 2, terrainData.heightmapResolution - 2] =
            UnityEngine.Random.Range(0f, 0.2f);*/

        while (squareSize > 0)
        {
            for (int x = 0; x < width; x+=squareSize)
            {
                for (int y = 0; y < width; y+=squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int) (x + squareSize / 2.0f);
                    midY = (int) (y + squareSize / 2.0f);

                    heightMap[midX, midY] = (float) ((heightMap[x, y] + heightMap[cornerX, y] + heightMap[x, cornerY] +
                                                      heightMap[cornerX, cornerY]) / 4.0f +
                                                     UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            for (int x = 0; x < width; x+= squareSize)
            {
                for (int y = 0; y < width; y+= squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int) (x + squareSize / 2.0f);
                    midY = (int) (y + squareSize / 2.0f);

                    pmidXR = (int) (midX + squareSize);
                    pmidYU = (int) (midY + squareSize);
                    pmidXL = (int) (midX - squareSize);
                    pmidYD = (int) (midY - squareSize);

                    // eliminate array out of bound
                    if (pmidXL <= 0 || pmidYD <= 0 || pmidXR >= width - 1 || pmidYU >= width - 1)
                    {
                        continue;
                    }

                    // bottom
                    heightMap[midX, y] = (float) ((heightMap[midX, midY] + heightMap[cornerX, y] + heightMap[midX, pmidYD] +
                                                 heightMap[x, y]) / 4.0f +
                                        UnityEngine.Random.Range(heightMin, heightMax));
                    // left
                    heightMap[x, midY] = (float) ((heightMap[x, cornerY] + heightMap[midX, midY] + heightMap[x,y] +
                                                   heightMap[pmidXL, midY]) / 4.0f +
                                                  UnityEngine.Random.Range(heightMin, heightMax));
                    // top
                    heightMap[midX, cornerY] = (float) ((heightMap[midX, pmidYU] + heightMap[cornerX, cornerY] + heightMap[midX, midY] +
                                                         heightMap[x, cornerY]) / 4.0f +
                                                        UnityEngine.Random.Range(heightMin, heightMax));
                    //right
                    heightMap[cornerX, midY] = (float) ((heightMap[cornerX, cornerY] + heightMap[pmidXR, midY] + heightMap[cornerX, y] +
                                                         heightMap[midX,midY]) / 4.0f +
                                                        UnityEngine.Random.Range(heightMin, heightMax));
                }
            }
            squareSize = (int) (squareSize / 2.0f);
            heightMax *= heightDampener;
            heightMin *= heightDampener;
        }
        terrainData.SetHeights(0,0,heightMap);
    }

    public void Perlin()
    {
        float[,] heightMap = GetHeightMap();

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                heightMap[x, y] += Utils.fBM((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistence) * perlinHeightScale; 
            }
        }
        terrainData.SetHeights(0,0,heightMap);
    }

    public void MultiplePerlinTerrain()
    {
        float[,] heightMap = GetHeightMap();
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                foreach (PerlinParameters p in perlinParameters)
                {
                    heightMap[x, y] += Utils.fBM((x + p.mPerlinOffsetX) * p.mPerlinXScale,
                                           (y + p.mPerlinOffsetY) * p.mPerlinYScale, p.mPerlinOctaves,
                                           p.mPerlinPersistence) *
                                       p.mPerlinHeightScale;
                }
            }
        }
        terrainData.SetHeights(0,0,heightMap);
    }

    public void Voronoi()
    {
        float[,] heightMap = GetHeightMap();
        for (int p = 0; p < voronoiPeaks; p++)
        {
            Vector3 peak = 
                new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                    UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight), UnityEngine.Random.Range(0, terrainData.heightmapResolution));
            if (heightMap[(int) peak.x, (int) peak.z] < peak.y)
            {
                heightMap[(int) peak.x, (int) peak.z] = peak.y;
            }
            else
            {
                continue;
            }
            
            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            
            float maxDistance = Vector2.Distance(new Vector2(0, 0),
                new Vector2(terrainData.heightmapResolution, terrainData.heightmapResolution));
            
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (!(x == peak.x && y == peak.z))
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;
                        if (voronoiType == VoronoiType.Combined)
                        {
                            h = peak.y - distanceToPeak * voronoiFalloff - Mathf.Pow(distanceToPeak, voronoiDropoff);
                        }else if (voronoiType == VoronoiType.Power)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak,voronoiDropoff) * voronoiFalloff;
                        }else if (voronoiType == VoronoiType.SinPow)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak * 3, voronoiFalloff) -
                                Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / voronoiDropoff;
                        }
                        else
                        {
                            h = peak.y - distanceToPeak * voronoiFalloff;
                        }

                        if (heightMap[x, y] < h)
                        {
                            heightMap[x, y] = h;
                        }
                        
                    }
                }
            }
        }
        terrainData.SetHeights(0,0,heightMap);
    }
    
    public void AddNewPerlin()
    {
        perlinParameters.Add(new PerlinParameters());
    }

    public void RemovePerlin()
    {
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();
        for (int i = 0; i < perlinParameters.Count; i++)
        {
            if (!perlinParameters[i].remove)
            {
                keptPerlinParameters.Add(perlinParameters[i]);
            }
        }

        if (keptPerlinParameters.Count==0)
        {
            keptPerlinParameters.Add(perlinParameters[0]);
        }

        perlinParameters = keptPerlinParameters;
    }
    
    public void RandomTerrain()
    {
        float[,] heightMap = GetHeightMap();
        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            {
                heightMap[x, z] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
            }
        }
        terrainData.SetHeights(0,0,heightMap);
    }

    public void LoadTexture()
    {
        float[,] heightMap = GetHeightMap();
        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            {
                heightMap[x, z] +=
                    heightMapImage.GetPixel((int) (x * heightMapScale.x), (int) (z * heightMapScale.z)).grayscale *
                    heightMapScale.y;
            }
        }
        terrainData.SetHeights(0,0,heightMap);
    }

    public void ResetTerrain()
    {
        float[,] heightMap;
        heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            {
                heightMap[x, z] = 0;
            }
        }
        terrainData.SetHeights(0,0,heightMap);
    }
    
    float[,] GetHeightMap()
    {
        if (resetTerrain)
        {
            return new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        }
        else
        {
            return terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        }
    }

    public void AddNewSplatHeight()
    {
        splatHeights.Add(new SplatHeights());
    }

    public void RemoveSplatHeight()
    {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
        for (int i = 0; i < splatHeights.Count; i++)
        {
            if (!splatHeights[i].remove)
            {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }

        if (keptSplatHeights.Count == 0)
        {
            keptSplatHeights.Add(splatHeights[0]);
        }

        splatHeights = keptSplatHeights;
    }

    float GetSteepness(float[,] heightmap, int x, int y, int width, int height)
    {
        float h = heightmap[x, y];
        int nx = x + 1;
        int ny = y + 1;

        if (nx > width - 1)
        {
            nx = x - 1;
        }

        if (ny > height - 1)
        {
            ny = y - 1;
        }

        float dx = heightmap[nx, y] - h;
        float dy = heightmap[x, ny] - h;
        Vector2 gradient = new Vector2(dx, dy);

        float steep = gradient.magnitude;

        return steep;
    }
    
    public void SplatMaps()
    {
        TerrainLayer[] newSplatPrototypes;
        newSplatPrototypes = new TerrainLayer[splatHeights.Count];
        int spindex = 0;
        foreach (SplatHeights sh in splatHeights)
        {
            newSplatPrototypes[spindex] = new TerrainLayer();
            newSplatPrototypes[spindex].diffuseTexture = sh.texture;
            newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
            newSplatPrototypes[spindex].tileSize = sh.tileSize;
            newSplatPrototypes[spindex].diffuseTexture.Apply(true);
            string path = "Assets/New Terrain Layer " + spindex + ".terrainlayer";
            AssetDatabase.CreateAsset(newSplatPrototypes[spindex],path);
            spindex++;
            Selection.activeObject = this.gameObject;
        }

        terrainData.terrainLayers = newSplatPrototypes;

        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,,] splatmapData =
            new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float[] splat = new float[terrainData.alphamapLayers];
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    float noise = Mathf.PerlinNoise(x * splatHeights[i].splatNoiseXScale, y * splatHeights[i].splatNoiseYScale) * splatHeights[i].splatNoiseScaler;
                    float offset = splatHeights[i].splatOffset + noise;
                    float thisHeightStart = splatHeights[i].minHeight - offset;
                    float thisHeightStop = splatHeights[i].maxHeight + offset;
                    //float steepness = GetSteepness(heightMap, x, y, terrainData.heightmapResolution,
                     //   terrainData.heightmapResolution);
                     float steepness = terrainData.GetSteepness(y / (float) terrainData.alphamapHeight,
                         x / (float) terrainData.alphamapWidth);
                    if ((heightMap[x,y] >= thisHeightStart && heightMap[x,y] <= thisHeightStop) && 
                        (steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope))
                    {
                        splat[i] = 1;
                    }
                }

                NormalizeVector(splat);
                for (int j = 0; j < splatHeights.Count; j++)
                {
                    splatmapData[x, y, j] = splat[j];
                }
            }
        }
        terrainData.SetAlphamaps(0,0,splatmapData);
    }
    
    public void Smooth()
    {
        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float smoothProgress = 0;
        EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress);
        for (int iteration = 0; iteration < smoothIteration; iteration++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    float avgHeight = heightMap[x, y];
                    List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y), terrainData.heightmapResolution,
                        terrainData.heightmapResolution);
                    foreach (Vector2 n in neighbours)
                    {
                        avgHeight += heightMap[(int) n.x, (int) n.y];
                    }

                    heightMap[x, y] = avgHeight / ((float) neighbours.Count + 1);
                }
            }

            smoothProgress++;
            EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress/smoothIteration);
        }
        terrainData.SetHeights(0, 0, heightMap);
        EditorUtility.ClearProgressBar();   
    }
    
    List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
    {
        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if (!(x==0 && y==0))
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1),
                        Mathf.Clamp(pos.y + y, 0, height - 1));
                    if (!neighbours.Contains(nPos))
                    {
                        neighbours.Add(nPos);
                    }
                }
            }
        }

        return neighbours;
    }

    void NormalizeVector(float[] v)
    {
        float total = 0;
        for (int i = 0; i < v.Length; i++)
        {
            total += v[i];
        }

        for (int i = 0; i < v.Length; i++)
        {
            v[i] /= total;
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("Initialising Terrain Data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData;
    }

    private void Awake()
    {
        SerializedObject tagManager =
            new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain");
        AddTag(tagsProp, "Cloud");
        AddTag(tagsProp, "Shore");
        
        // apply tag changes to tag database
        tagManager.ApplyModifiedProperties();
        
        // take this object
        this.gameObject.tag = "Terrain";
    }

    void AddTag(SerializedProperty tagsProp, string newTag)
    {
        bool found = false;
        // ensure the tag doesn't already exists
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag))
            {
                found = true;
                break;
            }
        }
        
        // add your new tag
        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
