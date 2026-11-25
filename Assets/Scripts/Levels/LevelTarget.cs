using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelTarget : MonoBehaviour
{
    [SerializeField, Min(0)] private int scoreValue = 500;
    [SerializeField] private float killHeight = -15f;

    public event Action<LevelTarget> Destroyed;

    public int ScoreValue => scoreValue;
    public bool IsDestroyed { get; private set; }

    public void Kill()
    {
        Notify();
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!IsDestroyed && transform.position.y < killHeight) Kill(); // cazut
    }

    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded) Notify(); // nu la unload
    }

    private void Notify()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        Destroyed?.Invoke(this);
    }
}
