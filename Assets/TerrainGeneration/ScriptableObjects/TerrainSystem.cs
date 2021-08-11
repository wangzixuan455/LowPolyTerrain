using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainSystem", menuName = "Terrain System", order = 3)]
public class TerrainSystem : ScriptableObject {

    public string TerName;
    [HideInInspector]
    public TerrainGenerator Generator;
    [HideInInspector]
    public Transform Terrain;

    public Vector2 Size = new Vector2(500f, 500f);
    public Vector2i Resolution = new Vector2i(100, 100);

    [HideInInspector]
    public Mesh MeshWorld;
    [HideInInspector]
    public Vertex[] Vertices;
    public Vector2 VertexDistance;//最小单位格子长度

    [HideInInspector]
    public Vector3[] VertexData;//Mesh顶点数据，三角形为一个单位
    [HideInInspector]
    public int[] TriangleData;//Mesh三角形数据（方形格2倍）
    [HideInInspector]
    public Vector2[] UVData;//MeshUV数据（顶点数）

    public Vector2[] _uv1;//ColorUV  最小单位：最小单位格的一个三角形（顶点数）
    public Vector2[] _uv2;//地形信息UV 跟Size的单位格数目一致（顶点数）

    public float[] HeightMap;//高度信息（顶点数）

    public int[] IndexMap;//地形信息的索引（size方形格数）

    [HideInInspector]
    public Material Material;
    public Texture2D Texture;
    public Texture2D InfoTexture;
    public Texture2D MainTexture;

    public FilterMode FilterMode = FilterMode.Trilinear;
    public int ChunkSize = 6000;

    public bool isInitialized = false;
    [HideInInspector]
    public Color[] ColorSet;
    private Color[] ColorCubeMap;//最小单位格

