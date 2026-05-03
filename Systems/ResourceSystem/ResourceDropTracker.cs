using JG.Audio;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ResourceDropTracker : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;

    private int playerId;
    private string resourceId;
    private int unitAmount;
    private int remainderAmount;
    private WorldParticleAttractor attractor;
    private ResourceDropPresenter presenter;
    private SoundData collectSound;

    private int lastCount;
    private bool active;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();
    }

    public ParticleSystem ParticleSystem => ps;

    public void Spawn(
        ResourceDropPresenter owner,
        int playerId,
        string resourceId,
        int burst,
        int unitAmount,
        int remainderAmount,
        Vector3 worldPosition,
        Material renderMaterial,
        WorldParticleAttractor attractor,
        SoundData collectSound)
    {
        this.presenter = owner;
        this.playerId = playerId;
        this.resourceId = resourceId;
        this.unitAmount = unitAmount;
        this.remainderAmount = remainderAmount;
        this.attractor = attractor;
        this.collectSound = collectSound;
        this.lastCount = 0;
        this.active = true;

        if (renderMaterial != null && psRenderer != null)
        {
            psRenderer.sharedMaterial = renderMaterial;
        }

        transform.position = worldPosition;
        gameObject.SetActive(true);

        ps.Clear(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ps.Emit(burst);

        if (attractor != null)
        {
            attractor.AddParticleSystem(ps);
        }
    }

    private void LateUpdate()
    {
        if (!active) return;

        int current = ps.particleCount;
        int absorbed = Mathf.Max(0, lastCount - current);
        if (absorbed > 0)
        {
            if (unitAmount > 0)
            {
                ResourceManager.Instance.AddResource(playerId, resourceId, absorbed * unitAmount);
            }

            if (collectSound != null && collectSound.clip != null)
            {
                Vector3 soundPos = attractor != null ? attractor.transform.position : transform.position;
                for (int i = 0; i < absorbed; i++)
                {
                    EventBus<PlaySoundEvent>.Raise(new PlaySoundEvent(collectSound, soundPos));
                }
            }
        }

        lastCount = current;

        if (current == 0)
        {
            Complete();
        }
    }

    private void Complete()
    {
        active = false;

        if (remainderAmount > 0)
        {
            ResourceManager.Instance.AddResource(playerId, resourceId, remainderAmount);
            remainderAmount = 0;
        }

        if (attractor != null)
        {
            attractor.RemoveParticleSystem(ps);
            attractor = null;
        }

        if (presenter != null)
        {
            presenter.Return(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (active && attractor != null)
        {
            attractor.RemoveParticleSystem(ps);
        }
        active = false;
        attractor = null;
    }
}
