using UnityEngine;
using Unity.Barracuda;
using System;

public class NeuralNetworkModel
{
    private IWorker worker;
    private Model modelAsset;
    private bool isInitialized = false;

    public void Initialize()
    {
        try
        {
            // Create a new neural network model instance
            modelAsset = new Model();
            
            // Initialize the worker with the model
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, modelAsset);
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize neural network model: {e.Message}");
            isInitialized = false;
        }
    }

    public float[] Predict(float[] input)
    {
        if (!isInitialized)
        {
            Debug.LogError("Neural network model not initialized");
            return null;
        }

        try
        {
            // Create input tensor
            var inputTensor = new Tensor(1, input.Length, input);
            
            // Execute the model
            worker.Execute(inputTensor);
            
            // Get the output tensor
            var outputTensor = worker.PeekOutput();
            
            // Convert output tensor to float array
            float[] output = outputTensor.ToReadOnlyArray();
            
            // Clean up tensors
            inputTensor.Dispose();
            outputTensor.Dispose();
            
            return output;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during prediction: {e.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (worker != null)
        {
            worker.Dispose();
            worker = null;
        }
        isInitialized = false;
    }
} 