    private Dictionary<Color, Vector2> ColorsUv = new Dictionary<Color, Vector2>();
    [HideInInspector]
    public Dictionary<int, Vector2> InfoUv = new Dictionary<int, Vector2>();
    public Dictionary<string, List<Vector3>> _biological = new Dictionary<string, List<Vector3>>();
    public Dictionary<int, GameObject> bioGames = new Dictionary<int, GameObject>();
    public int[] bioIndex;
    private GameObject[] games;
    public void ManageBioGames(GameObject gameObject)
    {
        if (!gameObject || bioGames.ContainsValue(gameObject))
            return;
        int len = bioGames.Count + 1;//从1开始，数组默认0
        bioGames.Add(len, gameObject);
    }
    public void ReadColor(int w, int h)
    {
        float pexlX = Texture.width / w;
        float pexlY = Texture.height / h;
        ColorSet = new Color[w * h];
        ColorsUv.Clear();
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                ColorSet[i * w + j] = Texture.GetPixel((int)(pexlX * (j + 0.5)), (int)(pexlY * (i + 0.5)));
                if (ColorsUv.ContainsKey(ColorSet[i * w + j]))
                    continue;
                ColorsUv.Add(ColorSet[i * w + j], new Vector2(j / (float)w + 0.0001f, (float)i / h + 0.0001f));
            }
        }
    }
    public void ReadInfo(int w, int h)
    {
        float pexlX = InfoTexture.width / w;
        float pexlY = InfoTexture.height / h;
        InfoUv.Clear();
        int index = 1;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                if (InfoUv.ContainsKey(index))
                    continue;
                InfoUv.Add(index, new Vector2(j / (float)w, (float)i / h));
                index++;
            }
        }
    }
    public TerrainSystem Initialise(TerrainGenerator genertor) {//初建Mesh
        Generator = genertor;
        Terrain = new GameObject("Terrain").transform;
        Terrain.SetParent(Generator.transform);
        if (!isInitialized)
        {
            isInitialized = true;
            Reinitialise();
        }
        else if (HeightMap.Length == 0 || _uv1.Length == 0 || _uv2.Length == 0)
            Reinitialise();
        else
            CreateTerrain();
        return this;
    }

    public void Reinitialise()//清空数据
    {
        HeightMap = new float[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        _uv1 = new Vector2[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        _uv2 = new Vector2[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        CreateTerrain();
    }
    public void ReHeight()//清空数据
    {
        HeightMap = new float[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        CreateTerrain();
    }
    public void ReUV1()
    {
        _uv1 = new Vector2[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        CreateTerrain();
    }
    public void ReUV2()
    {
        _uv2 = new Vector2[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        CreateTerrain();
    }
    public void SetSize(Vector2 size) {
        if (Size != size) {
            Size = size;
            int index = 0;
            for (int y = 0; y < Resolution.y - 1; y++) {
                for (int x = 0; x < Resolution.x - 1; x++) {
                    Vector2 position = GridToWorld(x, y);
                    Vertices[index].SetPosition(position.x, position.y, this);
                    index++;
                    Vector2 position1 = GridToWorld(x, y + 1);
                    Vertices[index].SetPosition(position1.x, position1.y, this);
                    index++;
                    Vector2 position2 = GridToWorld(x + 1, y);
                    Vertices[index].SetPosition(position2.x, position2.y, this);
                    index++;
                    Vector2 position3 = GridToWorld(x + 1, y + 1);
                    Vertices[index].SetPosition(position3.x, position3.y, this);
                    index++;
                }
            }
        }
    }

    public void SetResolution(Vector2i resolution) {
        resolution.x = Mathf.Max(resolution.x, 2);
        resolution.y = Mathf.Max(resolution.y, 2);
        if (Resolution.x != resolution.x || Resolution.y != resolution.y) {
            Resolution = resolution;
            CreateTerrain();
        }
    }

    public void Update() {
        if (Terrain == null)
        {
            //Reinitialise();
        }
        Terrain.localPosition = Vector3.zero;
        Terrain.localRotation = Quaternion.identity;
        for (int i = 0; i < Terrain.childCount; i++) {
            Terrain.GetChild(i).localPosition = Vector3.zero;
            Terrain.GetChild(i).localRotation = Quaternion.identity;
        }
    }

    void UpdateHeightCube(int a, float h)
    {        
        HeightMap[a] += h;
        VertexData[a].y = HeightMap[a];
    }
    public void ModifyTerrain(Vector2 world, float size, float strength, ToolType tool) {
        if (world.x < VertexData[0].x || world.y < VertexData[0].z || world.x > VertexData[VertexData.Length - 1].x || world.y > VertexData[VertexData.Length - 1].z)
            return;
        float sqrSize = size * size;
        float startX = world.x - size < VertexData[0].x ? VertexData[0].x : world.x - size;
        float startY = world.y - size > VertexData[0].z ? VertexData[0].z : world.y - size;

        for (float y = startY; y <= world.y + size; y += VertexDistance.y) {
            for (float x = startX; x <= world.x + size; x += VertexDistance.x) {
                float sqrDist = (world.x - x) * (world.x - x) + (world.y - y) * (world.y - y);
                if (sqrDist <= sqrSize) {
                    int CubeInd = GetCubeIndex(new Vector2(x, y));
                    List<int> indexCube = GetIndex(CubeInd);
                    for(int i = 0;i< indexCube.Count; i++)
                    {
                        if (indexCube[i] >= 0 && indexCube[i] < VertexData.Length)
                        {
                            switch (tool)
                            {
                                case ToolType.Normal:
                                    UpdateHeightCube(indexCube[i], strength);
                                    break;
                                case ToolType.Bumps:
                                    UpdateHeightCube(indexCube[i], -strength);
                                    break;
                            }
                                              
                        }
                    }
                }
            }
        }
        if (!MeshWorld)
        {
            MeshWorld = Terrain.GetComponentInChildren<MeshFilter>().sharedMesh;
        }
        MeshWorld.vertices = VertexData;
        Vector3[] nor = CaluNormal();
        MeshWorld.normals = nor;
    }

    public void ModifyTexture(Vector2 world, float size, Color color,bool left) {
        if (world.x < VertexData[0].x || world.y < VertexData[0].z || world.x > VertexData[VertexData.Length - 1].x || world.y > VertexData[VertexData.Length - 1].z)
            return;
        float sqrSize = size * size;
        int draw_left = left ? 1 : 0;
        float startX = world.x - size < VertexData[0].x ? VertexData[0].x : world.x - size;
        float startY = world.y - size > VertexData[0].z ? VertexData[0].z : world.y - size;
        for (float y = startY; y <= world.y + size; y += VertexDistance.y)
        {
            for (float x = startX; x <= world.x + size; x += VertexDistance.x)
            {
                float sqrDist = (world.x - x) * (world.x - x) + (world.y - y) * (world.y - y);
                if (sqrDist <= sqrSize)
                {
                    int MapIndex = GetCubeIndex(new Vector2(x,y));
                    int ansIndex = MapIndex * 2 + draw_left;
                    if (ansIndex >= 0 && ansIndex < ColorCubeMap.Length)
                    {
                        ColorCubeMap[ansIndex] = color;
                    }
                }
            }
        }
        SetColorUv();
        MeshWorld.uv2 = _uv1;
        MeshWorld.uv3 = _uv2;
    }
    public void SetColorCubeMap(Color color,int MapIndex, float size)
    {
        ColorCubeMap[MapIndex] = color;
        int StarX0 = (Mathf.CeilToInt(MapIndex / (Resolution.x - 1)) - 1) * (Resolution.x - 1);
        float size1 = 0;
        float size01 = size / 2;
        float size02 = size / 2;
        float inx = Mathf.Floor(MapIndex - (Resolution.x - 1) - size / 2);
        //int StarX =Mathf.CeilToInt(MapIndex / (Resolution.x - 1)) * (Resolution.x - 1);
        int EndX1 = StarX0 + (Resolution.x - 1);
        if (inx < StarX0)
        {
            size01 = size / 2 + MapIndex - StarX0;
            inx = StarX0;
        }
        //int a = (Resolution.x - 1) * Mathf.FloorToInt(MapIndex / (Resolution.x - 1));

        if ((MapIndex - Resolution.x + 1) + size > EndX1)
        {
            size02 = EndX1 - MapIndex + size / 2;
        }
        size1 = size01 + size02;
        for (int i = 0; i < size1; i++)
        {
            for (int j = 0; j < size1; j++)
            {
                ColorCubeMap[(int)inx + j] = color;
            }
            inx += Resolution.x - 1;
        }
    }
    
    public void ModifyInfo(Vector2 world, float size, int indexInfo)
    {
        if (world.x < VertexData[0].x || world.y < VertexData[0].z || world.x > VertexData[VertexData.Length - 1].x || world.y > VertexData[VertexData.Length - 1].z)
            return;
        float startX = world.x - size / 2f < VertexData[0].x ? VertexData[0].x : world.x - size / 2f;
        float startY = world.y - size / 2f > VertexData[0].z ? VertexData[0].z : world.y - size / 2f;
        float sqrSize = size * size;
        for (float y = startY; y <= world.y + size / 2f; y += VertexDistance.y)
        {
            for (float x = startX; x <= world.x + size / 2f; x += VertexDistance.x)
            {
                float sqrDist = (world.x - x) * (world.x - x) + (world.y - y) * (world.y - y);
                if (sqrDist <= sqrSize)
                {
                    int MapIndex = GetCubeIndex(world);
                    if (MapIndex >= 0 && MapIndex < ColorCubeMap.Length)
                        IndexMap[MapIndex] = indexInfo;
                }
            }
        }
        SetUvInfo();
        MeshWorld.uv2 = _uv1;
        MeshWorld.uv3 = _uv2;
    }

    public void ModifyDiolog(Vector2 world, float size, int obj)
    {
        if (world.x < VertexData[0].x || world.y < VertexData[0].z || world.x > VertexData[VertexData.Length - 1].x || world.y > VertexData[VertexData.Length - 1].z)
            return;
        float startX = world.x - size / 2f < VertexData[0].x ? VertexData[0].x : world.x - size / 2f;
        float startY = world.y - size / 2f > VertexData[0].z ? VertexData[0].z : world.y - size / 2f;
        float sqrSize = size * size;
        for (float y = startY; y <= world.y + size / 2f; y += VertexDistance.y)
        {
            for (float x = startX; x <= world.x + size / 2f; x += VertexDistance.x)
            {
                float sqrDist = (world.x - x) * (world.x - x) + (world.y - y) * (world.y - y);
                if (sqrDist <= sqrSize)
                {
                    int MapIndex = GetCubeIndex(world);
                    if (MapIndex >= 0 && MapIndex < ColorCubeMap.Length)
                        bioIndex[MapIndex] = obj;
                }
            }
        }
        CreateBios();
    }
    public void CreateBios()
    {
        for (int y = 0; y < Resolution.y - 1; y++)
        {
            for (int x = 0; x < Resolution.x - 1; x++)
            {
                int inx = x + y * (Resolution.y - 1);
                if (bioIndex[inx] == 0)
                {
                    if (games.Length != 0 && games[inx])
                    {
                        DestroyImmediate(games[inx]);
                        games[inx] = null;
                    }
                    continue;
                }
                else if (!bioGames.ContainsKey(bioIndex[inx]) || (bioIndex[inx] != 0 && bioGames.ContainsKey(bioIndex[inx]) && games[inx] == bioGames[bioIndex[inx]]))
                {
                    continue;
                }
                else
                {
                    if (games.Length != 0 && games[inx])
                    {
                        DestroyImmediate(games[inx]);
                        games[inx] = null;
                    }
                    GameObject obj = bioGames[bioIndex[inx]];
                    Vector3 vec = new Vector3((float)((VertexData[inx * 4].x + VertexData[(inx * 4) + 2].x) * 0.5), (float)((VertexData[inx * 4].z + VertexData[inx * 4 + 1].z) * 0.5), (float)((VertexData[inx * 4].y + VertexData[inx * 4 + 1].y) * 0.5));
                    games[inx] = PrefabUtility.InstantiatePrefab(obj) as GameObject;
                    games[inx].transform.position = vec;
                    games[inx].transform.SetParent(Terrain);
                }
            }
        }

    }


    public Vector2i GetCoordinates(float worldX, float worldY)
    {
        int x = Mathf.RoundToInt((worldX + Size.x / 2f) / Size.x * Resolution.x);
        int y = Mathf.RoundToInt((worldY + Size.y / 2f) / Size.y * Resolution.y);
        return new Vector2i(x, y);
    }

    public Vector2 GetGrid(float worldX, float worldY)
    {
        Vector2i ans = GetCoordinates(worldX, worldY);
        Vector2 vector2 = new Vector2(ans.x, ans.y);
        return vector2;
    }
    public int GetCubeIndex(Vector2 pos)
    {
        Vector2 vector = GetGrid(pos.x, pos.y);
        vector = new Vector2(vector.x - 1, vector.y - 1);
        return (int)(vector.x + vector.y * (Resolution.x - 1));

    }


    public List<int> GetIndex(int a)// a为原格,
    {
        List<int> ans = new List<int>();
        //int[] ans = new int[8];
        int[] num = new int[8];
        if(a % 2 == 0) // 双数开始
        {
            num[0] = a * 6 + 2;
            num[1] = num[0] + 3;
            num[2] = num[1] + 2;
            num[3] = num[2] + 3;
            num[4] = num[1] + (Resolution.x - 1) * 6 - 3;
            num[5] = num[4] + 1;
            num[6] = num[5] + 3;
            num[7] = num[6] + 3;
        }
        else //单数开始
        {
            num[0] = a * 6 + 5;
            num[1] = num[0] + 2;
            num[2] = num[0] + (Resolution.x - 1) * 6 - 1;
            num[3] = num[2] + 2;
            num[4] = -1;
            num[5] = -1;
            num[6] = -1;
            num[7] = -1;
        }   
        for (int i = 0; i < 8; i++)
        {
            if((num[i] < VertexData.Length) && (num[i] >= 0))
            {
                ans.Add(num[i]);
            }         
        }
        return ans;
    }

    public Vector2 GridToWorld(int gridX, int gridY) {
        return new Vector2(Size.x * (float)gridX / ((float)Resolution.x - 1f) - Size.x / 2f, Size.y * (float)gridY / ((float)Resolution.y - 1) - Size.y / 2f);
    }

    public int GridToArray(int gridX, int gridY) {
        return gridY * (Resolution.x - 1) + gridX;
    }

    public void SetHeightMap(float[] heightMap) {
        for (int i = 0; i < Vertices.Length; i++) {
            if (heightMap[i] < -1)
                heightMap[i] = -1;

            Vertices[i].SetHeight(heightMap[i], this);
        }
    }

    //public void SetColorMap(Color[] colorMap) {
    //	for(int i=0; i<Vertices.Length; i++) {
    //		Vertices[i].SetColor(colorMap[i], this);
    //	}
    //}

    //public float[] CreateHeightMap(int seed, float scale, int octaves, float persistance, float lacunarity, float falloffStrength, float falloffRamp, float falloffRange, Vector2 offset, float heightMultiplier, AnimationCurve heightCurve) {
    //	float[] heightMap = new float[(Resolution.x - 1)*(Resolution.y - 1) * 4];

    //	Vector2[] octaveOffsets = new Vector2[octaves];

    //	Random.InitState(seed);

    //	for(int i=0; i<octaves; i++) {
    //		float offsetX = Random.Range(-100f, 100f);
    //		float offsetY = Random.Range(-100f, 100f);
    //		octaveOffsets[i] = new Vector2(offsetX, offsetY);
    //	}

    //	float maxLocalNoiseHeight = float.MinValue;
    //	float minLocalNoiseHeight = float.MaxValue;
    //	for(int y=0; y<Resolution.y; y++) {
    //		for(int x=0; x<Resolution.x; x++) {
    //			float amplitude = 1f;
    //			float frequency = 1f;
    //			float noiseHeight = 0;

    //			for(int i=0; i<octaves; i++) {
    //				float xPos = (((float)x+offset.x) - (float)Resolution.x / 2f) / (float)Resolution.x;
    //				float yPos = (((float)y+offset.y) - (float)Resolution.y / 2f) / (float)Resolution.y;
    //				float sampleX = frequency * scale * xPos + octaveOffsets[i].y;
    //				float sampleY = frequency * scale * yPos + octaveOffsets[i].y;

    //				float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
    //				noiseHeight += perlinValue * amplitude;

    //				amplitude *= persistance;
    //				frequency *= lacunarity;
    //			}

    //			maxLocalNoiseHeight = Mathf.Max(maxLocalNoiseHeight, noiseHeight);
    //			minLocalNoiseHeight = Mathf.Min(minLocalNoiseHeight, noiseHeight);

    //			heightMap[GridToArray(x,y)] = noiseHeight;
    //		}
    //	}

    //	for(int y=0; y<Resolution.y; y++) {
    //		for(int x=0; x<Resolution.x; x++) {
    //			float value = Mathf.Max(Mathf.Abs(x / (float)Resolution.x * 2f - 1f), Mathf.Abs(y / (float)Resolution.y * 2f - 1f));
    //			float a = Mathf.Pow(value, falloffRamp);
    //			float b = Mathf.Pow(falloffRange - falloffRange * value, falloffRamp);
    //			float falloff = 1f - (a+b != 0f ? falloffStrength * a / (a + b) : 0f);
    //			heightMap[GridToArray(x,y)] = heightMultiplier * heightCurve.Evaluate(falloff * Utility.Normalise(heightMap[GridToArray(x,y)], minLocalNoiseHeight, maxLocalNoiseHeight, 0f, 1f));
    //		}
    //	}

    //	return heightMap;
    //}

    //public Color[] CreateColorMap() {
    //	Color[] colorMap = new Color[(Resolution.x - 1) * (Resolution.y - 1) * 4];
    //	for(int y=0; y<Resolution.y; y++) {
    //		for(int x=0; x<Resolution.x; x++) {
    //			float height = GetVertex(x,y)[0].GetHeight(this) / Generator.HeightMultiplier;
    //			int index = GetBiomeIndex(height);
    //			if(index != -1) {
    //				Color color, colorPrevious, colorNext;
    //				color = Biomes[index].Color;
    //				if(index > 0) {
    //					colorPrevious = Biomes[index-1].Color;
    //				} else {
    //					colorPrevious = Biomes[index].Color;
    //				}
    //				if(index < Biomes.Length-1) {
    //					colorNext = Biomes[index+1].Color;
    //				} else {
    //					colorNext = Biomes[index].Color;
    //				}
    //				float distPrevious = Interpolation * (1f - (height-Biomes[index].StartHeight) / (Biomes[index].EndHeight - Biomes[index].StartHeight));
    //				float distNext = Interpolation * (1f - (Biomes[index].EndHeight - height) / (Biomes[index].EndHeight - Biomes[index].StartHeight));
    //				color = Color.Lerp(Color.Lerp(color, colorPrevious, distPrevious), Color.Lerp(color, colorNext, distNext), 0.5f);
    //				colorMap[GridToArray(x,y)] = color;
    //			} else {
    //				colorMap[GridToArray(x,y)] = Color.white;
    //			}
    //		}
    //	}
    //	return colorMap;
    //}

    //public void SetBiomeStartHeight(int index, float value) {
    //	if(index > 0) {
    //		Biomes[index].StartHeight = Mathf.Max(Biomes[index-1].StartHeight, value);
    //		Biomes[index-1].EndHeight = Biomes[index].StartHeight;
    //	} else {
    //		Biomes[index].StartHeight = 0f;
    //	}
    //}

    //public void SetBiomeEndHeight(int index, float value) {
    //	if(index < Biomes.Length-1) {
    //		Biomes[index].EndHeight = Mathf.Min(Biomes[index+1].EndHeight, value);
    //		Biomes[index+1].StartHeight = Biomes[index].EndHeight;
    //	} else {
    //		Biomes[index].EndHeight = 1f;
    //	}
    //}

    //public void SetBiomeColor(int index, Color color) {
    //	Biomes[index].Color = color;
    //}

    //public int GetBiomeIndex(float height) {
    //	for(int i=0; i<Biomes.Length; i++) {
    //		if(Biomes[i].StartHeight <= height && Biomes[i].EndHeight >= height) {
    //			return i;
    //		}
    //	}
    //	return -1;
    //}

    private void CreateTerrain() {

        if (HeightMap.Length != ((Resolution.x - 1) * (Resolution.y - 1) * 6))
        {
            HeightMap = new float[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        }
        if (_uv1.Length != ((Resolution.x - 1) * (Resolution.y - 1) * 6))
        {
            _uv1 = new Vector2[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        }
        if (_uv2.Length != ((Resolution.x - 1) * (Resolution.y - 1) * 6))
        {
            _uv2 = new Vector2[(Resolution.x - 1) * (Resolution.y - 1) * 6];
        }
        //Clean up
        while (Terrain.childCount > 0) {
            DestroyImmediate(Terrain.GetChild(0).gameObject);
        }
        DestroyImmediate(Material);
        Resources.UnloadUnusedAssets();

        //Allocate memory
        int Rx = (Resolution.x - 1) * (Resolution.y - 1);
        int Sx = (int)((Size.x - 1) * (Size.y - 1));
        ColorCubeMap = new Color[Rx * 2];
        IndexMap = new int[Sx];//Size大小
        //Vertices = new Vertex[Rx * 6];
        VertexData = new Vector3[Rx * 6];
        TriangleData = new int[Rx * 6];
        UVData = new Vector2[Rx * 6];
        if (bioIndex.Length != Rx)
        {
            bioIndex = new int[Rx];
            if (games != null)
            {
                for (int i = 0; i < games.Length; i++)
                {
                    if (games[i])
                        DestroyImmediate(games[i]);
                }
            }           
            games = new GameObject[Rx];
        }
        if (games == null || games.Length != Rx)
        {
            games = new GameObject[Rx];
        }
		//Calculate vertex distance
		VertexDistance = new Vector2(Size.x/(float)Resolution.x, Size.y/(float)Resolution.y);

        int index = 0;
        int triangleIndex = 0;
        int uvIndex = 0;
        for (int y = 0; y < Resolution.y - 1; y++) {
			for(int x = 0; x < Resolution.x - 1; x++) {
                SetVerticsQuad(x, y,ref index,ref HeightMap);
                SetTriggleQuad(x, y, ref triangleIndex);
                SetUVQuad(x, y, ref uvIndex);
            }
		}        
       // SetUvInfo();
       // CreateBios();
        //Apply flat shading
        //Utility.FlatShading(ref VertexData, ref TriangleData, ref UVData);
		//Create meshes
        Vector3[] nor = CaluNormal();
        MeshWorld = Utility.CreateMeshes(VertexData, TriangleData, UVData, ChunkSize,_uv1,_uv2,nor);
		Material = new Material(Shader.Find("Unlit/test"));
        Material.SetTexture("_ColTex", Texture);
        Material.SetTexture("_InfoTex", InfoTexture);
        Material.SetTexture("_MainTex", MainTexture);
		//Instantiate
		GameObject instance = new GameObject("Mesh");
		instance.transform.SetParent(Terrain);
		MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = Material;
		MeshFilter filter = instance.AddComponent<MeshFilter>();
		filter.sharedMesh = MeshWorld;
		instance.AddComponent<MeshCollider>();
	}
    public void CreateAndSave(string path)
    {
        string pathAsset = Path.Combine(path.Substring(path.IndexOf("Assets")), "Terrain"+ TerName + ".asset");
        AssetDatabase.CreateAsset(MeshWorld, pathAsset);
    }
    void SetVerticsQuad(int x, int y,ref int index,ref float[] heightMap)
    {
        int indexQuad = GridToArray(x, y);
        if (indexQuad % 2 == 0)
        {
            Vector2 position = GridToWorld(x, y);
            // Vertices[index] = new Vertex(index);
            VertexData[index] = new Vector3(position.x, heightMap[index] < -1 ? -1 : heightMap[index], position.y);
            index++;
            Vector2 position1 = GridToWorld(x, y + 1);
            // Vertices[index] = new Vertex(index);
            VertexData[index] = new Vector3(position1.x, heightMap[index] < -1 ? -1 : heightMap[index], position1.y);
            index++;
            Vector2 position2 = GridToWorld(x + 1, y + 1);
            // Vertices[index] = new Vertex(index);
            VertexData[index] = new Vector3(position2.x, heightMap[index] < -1 ? -1 : heightMap[index], position2.y);
            index++;
            Vector2 position3 = GridToWorld(x, y);
            //Vertices[index] = new Vertex(index);
            VertexData[index] = new Vector3(position3.x, heightMap[index] < -1 ? -1 : heightMap[index], position3.y);
            index++;
            Vector2 position4 = GridToWorld(x + 1, y);
            //Vertices[index] = new Vertex(index);
            VertexData[index] = new Vector3(position4.x, heightMap[index] < -1 ? -1 : heightMap[index], position4.y);
            index++;
            Vector2 position5 = GridToWorld(x + 1, y + 1);
            //Vertices[index] = new Vertex(index);
            VertexData[index] = new Vector3(position5.x, heightMap[index] < -1 ? -1 : heightMap[index], position5.y);
            index++;
        }else
        {
            Vector2 position = GridToWorld(x, y);
            VertexData[index] = new Vector3(position.x, heightMap[index] < -1 ? -1 : heightMap[index], position.y);
            index++;
            Vector2 position1 = GridToWorld(x, y + 1);
            VertexData[index] = new Vector3(position1.x, heightMap[index] < -1 ? -1 : heightMap[index], position1.y);
            index++;
            Vector2 position2 = GridToWorld(x + 1, y);
            VertexData[index] = new Vector3(position2.x, heightMap[index] < -1 ? -1 : heightMap[index], position2.y);
            index++;
            Vector2 position3 = GridToWorld(x + 1, y);
            VertexData[index] = new Vector3(position3.x, heightMap[index] < -1 ? -1 : heightMap[index], position3.y);
            index++;
            Vector2 position4 = GridToWorld(x, y + 1);
            VertexData[index] = new Vector3(position4.x, heightMap[index] < -1 ? -1 : heightMap[index], position4.y);
            index++;
            Vector2 position5 = GridToWorld(x + 1, y + 1);
            VertexData[index] = new Vector3(position5.x, heightMap[index] < -1 ? -1 : heightMap[index], position5.y);
            index++;
        }
       
    }
    void SetTriggleQuad(int x,int y,ref int triangleIndex)
    {
        int indexQuad = GridToArray(x, y);
        int index1 = indexQuad * 6;
        if(indexQuad % 2 == 0)
        {
            TriangleData[triangleIndex] = index1;
            triangleIndex++;
            TriangleData[triangleIndex] = index1 + 1;
            triangleIndex++;
            TriangleData[triangleIndex] = index1 + 2;
            triangleIndex++;
            TriangleData[triangleIndex] = index1 + 3;
            triangleIndex++;
            TriangleData[triangleIndex] = index1 + 5;
            triangleIndex++;
            TriangleData[triangleIndex] = index1 + 4;
            triangleIndex++;
        }
        else{
            for(int i = 0; i < 6; i++,triangleIndex++)
            {
                TriangleData[triangleIndex] = index1 +i;
            }
        }
       
    }
    void SetUVQuad(int x,int y,ref int index)
    {
        int indexQuad = GridToArray(x, y);
        float tempx = 8 * Resolution.x / Size.x;
        float tempy = 8 * Resolution.y / Size.y;
        float disx = 1 / tempx;
        float disy = 1 / tempy;
        float x1 = x % (tempx + 1);
        float y1 = y % (tempy + 1);
        if(indexQuad % 2 == 0)
        {
            UVData[index] = new Vector2(x1 * disx, y1 * disy);
            index++;
            UVData[index] = new Vector2(x1 * disx, (y1 + 1) * disy);
            index++;
            UVData[index] = new Vector2((x1 + 1) * disx, (y1 + 1) * disy);
            index++;
            UVData[index] = new Vector2((x1) * disx, (y1) * disy);
            index++;
            UVData[index] = new Vector2((x1 + 1) * disx, (y1) * disy);
            index++;
            UVData[index] = new Vector2((x1 + 1) * disx, (y1 + 1) * disy);
            index++;
        }
        else
        {
            UVData[index] = new Vector2(x1 * disx, y1 * disy);
            index++;
            UVData[index] = new Vector2(x1 * disx, (y1 + 1) * disy);
            index++;
            UVData[index] = new Vector2((x1 + 1) * disx, (y1) * disy);
            index++;
            UVData[index] = new Vector2((x1 + 1) * disx, (y1) * disy);
            index++;
            UVData[index] = new Vector2((x1) * disx, (y1 + 1) * disy);
            index++;
            UVData[index] = new Vector2((x1 + 1) * disx, (y1 + 1) * disy);
            index++;
        }
       
    }
    void SetUvInfo()
    {
        //0101的uv在r,g
        int index = 0;
        float OffsetY = 1.0f / Generator.info_h;
        float OffsetX = 1.0f / Generator.info_w;
        for (int y = 0; y < Resolution.y - 1; y++)
        {
            for(int x = 0;x < Resolution.x - 1; x++)
            {
                int indexInfo = IndexMap[x + y * (Resolution.y - 1)];               
                if (indexInfo== 0 || !InfoUv.ContainsKey(indexInfo))
                {
                    _uv2[index] = new Vector2(0, 0);
                    index++;
                    _uv2[index] = new Vector2(0, 0);
                    index++;
                    _uv2[index] = new Vector2(0, 0);
                    index++;
                    _uv2[index] = new Vector2(0, 0);
                    index++;
                }
                else
                {
                    Vector2 uv = InfoUv[indexInfo];
                    _uv2[index] = new Vector2(uv.x, uv.y);
                    index++;
                    _uv2[index] = new Vector2(uv.x, uv.y + OffsetY);
                    index++;
                    _uv2[index] = new Vector2(uv.x + OffsetX, uv.y);
                    index++;
                    _uv2[index] = new Vector2(uv.x + OffsetX, uv.y + OffsetY);
                    index++;
                }
              
            }
        }

    }
    void SetColorUv()
    {
        int index = 0;
        float OffsetY = 1.0f / Generator.col_h ;
        float OffsetX = 1.0f / Generator.col_w ;
        float offset = 0.1f;
        for (int y = 0; y < Resolution.y - 1; y++)
        {
            for (int x = 0; x < Resolution.x - 1; x++)
            {
                int indexQuad = GridToArray(x, y);
                Color left = ColorCubeMap[indexQuad * 2];
                Color right = ColorCubeMap[indexQuad * 2 + 1];
                Vector2 uv_left = new Vector2(0, 0);
                Vector2 uv_right = new Vector2(0, 0);
                if (ColorsUv.ContainsKey(left))
                {
                    uv_left = ColorsUv[left];
                }
                if (ColorsUv.ContainsKey(right))
                {
                    uv_right = ColorsUv[right];
                }
                if(indexQuad % 2 == 0)
                {
                    _uv1[index] = new Vector2(uv_left.x + offset, uv_left.y + offset);
                    index++;
                    _uv1[index] = new Vector2(uv_left.x + offset, uv_left.y + OffsetY - offset);
                    index++;
                    _uv1[index] = new Vector2(uv_left.x + OffsetX - offset, uv_left.y + OffsetY - offset);
                    index++;
                    _uv1[index] = new Vector2(uv_right.x + offset, uv_right.y + offset);
                    index++;
                    _uv1[index] = new Vector2(uv_right.x + OffsetX - offset, uv_right.y + offset);
                    index++;
                    _uv1[index] = new Vector2(uv_right.x + OffsetX - offset, uv_right.y + OffsetY - offset);
                    index++;
                }
                else
                {
                    _uv1[index] = new Vector2(uv_left.x + offset, uv_left.y + offset);
                    index++;
                    _uv1[index] = new Vector2(uv_left.x + offset, uv_left.y + OffsetY - offset);
                    index++;
                    _uv1[index] = new Vector2(uv_left.x + OffsetX - offset, uv_left.y + offset);
                    index++;
                    _uv1[index] = new Vector2(uv_right.x + OffsetX - offset, uv_right.y + offset);
                    index++;
                    _uv1[index] = new Vector2(uv_right.x + offset, uv_right.y + OffsetY - offset);
                    index++;
                    _uv1[index] = new Vector2(uv_right.x + OffsetX - 0.001f, uv_right.y + OffsetY - 0.001f);
                    index++;
                }
            }
        }
    }
    Vector3[] CaluNormal()
    {
        Vector3[] nor = new Vector3[VertexData.Length];
        int ind = 0;
        for(int i = 0;i < (Resolution.y - 1); i++)
        {
            for(int j = 0;j < (Resolution.x - 1); j++)
            {
                int index = (i * (Resolution.y - 1) + j) * 6;
                Vector3 nor1 = new Vector3();
                nor1 = Vector3.Cross(new Vector3(VertexData[index + 1].x - VertexData[index].x, VertexData[index + 1].z - VertexData[index].z, VertexData[index + 1].y - VertexData[index].y), new Vector3(VertexData[index + 2].x- VertexData[index].x, VertexData[index + 2].z - VertexData[index].z, VertexData[index + 2].y - VertexData[index].y));
                var len = nor1.magnitude;
                nor1 /= len;
                for (int k = 0;k < 6; k++)
                {
                    nor[ind] = nor1;
                    ind++;
                }
            }
        }
        return nor;
    }
}
