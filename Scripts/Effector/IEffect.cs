using System.Collections;

public interface IEffect
{
    float StartDelay { get; set; }
    float EndDelay { get; set; }
    IEnumerator PlayEffect();  // This method will handle the effect logic and timing
}