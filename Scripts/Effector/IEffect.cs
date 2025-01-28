using System.Collections;

public interface IEffect
{
    float StartDelay { get; set; }
    float EndDelay { get; set; }

    void InitEffector();
    IEnumerator PlayEffect(bool decoupled = false);  // This method will handle the effect logic and timing
}