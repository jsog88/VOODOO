using UnityEngine;

public class BossGemPickup : MonoBehaviour
{
    public float rotateSpeed = 120f;
    public float bobHeight = 0.25f;
    public float attractRadius = 3.5f;
    public float moveSpeed = 12f;

    private Transform _player;
    private Vector3 _basePos;
    private bool _collected;

    private void Start()
    {
        _basePos = transform.position;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_collected || _player == null) return;

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        Vector3 targetPoint = _player.position + Vector3.up * 0.9f;
        float dist = Vector3.Distance(transform.position, targetPoint);

        if (dist <= attractRadius)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);
            if (dist <= 0.6f)
            {
                _collected = true;
                if (GameRunManager.Instance != null)
                {
                    GameRunManager.Instance.CollectBossGem();
                }
                Destroy(gameObject);
            }
        }
        else
        {
            transform.position = _basePos + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
        }
    }
}
