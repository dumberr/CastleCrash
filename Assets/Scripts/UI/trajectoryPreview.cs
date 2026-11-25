using UnityEngine;

public class TrajectoryPreview : MonoBehaviour
{
    public int pointsCount = 25;
    public float timeStep = 0.07f;

    private CatapultAiming aiming;
    private ProjectileLauncher launcher;
    private LineRenderer line;

    void Start()
    {
        aiming = GetComponent<CatapultAiming>();
        launcher = GetComponent<ProjectileLauncher>();

        // Creează automat o linie nouă doar pentru traiectorie (nu atinge linia veche)
        GameObject obj = new GameObject("TrajectoryLine");
        obj.transform.SetParent(transform, false);

        line = obj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.startWidth = 0.08f;
        line.endWidth = 0.02f;
        line.sortingOrder = 4;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        line.material = new Material(shader);

        line.startColor = Color.white;
        line.endColor = new Color(1f, 1f, 1f, 0.2f);
    }

    void LateUpdate()
    {
        if (aiming == null || line == null) return;

        if (launcher != null && !launcher.IsReady)
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;
        line.positionCount = pointsCount;

        Vector2 startPos = aiming.LaunchPosition;
        Vector2 startVelocity = aiming.LaunchVelocity;
        Vector2 gravity = Physics2D.gravity;

        for (int i = 0; i < pointsCount; i++)
        {
            float t = i * timeStep;
            Vector2 point = startPos + startVelocity * t + 0.5f * gravity * (t * t);
            line.SetPosition(i, point);
        }
    }
}