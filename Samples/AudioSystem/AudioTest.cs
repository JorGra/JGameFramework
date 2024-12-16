using JG.Audio;
using System.Collections;
using UnityEngine;

namespace JG.Samples
{

    public class AudioTest : MonoBehaviour
    {

        [SerializeField] SoundData soundData;
        // Start is called before the first frame update
        void Start()
        {
            //StartCoroutine(PlaySoundEveryInterval(0.01f));

        }

        // Update is called once per frame
        void Update()
        {
            //Play sound every 0.1 second

            if (Input.GetKeyDown(KeyCode.Space))
                SoundManager.Instance.CreateSound()
                    .WithSoundData(soundData)
                    .WithRadnomPitch(-.3f, .3f)
                    .WithPosition(transform.position)
                    .Play();
        }

        private IEnumerator PlaySoundEveryInterval(float interval)
        {
            while (true)
            {
                Debug.Log("Playing sound");
                SoundManager.Instance.CreateSound()
                    .WithSoundData(soundData)
                    .WithRadnomPitch(-.3f, .3f)
                    .WithPosition(transform.position)
                    .Play();
                yield return new WaitForSeconds(interval);
                //PlaySoundEveryInterval(0.01f);
            }
        }
    }

}