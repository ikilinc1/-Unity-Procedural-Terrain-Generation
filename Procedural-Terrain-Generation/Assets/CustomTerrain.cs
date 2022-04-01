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
    
    // Vegetation
    [System.Serializable]
    public class Vegetation
    {
        public GameObject mesh = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public float minScale = 0.5f;
        public float maxScale = 1.0f;
        public Color color1 = Color.white;
        public Color color2 = Color.white;
        public Color lightColor = Color.white;
        public float minRotation = 0;
        public float maxRotation = 360;
        public float density = 0.5f;
        public bool remove = false;
    }

    public List<Vegetation> vegetation = new List<Vegetation>()
    {
        new Vegetation()
    };

    public int maxTrees = 5000;
    public int distanceTrees = 5;
    
    // Details
    [System.Serializable]
    public class Detail
    {
        public GameObject prototype = null;
        public Texture2D prototypeTexture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90f;
        public Color healthyColor = Color.white;
        public Color dryColor = Color.white;
        public Vector2 heightRange = new Vector2(1, 1);
        public Vector2 widthRange = new Vector2(1, 1);
        public float noiseSpread = 0.5f;
        public float overlap = 0.01f;
        public float feather = 0.05f;
        public float density = 0.5f;
        public bool remove = false;
    }

    public List<Detail> details = new List<Detail>()
    {
        new Detail()
    };

    public int maxDetails = 5000;
    public int distanceDetail = 5;
    
    // Water
    public float waterHeight = 0.5f;
    public GameObject waterGO;
    public Material shoreLineMaterial;
    
    // Erosion
    public enum ErosionType
    {
        Rain = 0, Thermal = 1, Tidal = 2, River = 3, Wind = 4
    }

    public ErosionType erosionType = ErosionType.Rain;
    public float erosionStrength = 0.1f;
    public float erosionAmount = 0.01f;
    public int springsPerRiver = 5;
    public float solubility = 0.01f;
    public int droplets = 10;
    public int erosionSmoothAmount = 5;

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

    public void AddVegetationData()
    {
        vegetation.Add(new Vegetation());
    }

    public void RemoveVegetationData()
    {
        List<Vegetation> keptTreeData = new List<Vegetation>();
        for (int i = 0; i < vegetation.Count; i++)
        {
            if (!vegetation[i].remove)
            {
                keptTreeData.Add(vegetation[i]);
            }
        }

        if (keptTreeData.Count == 0)
        {
            keptTreeData.Add(vegetation[0]);
        }

        vegetation = keptTreeData;
    }

    public void PlantVegetation()
    {
        TreePrototype[] newTreePrototypes;
        newTreePrototypes = new TreePrototype[vegetation.Count];
        int tindex = 0;
        foreach (Vegetation t in vegetation)
        {
            newTreePrototypes[tindex] = new TreePrototype();
            newTreePrototypes[tindex].prefab = t.mesh;
            tindex++;
        }

        terrainData.treePrototypes = newTreePrototypes;

        List<TreeInstance> allVegetation = new List<TreeInstance>();
        for (int z = 0; z < terrainData.size.z; z+=distanceTrees)
        {
            for (int x = 0; x < terrainData.size.x; x+=distanceTrees)
            {
                for (int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > vegetation[tp].density)
                    {
                        break;
                    }
                    float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
                    float thisHeightStart = vegetation[tp].minHeight;
                    float thisHeightEnd = vegetation[tp].maxHeight;

                    float steepness =
                        terrainData.GetSteepness(x / (float) terrainData.size.x, z / (float) terrainData.size.z);

                    if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) && 
                        (steepness >= vegetation[tp].minSlope && steepness <= vegetation[tp].maxSlope))
                    {
                        TreeInstance instance = new TreeInstance();
                        instance.position = new Vector3((x + UnityEngine.Random.Range(-5.0f, 5.0f)) / 
                                                        terrainData.size.x, thisHeight, 
                            (z + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.z);

                        Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
                            instance.position.y * terrainData.size.y,
                            instance.position.z * terrainData.size.z) + this.transform.position;

                        RaycastHit hit;
                        int layerMask = 1 << terrainLayer;
                        if (Physics.Raycast(treeWorldPos + new Vector3(0, 10, 0), -Vector3.up, out hit, 100, layerMask) ||
                            Physics.Raycast(treeWorldPos - new Vector3(0, 10, 0), Vector3.up, out hit, 100, layerMask))
                        {
                            float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                            instance.position = new Vector3(instance.position.x, treeHeight, instance.position.z);
                            instance.rotation = UnityEngine.Random.Range(vegetation[tp].minRotation, vegetation[tp].maxRotation);
                            instance.prototypeIndex = tp;
                            instance.color = Color.Lerp(vegetation[tp].color1, vegetation[tp].color2,
                                UnityEngine.Random.Range(0.0f, 1.0f));
                            instance.lightmapColor = vegetation[tp].lightColor;
                            
                            float randomScaleValue = UnityEngine.Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);
                            instance.heightScale = randomScaleValue;
                            instance.widthScale = randomScaleValue;
                    
                            allVegetation.Add(instance);
                            if (allVegetation.Count >= maxTrees)
                            {
                                goto TREESDONE;
                            }
                        }
                    }
                }
            }
        }
        TREESDONE:
            terrainData.treeInstances = allVegetation.ToArray();
    }

    public void AddNewDetails()
    {
        details.Add(new Detail());
    }

    public void RemoveDetails()
    {
        List<Detail> keptDetails = new List<Detail>();
        for (int i = 0; i < details.Count; i++)
        {
            if (!details[i].remove)
            {
                keptDetails.Add(details[i]);
            }
        }

        if (keptDetails.Count == 0)
        {
            keptDetails.Add(details[0]);
        }

        details = keptDetails;
    }

    public void AddDetails()
    {
        DetailPrototype[] newDetailPrototypes;
        newDetailPrototypes = new DetailPrototype[details.Count];
        int dindex = 0;
        foreach (Detail d in details)
        {
            newDetailPrototypes[dindex] = new DetailPrototype();
            newDetailPrototypes[dindex].prototype = d.prototype;
            newDetailPrototypes[dindex].prototypeTexture = d.prototypeTexture;
            newDetailPrototypes[dindex].healthyColor = d.healthyColor;
            newDetailPrototypes[dindex].dryColor = d.dryColor;
            newDetailPrototypes[dindex].minHeight = d.heightRange.x;
            newDetailPrototypes[dindex].maxHeight = d.heightRange.y;
            newDetailPrototypes[dindex].minWidth = d.widthRange.x;
            newDetailPrototypes[dindex].maxWidth = d.widthRange.y;
            newDetailPrototypes[dindex].noiseSpread = d.noiseSpread;
            if (newDetailPrototypes[dindex].prototype)
            {
                newDetailPrototypes[dindex].usePrototypeMesh = true;
                newDetailPrototypes[dindex].renderMode = DetailRenderMode.VertexLit;
            }
            else
            {
                newDetailPrototypes[dindex].usePrototypeMesh = false;
                newDetailPrototypes[dindex].renderMode = DetailRenderMode.GrassBillboard;
            }

            dindex++;
        }
        terrainData.detailPrototypes = newDetailPrototypes;

        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];
            for (int y = 0; y < terrainData.detailHeight; y+=distanceDetail)
            {
                for (int x = 0; x < terrainData.detailWidth; x+=distanceDetail)
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > details[i].density)
                    {
                        continue;
                    }

                    int xHM = (int) (x / (float) terrainData.detailWidth * terrainData.heightmapResolution);
                    int yHM = (int) (y / (float) terrainData.detailHeight * terrainData.heightmapResolution);

                    float thisNoise = Utils.Map(Mathf.PerlinNoise(x * details[i].feather, y * details[i].feather), 0, 1,
                        0.5f, 1);

                    float thisHeightStart = details[i].minHeight * thisNoise - details[i].overlap * thisNoise; 
                    float thisHeightEnd = details[i].maxHeight * thisNoise + details[i].overlap * thisNoise;

                    float thisHeight = heightMap[yHM, xHM];
                    float steepness = terrainData.GetSteepness(xHM / (float) terrainData.size.x,
                        yHM / (float) terrainData.size.z);
                    if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) &&
                        (steepness >= details[i].minSlope && steepness <= details[i].maxSlope))
                    {
                        detailMap[y, x] = 1;
                    }
                }
            }
            terrainData.SetDetailLayer(0,0,i,detailMap);
        }
    }

    public void AddWater()
    {
        GameObject water = GameObject.Find("water");
        if (!water)
        {
            water = Instantiate(waterGO, this.transform.position, this.transform.rotation);
            water.name = "water";
        }

        water.transform.position = this.transform.position + new Vector3(terrainData.size.x / 2,
            waterHeight * terrainData.size.y, terrainData.size.z / 2);
        water.transform.localScale = new Vector3(terrainData.size.x, 1, terrainData.size.z);
    }

    public void DrawShoreLine()
    {
        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                // find spot on shore
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapResolution,
                    terrainData.heightmapResolution);

                foreach (Vector2 n in neighbours)
                {
                    if (heightMap[x,y] < waterHeight && heightMap[(int)n.x,(int)n.y] > waterHeight)
                    {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        go.transform.localScale *= 10.0f;

                        go.transform.position = this.transform.position +
                                                new Vector3(
                                                    y / (float) terrainData.heightmapResolution *
                                                    terrainData.size.z, waterHeight * terrainData.size.y,
                                                    x / (float) terrainData.heightmapResolution *
                                                    terrainData.size.x);

                        go.transform.LookAt(new Vector3(
                            n.y / (float) terrainData.heightmapResolution * terrainData.size.z,
                            waterHeight * terrainData.size.y,
                            n.x / (float) terrainData.heightmapResolution * terrainData.size.x));
                        
                        go.transform.Rotate(90,0,0);
                        go.tag = "Shore";
                    }
                }
            }
        }

        GameObject[] shoreQuads = GameObject.FindGameObjectsWithTag("Shore");
        MeshFilter[] meshFilters = new MeshFilter[shoreQuads.Length];
        for (int m = 0; m < shoreQuads.Length; m++)
        {
            meshFilters[m] = shoreQuads[m].GetComponent<MeshFilter>();
        }

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i<meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            i++;
        }
        
        GameObject currentShoreLine = GameObject.Find("Shoreline");
        if (currentShoreLine)
        {
            DestroyImmediate(currentShoreLine);
        }

        GameObject shoreLine = new GameObject();
        shoreLine.name = "ShoreLine";
        shoreLine.AddComponent<WaveAnimation>();
        shoreLine.transform.position = this.transform.position;
        shoreLine.transform.rotation = this.transform.rotation;
        MeshFilter thisMF = shoreLine.AddComponent<MeshFilter>();
        thisMF.mesh = new Mesh();
        shoreLine.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);

        MeshRenderer r = shoreLine.AddComponent<MeshRenderer>();
        r.sharedMaterial = shoreLineMaterial;

        for (int sQ = 0; sQ < shoreQuads.Length; sQ++)
        {
            DestroyImmediate(shoreQuads[sQ]);
        }
    }

    public void Erode()
    {
        if (erosionType == ErosionType.Rain)
        {
            Rain();
        }
        else if (erosionType == ErosionType.Tidal)
        {
            Tidal();
        }
        else if (erosionType == ErosionType.Thermal)
        {
            Thermal();
        }
        else if (erosionType == ErosionType.River)
        {
            River();
        }
        else if (erosionType == ErosionType.Wind)
        {
            Wind();
        }

        smoothIteration = erosionSmoothAmount;
         Smooth();
        
    }

    public void Rain()
    {
        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        for (int i = 0; i < droplets; i++)
        {
            heightMap[UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                UnityEngine.Random.Range(0, terrainData.heightmapResolution)] -= erosionStrength;
        }
        
        terrainData.SetHeights(0,0,heightMap);
    }

    public void Tidal()
    {
        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neightbours = GenerateNeighbours(thisLocation, terrainData.heightmapResolution,
                    terrainData.heightmapResolution);

                foreach (Vector2 n in neightbours)
                {
                    if (heightMap[x,y] < waterHeight && heightMap[(int) n.x,(int) n.y] > waterHeight)
                    {
                        heightMap[x, y] = waterHeight;
                        heightMap[(int) n.x, (int) n.y] = waterHeight;
                    }
                }
            }
        }
        
        terrainData.SetHeights(0,0,heightMap);
    }

    public void Thermal()
    {
        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neightbours = GenerateNeighbours(thisLocation, terrainData.heightmapResolution,
                    terrainData.heightmapResolution);

                foreach (Vector2 n in neightbours)
                {
                    if (heightMap[x,y] > heightMap[(int) n.x,(int) n.y] + erosionStrength)
                    {
                        float currentHeight = heightMap[x, y];
                        heightMap[x, y] -= currentHeight * erosionAmount;
                        heightMap[(int) n.x, (int) n.y] += currentHeight * erosionAmount;
                    }
                }
            }
        }
        
        terrainData.SetHeights(0,0,heightMap);
    }

    public void River()
    {
        float[,] heightMap =
            terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,] erosionMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (int i = 0; i < droplets; i++)
        {
            Vector2 dropletPosition = new Vector2(UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                UnityEngine.Random.Range(0, terrainData.heightmapResolution));
            erosionMap[(int) dropletPosition.x, (int) dropletPosition.y] = erosionStrength;
            for (int j = 0; j < springsPerRiver; j++)
            {
                erosionMap = RunRiver(dropletPosition, heightMap, erosionMap, terrainData.heightmapResolution,
                    terrainData.heightmapResolution);
            }
        }

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                if (erosionMap[x,y] > 0)
                {
                    heightMap[x, y] -= erosionMap[x, y];
                }
            }
        }
        
        terrainData.SetHeights(0,0,heightMap);
    }

    float[,] RunRiver(Vector3 dropletPosition, float[,] heightMap, float[,] erosionMap, int width, int height)
    {
        while (erosionMap[(int) dropletPosition.x, (int) dropletPosition.y] > 0)
        {
            List<Vector2> neighbours = GenerateNeighbours(dropletPosition, width, height);
            neighbours.Shuffle();
            bool foundLower = false;
            foreach (Vector2 n in neighbours)
            {
                if (heightMap[(int) n.x, (int) n.y] < heightMap[(int) dropletPosition.x, (int) dropletPosition.y])
                {
                    erosionMap[(int) n.x, (int) n.y] = erosionMap[(int) dropletPosition.x, (int) dropletPosition.y];
                    dropletPosition = n;
                    foundLower = true;
                    break;
                }
            }

            if (!foundLower)
            {
                erosionMap[(int) dropletPosition.x, (int) dropletPosition.y] -= solubility;
            }
        }

        return erosionMap;
    }

    public void Wind(){}

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

    public enum TagType {Tag = 0, Layer = 1}
    [SerializeField] 
    private int terrainLayer = -1;
    private void Awake()
    {
        SerializedObject tagManager =
            new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain" ,TagType.Tag);
        AddTag(tagsProp, "Cloud",TagType.Tag);
        AddTag(tagsProp, "Shore",TagType.Tag);
        tagManager.ApplyModifiedProperties();

        SerializedProperty layerProp = tagManager.FindProperty("layers");
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties();
        
        // take this object
        this.gameObject.tag = "Terrain";
        this.gameObject.layer = terrainLayer;
    }

    int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
    {
        bool found = false;
        // ensure the tag doesn't already exists
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag))
            {
                found = true;
                return i;
            }
        }
        
        // add your new tag
        if (!found && tType == TagType.Tag)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
        // add your new layer
        else if (!found && tType == TagType.Layer)
        {
            for (int j = 8; j < tagsProp.arraySize; j++)
            {
                SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
                // add layer in next empty slot
                if (newLayer.stringValue == "")
                {
                    newLayer.stringValue = newTag;
                    return j;
                }
            }
        }

        return -1;
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
