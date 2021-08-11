﻿using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (TerrainGenerator))]
public class TerrainGeneratorEditor : Editor {
	
	private TerrainGenerator Target;

	private bool LeftMouseDown = false;
	private RaycastHit Hit;

	private System.DateTime Time;
	private float DeltaTime;

	private Vector2i Resolution;

    private Biome[] oldBiomes;
    
	void Awake() {
		Target = (TerrainGenerator)target;
        if (Target.TerrainSystem != null)
		    Resolution = Target.TerrainSystem.Resolution;
	}

	void OnEnable() {
		Tools.hidden = true;
	}

	void OnDisable() {
		Tools.hidden = false;
	}

    void OnSceneGUI() {
		DeltaTime = Mathf.Min(0.01f, (float)(System.DateTime.Now-Time).Duration().TotalSeconds); //100Hz Baseline
		Time = System.DateTime.Now;
		if(Event.current.type == EventType.Layout) {
			HandleUtility.AddDefaultControl(0);
		}
		DrawCursor();
		HandleInteraction();
    }

	public override void OnInspectorGUI() {
		Undo.RecordObject(Target, Target.name);
		Inspect();
		if(GUI.changed) {
			EditorUtility.SetDirty(Target);
            //if (oldBiomes != Target.TerrainSystem.Biomes)
          //  {
           //     oldBiomes = Target.TerrainSystem.Biomes;
               // Target.TerrainSystem.SetColorMap(Target.TerrainSystem.CreateColorMap());
           // }
        }
	}

	private void Inspect() {
        if (GUILayout.Button("初始化"))
        {
            TerrainGenerator terr = Target.gameObject.GetComponent<TerrainGenerator>();
            if(terr != null)
            {
                Target.TerrainSystem.Initialise(terr);
            }
            
        }
        Target.TerrainSystem = (TerrainSystem)EditorGUILayout.ObjectField("TerrainSystem", Target.TerrainSystem, typeof(TerrainSystem), true, null);
        InspectWorld(Target.TerrainSystem);
		//InspectTerrain();
		InspectTools();
        InspectSave();
        if (GUILayout.Button("ResetHeight"))
        {
            Target.TerrainSystem.ReHeight();
        }
        if (GUILayout.Button("ResetColor"))
        {
            Target.TerrainSystem.ReUV1();
        }
        if (GUILayout.Button("ResetInfo"))
        {
            Target.TerrainSystem.ReUV2();
        }
        if (GUILayout.Button("Reset")) {
			Target.TerrainSystem.Reinitialise();           
		}
	}

	private void InspectWorld(TerrainSystem terrainSystem) {
		if(terrainSystem == null) {
			return;
		}
		using(new EditorGUILayout.VerticalScope ("Button")) {
			GUI.backgroundColor = Color.white;
			EditorGUILayout.HelpBox("World", MessageType.None);

			terrainSystem.SetSize(EditorGUILayout.Vector2Field("Size", new Vector2(terrainSystem.Size.x,terrainSystem.Size.y)));
			Vector2 resolution = EditorGUILayout.Vector2Field("Resolution", new Vector2(Resolution.x, Resolution.y));
			Resolution = new Vector2i((int)resolution.x, (int)resolution.y);
			if(Resolution.x != terrainSystem.Resolution.x || Resolution.y != terrainSystem.Resolution.y) {
				EditorGUILayout.HelpBox("Changing the resolution will reset the world.", MessageType.Warning);
				if(GUILayout.Button("Apply")) {
					terrainSystem.SetResolution(Resolution);
				}
			}
		}
	}

    //private void InspectTerrain() {
    //	using(new EditorGUILayout.VerticalScope ("Button")) {
    //		GUI.backgroundColor = Color.white;
    //		EditorGUILayout.HelpBox("Terrain", MessageType.None);

