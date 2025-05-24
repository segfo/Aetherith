using System;
using System.Collections.Generic;
using UnityEngine;

public class ShakeDetector : MonoBehaviour
{
    private int bufferSize = 10;
    [SerializeField] private float angleThreshold = 120f; // 角度（度）以上で反転とみなす
    [SerializeField] private int directionChangeCountThreshold = 3;
    [SerializeField] private float timeWindow = 0.5f;

    private Queue<Vector3> velocityBuffer = new Queue<Vector3>();
    private Queue<float> timestampBuffer = new Queue<float>();

    public event Action OnShaken;

    void UpdateVelocity(Vector3 velocity)
    {
        // バッファに追加
        velocityBuffer.Enqueue(velocity);
        timestampBuffer.Enqueue(Time.time);

        // 古いデータ削除
        while (velocityBuffer.Count > bufferSize || (timestampBuffer.Count > 0 && Time.time - timestampBuffer.Peek() > timeWindow))
        {
            velocityBuffer.Dequeue();
            timestampBuffer.Dequeue();
        }
        DetectShakePattern();
    }

    void DetectShakePattern()
    {
        if (velocityBuffer.Count < 3) return;

        Vector3[] velocities = velocityBuffer.ToArray();
        int directionChanges = 0;

        for (int i = 1; i < velocities.Length - 1; i++)
        {
            Vector3 prev = velocities[i - 1].normalized;
            Vector3 current = velocities[i].normalized;

            float angle = Vector3.Angle(prev, current);
            if (angle > angleThreshold)
            {
                directionChanges++;
            }
        }

        if (directionChanges >= directionChangeCountThreshold)
        {
            OnShaken?.Invoke();
            velocityBuffer.Clear();
            timestampBuffer.Clear();
        }
    }

    public void FeedVelocity(Vector3 velocity)
    {
        if (velocity.magnitude > 0.01f)
        {
            UpdateVelocity(velocity);
        }
    }
}
