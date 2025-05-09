using UnityEngine;

public class AIShooting : MonoBehaviour
{
    [SerializeField] internal float bulletSpeed = 20f;
    [SerializeField] internal float fireRate = 1f;
    [SerializeField] internal float bulletLifetime = 3f;
    [SerializeField] internal float bulletDamage = 10f;
    [SerializeField] internal Vector3 firePointOffset = new Vector3(0, 1, 1);

    private float nextFireTime;
    private GameObject bulletPrefab;
    private Transform firePoint;

    private void Awake()
    {
        CreateFirePoint();
        CreateBulletPrefab();
    }

    private void CreateFirePoint()
    {
        // Create FirePoint GameObject
        GameObject firePointObj = new GameObject("FirePoint");
        
        // Make it a child of the AI
        firePointObj.transform.SetParent(transform);
        
        // Position it relative to the AI
        firePointObj.transform.localPosition = firePointOffset;
        
        // Store the reference
        firePoint = firePointObj.transform;
    }

    private void CreateBulletPrefab()
    {
        // Create bullet GameObject
        bulletPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletPrefab.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        
        // Remove Rigidbody and Collider
        DestroyImmediate(bulletPrefab.GetComponent<Rigidbody>());
        DestroyImmediate(bulletPrefab.GetComponent<SphereCollider>());
        
        // Add Bullet script
        Bullet bulletScript = bulletPrefab.AddComponent<Bullet>();
        
        // Set material color
        bulletPrefab.GetComponent<Renderer>().material.color = Color.red;
        
        // Make it inactive initially
        bulletPrefab.SetActive(false);
    }

    public void Shoot()
    {
        if (Time.time >= nextFireTime)
        {
            // Create bullet instance
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            bullet.SetActive(true);

            // Set shooter reference for RL
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.shooter = this.gameObject;
            }

            // Set next fire time
            nextFireTime = Time.time + fireRate;
        }
    }
} 