    //		Target.Seed = EditorGUILayout.IntField("Seed", Target.Seed);
    //		Target.Scale = EditorGUILayout.FloatField("Scale", Target.Scale);
    //		Target.Octaves = EditorGUILayout.IntField("Octaves", Target.Octaves);
    //		Target.Persistance = EditorGUILayout.FloatField("Persistance", Target.Persistance);
    //		Target.Lacunarity = EditorGUILayout.FloatField("Lacunarity", Target.Lacunarity);
    //		Target.FalloffStrength = EditorGUILayout.FloatField("FalloffStrength", Target.FalloffStrength);
    //		Target.FalloffRamp = EditorGUILayout.FloatField("FalloffRamp", Target.FalloffRamp);
    //		Target.FalloffRange = EditorGUILayout.FloatField("FalloffRange", Target.FalloffRange);
    //		Target.Offset = EditorGUILayout.Vector2Field("Offset", Target.Offset);
    //		Target.HeightMultiplier = EditorGUILayout.FloatField("HeightMultiplier", Target.HeightMultiplier);
    //		Target.HeightCurve = EditorGUILayout.CurveField("HeightCurve", Target.HeightCurve);


    //           if (Target.TerrainSystem == null)
    //               return;

    //           using (new EditorGUILayout.VerticalScope("Button"))
    //           {
    //               GUI.backgroundColor = Color.white;
    //               EditorGUILayout.HelpBox("Biomes", MessageType.None);

    //               Target.TerrainSystem.Interpolation = EditorGUILayout.Slider("Interpolation", Target.TerrainSystem.Interpolation, 0f, 1f);
    //               Target.TerrainSystem.FilterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", Target.TerrainSystem.FilterMode);
    //               for (int i = 0; i < Target.TerrainSystem.Biomes.Length; i++)
    //               {
    //                   Target.TerrainSystem.Biomes[i].Color = EditorGUILayout.ColorField(Target.TerrainSystem.Biomes[i].Color);
    //                   float start = Target.TerrainSystem.Biomes[i].StartHeight;
    //                   float end = Target.TerrainSystem.Biomes[i].EndHeight;
    //                   EditorGUILayout.MinMaxSlider(ref start, ref end, 0f, 1f);
    //                   Target.TerrainSystem.SetBiomeStartHeight(i, start);
    //                   Target.TerrainSystem.SetBiomeEndHeight(i, end);
    //               }

    //               if (GUILayout.Button("Add Biome"))
    //               {
    //                   System.Array.Resize(ref Target.TerrainSystem.Biomes, Target.TerrainSystem.Biomes.Length + 1);
    //                   Target.TerrainSystem.Biomes[Target.TerrainSystem.Biomes.Length - 1] = new Biome();
    //               }
    //               if (GUILayout.Button("Remove Biome"))
    //               {
    //                   if (Target.TerrainSystem.Biomes.Length > 0)
    //                   {
    //                       System.Array.Resize(ref Target.TerrainSystem.Biomes, Target.TerrainSystem.Biomes.Length - 1);
    //                   }
    //               }
    //               if (GUILayout.Button("Generate Biomes"))
    //               {
    //                   if (Target.TerrainSystem.Biomes.Length > 0)
    //                   {
    //                       Target.TerrainSystem.SetColorMap(Target.TerrainSystem.CreateColorMap());
    //                   }
    //               }
    //           }
    //           GUILayout.Space(10);
    //           if (GUILayout.Button("Generate"))
    //           {
    //               Target.TerrainSystem.SetHeightMap(Target.TerrainSystem.CreateHeightMap(
    //                   Target.Seed, Target.Scale, Target.Octaves, Target.Persistance, Target.Lacunarity, Target.FalloffStrength, Target.FalloffRamp, Target.FalloffRange, Target.Offset, Target.HeightMultiplier, Target.HeightCurve
    //               ));

    //           }
    //           GUILayout.Space(2);

    //       }
    //}

