using System.Collections;
using UnityEngine;

/// <summary>
/// EffectBase adapter that wraps a SquashStretchApplier,
/// allowing squash/stretch to be used inside an EffectorController.
/// </summary>
public class SquashStretchEffect : EffectBase
{
    [SerializeField] Transform target;
    [SerializeField] SquashStretchProfile profile;

    SquashStretchApplier _applier;

    public override void InitEffector()
    {
        _applier = new SquashStretchApplier(target != null ? target : transform);
    }

    protected override IEnumerator PlayEffectLogic()
    {
        if (_applier == null)
            InitEffector();

        if (profile == null) yield break;

        _applier.Play(profile);
        float elapsed = 0f;
        while (elapsed < profile.duration)
        {
            elapsed += Time.deltaTime;
            _applier.Tick(Time.deltaTime);
            yield return null;
        }
        _applier.Stop();
    }
}
