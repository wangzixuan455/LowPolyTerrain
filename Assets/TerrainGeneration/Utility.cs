using System.Collections.Generic;
using UnityEngine;

public static class Utility {
	public static float Normalise(float value, float valueMin, float valueMax, float resultMin, float resultMax) {
		if(valueMax-valueMin != 0f) {
			return (value-valueMin)/(valueMax-valueMin)*(resultMax-resultMin) + resultMin;
		} else {
			return 0f;
		}
	}

	//public static void FlatShading(ref Vector3[] vertices, ref int[] triangles, ref Vector2[] uvs) {
	//	Vector3[] flatVertices = new Vector3[triangles.Length];
	//	Vector2[] flatUVs = new Vector2[triangles.Length];
	//	for(int i=0; i<triangles.Length; i++) {
	//		flatVertices[i] = vertices[triangles[i]];
	//		flatUVs[i] = uvs[triangles[i]];
	//		triangles[i] = i;
	//	}
	//	vertices = flatVertices;
	//	uvs = flatUVs;
	//}

	public static Mesh CreateMeshes(Vector3[] vertices, int[] triangles, Vector2[] uvs, int chunkSize,Vector2[] uv1,Vector2[] uv2,Vector3[] nor) {
        //Mesh[] meshes = new Mesh[Mathf.CeilToInt((float)vertices.Length / (float)chunkSize)];//分成几个Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
        mesh.uv2 = uv1;
        mesh.uv3 = uv2;
        mesh.normals = nor;
		return mesh;
	}


    public static T[] RangeSubset<T>(this T[] array, int startIndex, int length) {
        T[] subset = new T[length];
        System.Array.Copy(array, startIndex, subset, 0, length);
        return subset;
    }

 	public static Texture2D CreateTexture(Color[] colorMap, int dimX, int dimY, FilterMode filterMode) {
		Texture2D texture = new Texture2D(dimX, dimY);
		texture.filterMode = filterMode;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colorMap);
		texture.Apply();
		return texture;
	}
}