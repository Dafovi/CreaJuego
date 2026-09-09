using System;
using UnityEngine;
namespace CreaJuego
{
    [DisallowMultipleComponent]
    public sealed class EnemyVitality : MonoBehaviour
    {
        public int Remaining { get; private set; }
        public bool Defeated { get; private set; }
        public Func<bool> playing;
        public Action defeated;
        float hideAt;
        void Awake() { Remaining=Mathf.Clamp(GetComponent<GameItem>().health,1,5); }
        public bool TryReceive(int damage)
        {
            if(!isActiveAndEnabled || Defeated || playing==null || !playing() || damage<=0) return false;
            Remaining=Mathf.Max(0,Remaining-damage); GetComponent<DamageFeedback>()?.Flash();
            if(Remaining==0) { Defeated=true; hideAt=Time.time+.15f; defeated?.Invoke(); }
            return true;
        }
        void Update() { if(Defeated && Time.time>=hideAt) gameObject.SetActive(false); }
    }
}
