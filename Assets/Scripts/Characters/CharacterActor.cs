using UnityEngine;

namespace CodigoECavaleiros.Characters
{
    public enum Emotion { Neutra, Raiva, Feliz }

    /// <summary>
    /// Controla a aparência (rosto) e as animações de um personagem.
    /// Não contém regras de jogo: só apresentação.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterActor : MonoBehaviour
    {
        [Header("Rostos (um ativo por vez)")]
        public GameObject faceNeutral;
        public GameObject faceAngry;
        public GameObject faceHappy;

        [Header("Dados")]
        public string displayName = "Personagem";

        static readonly int TrigPain = Animator.StringToHash("Dor");
        static readonly int TrigRage = Animator.StringToHash("Raiva");
        static readonly int TrigCheer = Animator.StringToHash("Comemorar");
        static readonly int TrigLament = Animator.StringToHash("Lamentar");
        static readonly int TrigIdle = Animator.StringToHash("Idle");

        Animator animator;

        void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            SetEmotion(Emotion.Neutra);
        }

        public void SetEmotion(Emotion e)
        {
            if (faceNeutral) faceNeutral.SetActive(e == Emotion.Neutra);
            if (faceAngry) faceAngry.SetActive(e == Emotion.Raiva);
            if (faceHappy) faceHappy.SetActive(e == Emotion.Feliz);
        }

        void Fire(int trigger)
        {
            if (animator == null) animator = GetComponent<Animator>();
            foreach (var t in new[] { TrigPain, TrigRage, TrigCheer, TrigLament, TrigIdle })
                animator.ResetTrigger(t);
            animator.SetTrigger(trigger);
        }

        public void PlayIdle()    { SetEmotion(Emotion.Neutra); Fire(TrigIdle); }
        public void PlayPain()    { SetEmotion(Emotion.Raiva);  Fire(TrigPain); }   // cara de dor/raiva
        public void PlayRage()    { SetEmotion(Emotion.Raiva);  Fire(TrigRage); }
        public void PlayCheer()   { SetEmotion(Emotion.Feliz);  Fire(TrigCheer); }
        public void PlayLament()  { SetEmotion(Emotion.Neutra); Fire(TrigLament); }
    }
}
