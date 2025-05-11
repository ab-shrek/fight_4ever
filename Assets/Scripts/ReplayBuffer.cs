using System;
using System.Collections.Generic;
using UnityEngine;

public class ReplayBuffer
{
    private readonly int capacity;
    private readonly List<Experience> buffer;
    private int position = 0;

    private class Experience
    {
        public float[] state;
        public float[] action;
        public float reward;
        public float[] nextState;
        public bool done;

        public Experience(float[] state, float[] action, float reward, float[] nextState, bool done)
        {
            this.state = state;
            this.action = action;
            this.reward = reward;
            this.nextState = nextState;
            this.done = done;
        }
    }

    public ReplayBuffer(int capacity)
    {
        this.capacity = capacity;
        this.buffer = new List<Experience>(capacity);
    }

    public void Add(float[] state, float[] action, float reward, float[] nextState, bool done)
    {
        if (buffer.Count < capacity)
        {
            buffer.Add(new Experience(state, action, reward, nextState, done));
        }
        else
        {
            buffer[position] = new Experience(state, action, reward, nextState, done);
            position = (position + 1) % capacity;
        }
    }

    public (float[] states, float[] actions, float[] rewards, float[] nextStates, bool[] dones) Sample(int batchSize)
    {
        if (buffer.Count < batchSize)
        {
            throw new InvalidOperationException("Not enough experiences in buffer for sampling");
        }

        int[] indices = new int[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            indices[i] = UnityEngine.Random.Range(0, buffer.Count);
        }

        float[] states = new float[batchSize * buffer[0].state.Length];
        float[] actions = new float[batchSize * buffer[0].action.Length];
        float[] rewards = new float[batchSize];
        float[] nextStates = new float[batchSize * buffer[0].nextState.Length];
        bool[] dones = new bool[batchSize];

        for (int i = 0; i < batchSize; i++)
        {
            var exp = buffer[indices[i]];
            Array.Copy(exp.state, 0, states, i * exp.state.Length, exp.state.Length);
            Array.Copy(exp.action, 0, actions, i * exp.action.Length, exp.action.Length);
            rewards[i] = exp.reward;
            Array.Copy(exp.nextState, 0, nextStates, i * exp.nextState.Length, exp.nextState.Length);
            dones[i] = exp.done;
        }

        return (states, actions, rewards, nextStates, dones);
    }

    public int Count => buffer.Count;
} 