    private void InspectTools()
    {
        using (new EditorGUILayout.VerticalScope("Button"))
        {
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Tools", MessageType.None);

            Target.ToolType = (ToolType)EditorGUILayout.EnumPopup("Type", Target.ToolType);
            Target.ToolSize = EditorGUILayout.FloatField("Size", Target.ToolSize);
            Target.ToolStrength = EditorGUILayout.FloatField("Strength", Target.ToolStrength);
            if (Target.ToolType == ToolType.Brush)
            {
                using (new EditorGUILayout.VerticalScope("Button"))
                {
                    GUI.backgroundColor = Color.white;
                    Target.col_h = EditorGUILayout.IntField("hight", Target.col_h);
                    Target.col_w = EditorGUILayout.IntField("width", Target.col_w);
                    Target.ToolLeft = EditorGUILayout.Toggle("绘制左三角形", Target.ToolLeft);
                    EditorGUILayout.HelpBox("Color", MessageType.None);
                    if (GUILayout.Button("Show Color"))
                    {
                        Target.TerrainSystem.ReadColor(Target.col_w, Target.col_h);
                    }
                    for (int i = 0; i < Target.TerrainSystem.ColorSet.Length; i++)
                    {
                        EditorGUILayout.ColorField("Color" + i, Target.TerrainSystem.ColorSet[i]);
                        if (GUILayout.Button("Set Color" + i))
                        {
                            Target.ToolColor = Target.TerrainSystem.ColorSet[i];
                        }
                    }
                }
            }
            if (Target.ToolType == ToolType.Info)
            {
                using (new EditorGUILayout.VerticalScope("Button"))
                {
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.HelpBox("Info", MessageType.None);
                    Target.info_h = EditorGUILayout.IntField("hight", Target.info_h);
                    Target.info_w = EditorGUILayout.IntField("width", Target.info_w);
                    if (GUILayout.Button("Show Info"))
                    {
                        Target.TerrainSystem.ReadInfo(Target.info_w, Target.info_h);
                    }
                    for (int i = 0; i <= Target.TerrainSystem.InfoUv.Count; i++)
                    {
                        if (GUILayout.Button("Set Info" + i))
                        {
                            Target.InfoIndex = i;
                        }
                    }
                }
            }
            if (Target.ToolType == ToolType.Biolog)
            {
                using (new EditorGUILayout.VerticalScope("Button"))
                {
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.HelpBox("Biolog", MessageType.None);
                    Target.ToolBio = (GameObject)EditorGUILayout.ObjectField("Biological", Target.ToolBio, typeof(GameObject), Target.ToolBio, GUILayout.Width(300));
                    if (GUILayout.Button("Add prefab"))
                    {
                        Target.TerrainSystem.ManageBioGames(Target.ToolBio);
                    }
                    for (int i = 0; i <= Target.TerrainSystem.bioGames.Count; i++)
                    {
                        if (GUILayout.Button("Set Biological" + i))
                        {
                            Target.ToolBioIndex = i;
                        }
                    }
                }

            }
        }
    }
        private void InspectSave()
    {
        using (new EditorGUILayout.VerticalScope("Button"))
        {
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Save & Create", MessageType.None);
            Target.TerrainSystem.TerName = EditorGUILayout.TextField("MeshName", Target.TerrainSystem.TerName);
            if (GUILayout.Button("Save And Create Mesh"))
            {
                Target.path = EditorUtility.OpenFolderPanel("选择数据存储目录", Target.path, "");
                Target.TerrainSystem.CreateAndSave(Target.path);
            }
        }
    }
	private void DrawCursor() {
		Ray ray = HandleUtility.GUIPointToWorldRay(new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y));
        Target.isMouseOver = Physics.Raycast(ray.origin, ray.direction, out Hit);
		Target.MousePosition = Hit.point;
        EditorUtility.SetDirty(target);
    }

	private void HandleInteraction() {
		if(Event.current.type == EventType.MouseDown && Event.current.button == 0) {
			LeftMouseDown = true;
		}
		if(Event.current.type == EventType.MouseUp && Event.current.button == 0) {
			LeftMouseDown = false;
		}
		if(LeftMouseDown && Target.isMouseOver) {
			if(Target.ToolType == ToolType.Brush) {
				Target.TerrainSystem.ModifyTexture(new Vector2(Target.MousePosition.x, Target.MousePosition.z), Target.ToolSize, Target.ToolColor,Target.ToolLeft);
			} else if(Target.ToolType == ToolType.Normal || Target.ToolType == ToolType.Bumps) {
				Target.TerrainSystem.ModifyTerrain(new Vector2(Target.MousePosition.x, Target.MousePosition.z), Target.ToolSize, Target.ToolStrength*DeltaTime, Target.ToolType);
			}
            else if(Target.ToolType == ToolType.Info)
            {
                Target.TerrainSystem.ModifyInfo(new Vector2(Target.MousePosition.x, Target.MousePosition.z), Target.ToolSize, Target.InfoIndex);
            }
            else if(Target.ToolType == ToolType.Biolog){
                Target.TerrainSystem.ModifyDiolog(new Vector2(Target.MousePosition.x, Target.MousePosition.z), Target.ToolSize, Target.ToolBioIndex);
            }
		}
	}

}
