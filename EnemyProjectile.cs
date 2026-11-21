using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private const float Speed = 7f;
    private const float HitRadius = .12f;
    private const float VisualDiameter = .34f;
    private const float Lifetime = 2f;

    private readonly List<RaycastHit2D> hits = new();
    private ContactFilter2D contactFilter;
    private int wallLayer;
    private Vector2 direction;
    private float hitRadius;
    private Vector2 slashHitbox;
    private PlayerHealth target;
    private float damage;

    public static void Fire(Vector2 origin, Vector2 aim, float damage, Sprite sprite, PlayerHealth target, float sizeMultiplier = 1f)
    {
        float scale = VisualDiameter * sizeMultiplier / sprite.bounds.size.x;
        var projectile = Launch(origin, aim, damage, sprite, target, scale, Lifetime);
        projectile.hitRadius = HitRadius * sizeMultiplier;
    }

    public static void FireSlash(Vector2 origin, Vector2 aim, float damage, Sprite sprite, PlayerHealth target, float span)
    {
        float scale = span / sprite.bounds.size.y;
        var projectile = Launch(origin, aim, damage, sprite, target, scale, .32f);
        projectile.slashHitbox = new Vector2(.32f, span * .78f);
        float angle = Mathf.Atan2(projectile.direction.y, projectile.direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static EnemyProjectile Launch(Vector2 origin, Vector2 aim, float damage, Sprite sprite, PlayerHealth target, float scale, float lifetime)
    {
        var bullet = new GameObject("Enemy Bullet");
        bullet.transform.position = origin;

        var renderer = bullet.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 15;
        bullet.transform.localScale = Vector3.one * scale;

        var projectile = bullet.AddComponent<EnemyProjectile>();
        projectile.direction = aim.sqrMagnitude > .0001f ? aim.normalized : Vector2.down;
        projectile.target = target;
        projectile.damage = damage;
        projectile.wallLayer = LayerMask.NameToLayer("ArenaWall");
        projectile.contactFilter.SetLayerMask(LayerMask.GetMask("ArenaWall") | (1 << target.gameObject.layer));
        Destroy(bullet, lifetime);
        return projectile;
    }

    private void FixedUpdate()
    {
        float distance = Speed * Time.fixedDeltaTime;
        if (slashHitbox == Vector2.zero)
            Physics2D.CircleCast(transform.position, hitRadius, direction, contactFilter, hits, distance);
        else
            Physics2D.BoxCast(transform.position, slashHitbox, transform.eulerAngles.z, direction, contactFilter, hits, distance);
        RaycastHit2D firstHit = default;
        foreach (RaycastHit2D hit in hits)
        {
            bool isWall = hit.collider.gameObject.layer == wallLayer;
            if (!isWall && (target == null || hit.collider.GetComponentInParent<PlayerHealth>() != target))
                continue;

            if (firstHit.collider == null || hit.distance < firstHit.distance ||
                (isWall && hit.distance == firstHit.distance))
                firstHit = hit;
        }

        if (firstHit.collider == null)
        {
            transform.position += (Vector3)(direction * distance);
            return;
        }

        if (firstHit.collider.gameObject.layer != wallLayer)
            target.TakeDamage(damage);

        enabled = false;
        Destroy(gameObject);
    }
}
