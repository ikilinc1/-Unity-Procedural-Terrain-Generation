using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EditorGUITable;

[CustomEditor(typeof(CustomTerrain))]
[CanEditMultipleObjects]
public class CustomTerrainEditor : Editor
{
    // properties -----------------
    private SerializedProperty randomHeightRange;
    private SerializedProperty heightMapScale;
    private SerializedProperty heightMapImage;
    private SerializedProperty perlinXScale;
    private SerializedProperty perlinYScale;
    private SerializedProperty perlinOffsetX;
    private SerializedProperty perlinOffsetY;
    private SerializedProperty perlinOctaves;
    private SerializedProperty perlinPersistence;
    private SerializedProperty perlinHeightScale;
    private SerializedProperty resetTerrain;
    private SerializedProperty voronoiFalloff;
    private SerializedProperty voronoiDropoff;
    private SerializedProperty voronoiMinHeight;
    private SerializedProperty voronoiMaxHeight;
    private SerializedProperty voronoiPeaks;
    private SerializedProperty voronoiType;
    private SerializedProperty MPDMinHeight;
    private SerializedProperty MPDMaxHeight;
    private SerializedProperty MPDDampening;
    private SerializedProperty MPDRoughness;
    private SerializedProperty smoothIteration;

    private GUITableState perlinParameterTable;
    private SerializedProperty perlinParameters;
    
    private GUITableState splatMapTable;
    private SerializedProperty splatHeights;

    // fold outs ------------------
    private bool showRandom = false;
    private bool showLoadHeights = false;
    private bool showPerlinNoice = false;
    private bool showMultiplePerlin = false;
    private bool showVoronoi = false;
    private bool showMPD = false;
    private bool showSmooth = false;
    private bool showSplatMaps = false;

    private void OnEnable()
    {
        randomHeightRange = serializedObject.FindProperty("randomHeightRange");
        heightMapScale = serializedObject.FindProperty("heightMapScale");
        heightMapImage = serializedObject.FindProperty("heightMapImage");
        perlinXScale = serializedObject.FindProperty("perlinXScale");
        perlinYScale = serializedObject.FindProperty("perlinYScale");
        perlinOffsetX = serializedObject.FindProperty("perlinOffsetX");
        perlinOffsetY = serializedObject.FindProperty("perlinOffsetY");
        perlinOctaves = serializedObject.FindProperty("perlinOctaves");
        perlinPersistence = serializedObject.FindProperty("perlinPersistence");
        perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");
        resetTerrain = serializedObject.FindProperty("resetTerrain");
        voronoiFalloff = serializedObject.FindProperty("voronoiFalloff");
        voronoiDropoff = serializedObject.FindProperty("voronoiDropoff");
        voronoiMinHeight = serializedObject.FindProperty("voronoiMinHeight");
        voronoiMaxHeight = serializedObject.FindProperty("voronoiMaxHeight");
        voronoiPeaks = serializedObject.FindProperty("voronoiPeaks");
        voronoiType = serializedObject.FindProperty("voronoiType");
        MPDMinHeight = serializedObject.FindProperty("MPDMinHeight");
        MPDMaxHeight = serializedObject.FindProperty("MPDMaxHeight");
        MPDDampening = serializedObject.FindProperty("MPDDampening");
        MPDRoughness = serializedObject.FindProperty("MPDRoughness");
        smoothIteration = serializedObject.FindProperty("smoothIteration");
        perlinParameterTable = new GUITableState("perlinParameters");
        perlinParameters = serializedObject.FindProperty("perlinParameters");
        splatMapTable = new GUITableState("splatHeights");
        splatHeights = serializedObject.FindProperty("splatHeights");
    }

    private Vector2 scrollPos;
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CustomTerrain terrain = (CustomTerrain) target;

