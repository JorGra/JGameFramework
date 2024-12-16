using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Samples
{

    public class Entity : MonoBehaviour
    {
        [SerializeField] BaseStats baseStats;
        public Stats Stats { get; private set; }

        private void Awake()
        {
            Stats = new Stats(new StatsMediator(), baseStats);
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            Stats.Mediator.Update(Time.deltaTime);
        }
    }

}