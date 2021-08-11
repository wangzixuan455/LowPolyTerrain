using UnityEngine;

[System.Serializable]
public class Vector2i {
	public int x;
	public int y;
	public Vector2i(int _x, int _y) {
		x = _x;
		y = _y;
	}
}

[System.Serializable]
public class Biome {
	public Color Color = Color.black;
	public float StartHeight = 0f;
	public float EndHeight = 1f;
}

[System.Serializable]
public class Vertex {
	public int Index;
	public int[] VertexIndices = new int[0];

	public Vertex(int index) {
		Index = index;
	}

	public void SetPosition(float x, float y, TerrainSystem terrainSystem) {
		for(int i=0; i<VertexIndices.Length; i++) {
			terrainSystem.VertexData[VertexIndices[i]].x = x;
			terrainSystem.VertexData[VertexIndices[i]].z = y;
		}
	}

	public void SetHeight(float value, TerrainSystem terrainSystem) {
		terrainSystem.HeightMap[Index] = value;
		for(int i=0; i<VertexIndices.Length; i++) {
			terrainSystem.VertexData[VertexIndices[i]].y = terrainSystem.HeightMap[Index];
		}
	}

	public void UpdateHeight(float value, TerrainSystem terrainSystem) {
		terrainSystem.HeightMap[Index] += value;
		terrainSystem.VertexData[Index].y = terrainSystem.HeightMap[Index];
	}

	public float GetHeight(TerrainSystem terrainSystem) {
		return terrainSystem.HeightMap[Index];
	}


	public void AddVertexIndex(int index) {
		System.Array.Resize(ref VertexIndices, VertexIndices.Length+1);
		VertexIndices[VertexIndices.Length-1] = index;
	}
	
}