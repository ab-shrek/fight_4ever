using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class AIShooting : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float maxShootingDistance = 50f;
    [SerializeField] private float accuracy = 0.8f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstacleLayer;

    private float nextFireTime;
    private GameObject currentTarget;
    private bool isShooting;
    private string instanceId;
    private StreamWriter logFile;

    private void Start()
    {
        try
        {
            instanceId = System.Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "unknown";
            Log($"Initialized with fire rate: {fireRate}, bullet speed: {bulletSpeed}, max distance: {maxShootingDistance}, accuracy: {accuracy}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AIShooting] Error in Start: {e.Message}\n{e.StackTrace}");
        }
    }

    public void SetLogFile(StreamWriter file)
    {
        logFile = file;
        Log("Log file set");
    }

    private void Log(string message)
    {
        string fullMessage = $"[AIShooting {gameObject.name}] {message}";
        if (logFile != null)
        {
            logFile.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {fullMessage}");
            logFile.Flush();
        }
        Debug.Log(fullMessage);
    }

    public void SetTarget(GameObject target)
    {
        currentTarget = target;
        Log($"Target set to: {(target != null ? target.name : "None")}");
    }

    public void StartShooting()
    {
        isShooting = true;
        Log("Started shooting");
    }

    public void StopShooting()
    {
        isShooting = false;
        Log("Stopped shooting");
    }

    private void Update()
    {
        if (!isShooting || currentTarget == null)
            return;

        if (Time.time >= nextFireTime)
        {
            if (CanShootTarget())
            {
                Shoot();
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    private bool CanShootTarget()
    {
        if (currentTarget == null)
            return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance > maxShootingDistance)
        {
            Log($"Target too far: {distance:F2}m");
            return false;
        }

        Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToTarget, out hit, distance, obstacleLayer))
        {
            Log($"Line of sight blocked by: {hit.collider.gameObject.name}");
            return false;
        }

        return true;
    }

    public void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Log("Cannot shoot: bullet prefab or fire point is null");
            return;
        }

        // Calculate spread based on accuracy
        float spread = (1f - accuracy) * 5f;
        Vector3 spreadDirection = firePoint.forward;
        spreadDirection += new Vector3(
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread)
        );
        spreadDirection.Normalize();

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(spreadDirection));
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.shooter = gameObject;
        }

        Log($"Shot bullet at target: {currentTarget.name} | Position: {firePoint.position} | Direction: {spreadDirection}");
    }

    private void OnDestroy()
    {
        if (logFile != null)
        {
            logFile.Close();
            logFile = null;
        }
    }
} 