using UnityEngine;

public enum BoundaryMode
{
    Breach,
    CleanupBottom,
    CleanupTop
}

public class BoundaryTrigger : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] BoundaryMode _mode = BoundaryMode.CleanupBottom;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(BoundaryTrigger)} requires a {nameof(RoundManager)} reference.", this);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        switch (_mode)
        {
            case BoundaryMode.Breach:
                HandleBreach(other);
                break;
            case BoundaryMode.CleanupBottom:
                HandleCleanupBottom(other);
                break;
            case BoundaryMode.CleanupTop:
                HandleCleanupTop(other);
                break;
        }
    }

    void HandleBreach(Collider2D other)
    {
        if (!TryGetFromCollider(other, out FallingThreat threat))
        {
            return;
        }

        if (!threat.IsInUse || threat.Kind != ThreatKind.Breach)
        {
            return;
        }

        if (_roundManager != null)
        {
            _roundManager.ReportBreachReached();
        }
    }

    void HandleCleanupBottom(Collider2D other)
    {
        if (TryGetFromCollider(other, out FallingThreat threat) && threat.IsInUse)
        {
            threat.ReturnToPool();
            return;
        }

        if (TryGetFromCollider(other, out EnemyProjectile enemyProjectile) && enemyProjectile.IsInUse)
        {
            enemyProjectile.ReturnToPool();
        }
    }

    void HandleCleanupTop(Collider2D other)
    {
        if (TryGetFromCollider(other, out PlayerProjectile playerProjectile) && playerProjectile.IsInUse)
        {
            playerProjectile.ReturnToPool();
        }
    }

    static bool TryGetFromCollider<T>(Collider2D other, out T component) where T : Component
    {
        component = other.GetComponentInParent<T>();
        return component != null;
    }
}
