using UnityEngine;

/// <summary>
/// Tiny particle helpers built in code so the game has hit/harvest/death effects
/// without any prefabs. Uses the built-in Sprites/Default shader (always included in builds).
/// </summary>
public static class FX
{
    private static Material material;

    private static Material Mat
    {
        get
        {
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            return material;
        }
    }

    public static void Burst(Vector3 position, Color color, int count = 10, float size = 0.12f, float speed = 3f)
    {
        GameObject go = new GameObject("FX_Burst");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = 0.45f;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(count * 2, 8);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.material = Mat;
        r.sortingOrder = 30;

        ps.Play();
        Object.Destroy(go, 1.5f);
    }

    public static void Line(Vector3 from, Vector3 to, Color color, float life = 0.08f)
    {
        GameObject go = new GameObject("FX_Line");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.startColor = color;
        lr.endColor = color;
        lr.material = Mat;
        lr.sortingOrder = 30;
        Object.Destroy(go, life);
    }
}
