using UnityEngine;

/// <summary>
/// Lightweight squash/stretch animator driven by an external Tick() call.
/// No coroutines, no MonoBehaviour — pure state machine.
///
/// Direct mode (WriteScale = true, default): writes localScale each tick.
/// Multiplier mode (WriteScale = false): only updates CurrentMultiplier for external combining.
/// </summary>
public class SquashStretchApplier
{
    readonly Transform _target;
    readonly Vector3 _originalScale;

    float _elapsed;
    float _duration;
    AnimationCurve _curveX;
    AnimationCurve _curveY;

    public bool WriteScale { get; set; } = true;
    public Vector2 CurrentMultiplier { get; private set; } = Vector2.one;
    public bool IsPlaying { get; private set; }

    public SquashStretchApplier(Transform target)
    {
        _target = target;
        _originalScale = target != null ? target.localScale : Vector3.one;
    }

    public void Play(SquashStretchProfile profile)
    {
        if (profile == null) return;

        if (IsPlaying && profile.retriggerMode == RetriggerMode.Ignore)
            return;

        _elapsed = 0f;
        _duration = profile.duration;
        _curveX = profile.scaleX;
        _curveY = profile.scaleY;
        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
        CurrentMultiplier = Vector2.one;
        _curveX = null;
        _curveY = null;

        if (WriteScale && _target != null)
            _target.localScale = _originalScale;
    }

    /// <summary>
    /// Advance the animation. Call once per frame from Update or LateUpdate.
    /// Costs one branch when idle.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsPlaying) return;

        _elapsed += deltaTime;

        if (_elapsed >= _duration)
        {
            IsPlaying = false;
            CurrentMultiplier = Vector2.one;
            _curveX = null;
            _curveY = null;

            if (WriteScale && _target != null)
                _target.localScale = _originalScale;
            return;
        }

        float t = _elapsed / _duration;
        float mx = _curveX.Evaluate(t);
        float my = _curveY.Evaluate(t);
        CurrentMultiplier = new Vector2(mx, my);

        if (WriteScale && _target != null)
        {
            _target.localScale = new Vector3(
                _originalScale.x * mx,
                _originalScale.y * my,
                _originalScale.z);
        }
    }
}