        Rect r = EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(r.width), GUILayout.Height(r.height));
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(resetTerrain);

        showRandom = EditorGUILayout.Foldout(showRandom, "Random");
        if (showRandom)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(randomHeightRange);
            if (GUILayout.Button("Random Heights"))
            {
                terrain.RandomTerrain();
            }
        }

        showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
        if (showLoadHeights)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(heightMapImage);
            EditorGUILayout.PropertyField(heightMapScale);
            if (GUILayout.Button("Load Texture"))
            {
                terrain.LoadTexture();
            }
        }
        
        showPerlinNoice = EditorGUILayout.Foldout(showPerlinNoice, "Single Perlin Noise");
        if (showPerlinNoice)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Perlin Noise", EditorStyles.boldLabel);
            EditorGUILayout.Slider(perlinXScale, 0,1,new GUIContent("X Scale"));
            EditorGUILayout.Slider(perlinYScale, 0,1,new GUIContent("Y Scale"));
            EditorGUILayout.IntSlider(perlinOffsetX, 0,10000,new GUIContent("X Offset"));
            EditorGUILayout.IntSlider(perlinOffsetY, 0,10000,new GUIContent("Y Offset"));
            EditorGUILayout.IntSlider(perlinOctaves, 1,10,new GUIContent("Octaves"));
            EditorGUILayout.Slider(perlinPersistence, 0.1f,10,new GUIContent("Persistence"));
            EditorGUILayout.Slider(perlinHeightScale, 0,1,new GUIContent("Height Scale"));
            if (GUILayout.Button("Perlin"))
            {
                terrain.Perlin();
            }
        }

        showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin Noise");
        if (showMultiplePerlin)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Multiple Perlin Noise", EditorStyles.boldLabel);
            perlinParameterTable =
                GUITableLayout.DrawTable(perlinParameterTable, perlinParameters);
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewPerlin();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemovePerlin();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply Multiple Perlin"))
            {
                terrain.MultiplePerlinTerrain();
            }
        }
        
        showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
        if (showVoronoi)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Voronoi", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(voronoiPeaks, 1, 10, new GUIContent("Peak Count"));
            EditorGUILayout.Slider(voronoiFalloff, 0, 10, new GUIContent("Falloff"));
            EditorGUILayout.Slider(voronoiDropoff, 0, 10, new GUIContent("Drop-off"));
            EditorGUILayout.Slider(voronoiMinHeight, 0, 1, new GUIContent("Min Height"));
            EditorGUILayout.Slider(voronoiMaxHeight, 0, 1, new GUIContent("Max Height"));
            EditorGUILayout.PropertyField(voronoiType);
            if (GUILayout.Button("Voronoi"))
            {
                terrain.Voronoi();
            }
        }
        
        showMPD = EditorGUILayout.Foldout(showMPD, "Midpoint Displacement");
        if (showMPD)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Midpoint Displacement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(MPDMinHeight);
            EditorGUILayout.PropertyField(MPDMaxHeight);
            EditorGUILayout.PropertyField(MPDDampening);
            EditorGUILayout.PropertyField(MPDRoughness);
            if (GUILayout.Button("Midpoint Displacement"))
            {
                terrain.MidpointDisplacement();
            }
        }

        showSplatMaps = EditorGUILayout.Foldout(showSplatMaps, "Splat Maps");
        if (showSplatMaps)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Splat Maps", EditorStyles.boldLabel);
            splatMapTable =
                GUITableLayout.DrawTable(splatMapTable, splatHeights);
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewSplatHeight();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveSplatHeight();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply Splat Maps"))
            {
                terrain.SplatMaps();
            }
        }

        showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth Terrain");
        if (showSmooth)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Smooth Terrain", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(smoothIteration, 1, 20, new GUIContent("Smooth Iteration"));
            if (GUILayout.Button("Smooth Terrain"))
            {
                terrain.Smooth();
            }
        }


        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (GUILayout.Button("Reset Terrain"))
        {
            terrain.ResetTerrain();
        }
        


        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
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
