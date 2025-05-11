using UnityEngine;
using UnityEditor;
using Unity.Barracuda;
using Unity.Barracuda.ONNX;
using System.IO;

public class ONNXToNNModelConverter : EditorWindow
{
    private string onnxFolder = "Assets/runtime_models";
    private string outputFolder = "Assets/Resources/NNModels";

    [MenuItem("Tools/Convert ONNX to NNModel")]
    public static void ShowWindow()
    {
        GetWindow<ONNXToNNModelConverter>("ONNX to NNModel Converter");
    }

    void OnGUI()
    {
        GUILayout.Label("ONNX to NNModel Converter", EditorStyles.boldLabel);
        onnxFolder = EditorGUILayout.TextField("ONNX Folder", onnxFolder);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        if (GUILayout.Button("Convert All ONNX Files"))
        {
            ConvertAllONNXFiles(onnxFolder, outputFolder);
        }
    }

    // Batch mode static method for command line
    public static void ConvertAllONNXFiles()
    {
        ConvertAllONNXFiles("Assets/runtime_models", "Assets/Resources/NNModels");
    }

    // Core conversion logic
    public static void ConvertAllONNXFiles(string onnxFolder, string outputFolder)
    {
        if (!Directory.Exists(onnxFolder))
        {
            Debug.LogError($"ONNX folder does not exist: {onnxFolder}");
            return;
        }
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }
        string[] onnxFiles = Directory.GetFiles(onnxFolder, "*.onnx");
        foreach (var onnxPath in onnxFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(onnxPath);
            string nnModelPath = Path.Combine(outputFolder, fileName + ".nn");
            Debug.Log($"Converting {onnxPath} to {nnModelPath}");
            var converter = new ONNXModelConverter(true, false, false);
            var model = converter.Convert(onnxPath);
            ModelWriter.Save(nnModelPath, model);
            AssetDatabase.ImportAsset(nnModelPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ONNX to NNModel conversion complete.");
    }
} 