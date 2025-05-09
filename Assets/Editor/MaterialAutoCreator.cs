using UnityEngine;
using UnityEditor;
using System.IO;

public class MaterialAutoCreator
{
    [MenuItem("Tools/Create Required Materials")]
    public static void CreateMaterials()
    {
        string resourcesPath = "Assets/Resources";
        if (!Directory.Exists(resourcesPath))
            Directory.CreateDirectory(resourcesPath);

        CreateMaterial("GroundMat", new Color(0.18f, 0.18f, 0.18f));
        CreateMaterial("WallMat", new Color(0.4f, 0.2f, 0.1f));
        CreateMaterial("CoverMat", new Color(0.5f, 0.5f, 0.5f));
        CreateMaterial("ZoneMat", new Color(0.2f, 0.4f, 1f, 0.18f)); // Example: blue zone
        CreateMaterial("GridMat", new Color(1f, 1f, 1f, 0.3f));      // Example: white grid

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All required materials created in Assets/Resources!");
    }

    private static void CreateMaterial(string name, Color color)
    {
        string path = $"Assets/Resources/{name}.mat";
        if (File.Exists(path))
            return; // Don't overwrite

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            Debug.LogError("Unlit/Color shader not found!");
            return;
        }

        Material mat = new Material(shader);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
    }
} 