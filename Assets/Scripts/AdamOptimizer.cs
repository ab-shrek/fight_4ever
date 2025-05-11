using UnityEngine;
using System;

public class AdamOptimizer
{
    private readonly float learningRate;
    private readonly float beta1;
    private readonly float beta2;
    private readonly float epsilon;
    private int t;
    private float[] m;
    private float[] v;

    public AdamOptimizer(float learningRate = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1e-8f)
    {
        this.learningRate = learningRate;
        this.beta1 = beta1;
        this.beta2 = beta2;
        this.epsilon = epsilon;
        this.t = 0;
    }

    public void Initialize(int parameterCount)
    {
        m = new float[parameterCount];
        v = new float[parameterCount];
        Array.Clear(m, 0, parameterCount);
        Array.Clear(v, 0, parameterCount);
    }

    public void Update(float[] parameters, float[] gradients)
    {
        if (m == null || v == null)
        {
            Initialize(parameters.Length);
        }

        t++;

        for (int i = 0; i < parameters.Length; i++)
        {
            // Update biased first moment estimate
            m[i] = beta1 * m[i] + (1 - beta1) * gradients[i];

            // Update biased second moment estimate
            v[i] = beta2 * v[i] + (1 - beta2) * gradients[i] * gradients[i];

            // Compute bias-corrected first moment estimate
            float mHat = m[i] / (1 - Mathf.Pow(beta1, t));

            // Compute bias-corrected second moment estimate
            float vHat = v[i] / (1 - Mathf.Pow(beta2, t));

            // Update parameters
            parameters[i] -= learningRate * mHat / (Mathf.Sqrt(vHat) + epsilon);
        }
    }
} 