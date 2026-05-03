using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WorldParticleAttractor : MonoBehaviour
{
    public enum Movement { Linear, Smooth, Sphere }

    [Range(0.05f, 10f)] public float destinationRadius = 0.3f;
    [Range(0f, 0.95f)] public float delayRate = 0f;
    [Range(0.001f, 100f)] public float maxSpeed = 6f;
    public Movement movement = Movement.Smooth;
    public UnityEvent onAttracted;

    private readonly List<ParticleSystem> particleSystems = new();
    private static ParticleSystem.Particle[] particleBuffer;

    public void AddParticleSystem(ParticleSystem ps)
    {
        if (ps == null) return;
        if (particleSystems.Contains(ps)) return;
        particleSystems.Add(ps);
    }

    public void RemoveParticleSystem(ParticleSystem ps)
    {
        if (ps == null) return;
        particleSystems.Remove(ps);
    }

    private void LateUpdate()
    {
        if (particleSystems.Count == 0) return;

        var worldDst = transform.position;

        for (int i = particleSystems.Count - 1; i >= 0; i--)
        {
            var ps = particleSystems[i];
            if (ps == null) { particleSystems.RemoveAt(i); continue; }

            int count = ps.particleCount;
            if (count == 0) continue;

            EnsureBuffer(count);
            ps.GetParticles(particleBuffer, count);

            bool isLocal = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            var dstPos = isLocal ? ps.transform.InverseTransformPoint(worldDst) : worldDst;

            for (int j = 0; j < count; j++)
            {
                var p = particleBuffer[j];
                if (p.remainingLifetime <= 0f) continue;

                if (Vector3.Distance(p.position, dstPos) < destinationRadius)
                {
                    p.remainingLifetime = 0f;
                    particleBuffer[j] = p;
                    onAttracted?.Invoke();
                    continue;
                }

                float delay = p.startLifetime * delayRate;
                float duration = Mathf.Max(0.0001f, p.startLifetime - delay);
                float time = Mathf.Max(0f, p.startLifetime - p.remainingLifetime - delay);
                if (time <= 0f) continue;

                float speed = maxSpeed * 60f * Time.deltaTime;
                Vector3 target = dstPos;
                switch (movement)
                {
                    case Movement.Linear:
                        speed /= duration;
                        break;
                    case Movement.Smooth:
                        target = Vector3.Lerp(p.position, dstPos, time / duration);
                        break;
                    case Movement.Sphere:
                        target = Vector3.Slerp(p.position, dstPos, time / duration);
                        break;
                }

                p.position = Vector3.MoveTowards(p.position, target, speed);
                p.velocity *= 0.5f;
                particleBuffer[j] = p;
            }

            ps.SetParticles(particleBuffer, count);
        }
    }

    private static void EnsureBuffer(int count)
    {
        if (particleBuffer == null || particleBuffer.Length < count)
        {
            particleBuffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(Mathf.Max(16, count))];
        }
    }
}
