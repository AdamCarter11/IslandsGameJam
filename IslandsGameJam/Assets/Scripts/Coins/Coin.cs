using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct CoinDenomination
{
    public int amount;
    public Sprite sprite;
}

/// <summary>
/// World-dropped coin. Attracts to the cursor when nearby, accelerates toward it, and Collects on contact (ie, when mouse hovers)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    public static event Action<int, Vector3> OnCoinCollected;

    [SerializeField] List<CoinDenomination> denominations = new();
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Cursor Magnet")]
    [SerializeField] float attractRadius = 1.5f;
    [SerializeField] float collectRadius = 0.2f;
    [SerializeField] float initialSpeed = 4f;
    [SerializeField] float acceleration = 18f;
    [SerializeField] float maxSpeed = 40f;

    int amount;
    Collider2D cachedCollider;
    bool attracting;
    float currentSpeed;
    bool collected;

    /// <summary>Inspector denomination config used by CoinDropService for batching.</summary>
    public IReadOnlyList<CoinDenomination> Denominations => denominations;

    void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (collected || Mouse.current == null || Camera.main == null)
            return;

        #region Grab positional values and distance
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld3 = Camera.main.ScreenToWorldPoint(mouseScreen);
        Vector2 cursor = new Vector2(mouseWorld3.x, mouseWorld3.y);
        Vector2 pos = transform.position;
        float dist = Vector2.Distance(pos, cursor);
        #endregion

        if (!attracting)
        {
            if (dist > attractRadius)
                return;

            attracting = true;
            currentSpeed = initialSpeed;
        }

        // collect the coin when it gets close enough to cursor (initial check before doing actual movement)
        if (dist <= collectRadius)
        {
            Collect();
            return;
        }

        // move the coin
        currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);
        Vector2 next = Vector2.MoveTowards(pos, cursor, currentSpeed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);

        // collect the coin when it gets close enough to cursor
        if (Vector2.Distance(next, cursor) <= collectRadius)
            Collect();
    }

    /// <summary>
    /// Sets coin value, applies matching sprite from the denomination list, and enables the collider
    /// </summary>
    public void Setup(int value)
    {
        amount = value;

        #region Grab sprite from denom list and assign it
        if (spriteRenderer != null)
        {
            Sprite match = null;
            for (int i = 0; i < denominations.Count; i++)
            {
                if (denominations[i].amount == value)
                {
                    match = denominations[i].sprite;
                    break;
                }
            }
            if (match != null)
                spriteRenderer.sprite = match;
        }
        #endregion

        #region Collider logic
        if (cachedCollider == null)
            cachedCollider = GetComponent<Collider2D>();
        if (cachedCollider != null)
            cachedCollider.enabled = true;
        #endregion
    }

    /// <summary>
    /// Adds gold to inventory, fires OnCoinCollected, then destroys this coin
    /// </summary>
    public void Collect()
    {
        if (collected)
            return;
        collected = true;

        var inventory = GameManager.Main != null ? GameManager.Main.Inventory : null;
        if (inventory != null)
            inventory.AddGold(amount);

        OnCoinCollected?.Invoke(amount, transform.position);
        Destroy(gameObject);
    }
}
