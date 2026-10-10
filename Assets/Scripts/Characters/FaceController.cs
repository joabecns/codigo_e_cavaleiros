using UnityEngine;

namespace CodigoECavaleiros.Characters
{
    /// <summary>Expressões faciais disponíveis para os personagens.</summary>
    public enum FaceType
    {
        Normal,
        Feliz,
        Raiva
    }

    /// <summary>
    /// Liga um rosto por vez (normal, feliz ou raiva). Cada rosto é uma malha
    /// separada presa ao mesmo esqueleto do personagem.
    /// </summary>
    public class FaceController : MonoBehaviour
    {
        [SerializeField] private Renderer faceNormal;
        [SerializeField] private Renderer faceFeliz;
        [SerializeField] private Renderer faceRaiva;
        [SerializeField] private FaceType startFace = FaceType.Normal;

        public FaceType Current { get; private set; }

        private void Awake()
        {
            SetFace(startFace);
        }

        public void SetFace(FaceType face)
        {
            Current = face;
            if (faceNormal != null) faceNormal.enabled = face == FaceType.Normal;
            if (faceFeliz != null) faceFeliz.enabled = face == FaceType.Feliz;
            if (faceRaiva != null) faceRaiva.enabled = face == FaceType.Raiva;
        }

        /// <summary>Atalho para ligar o rosto pelo nome (usado por eventos de animação).</summary>
        public void SetFaceByName(string faceName)
        {
            FaceType parsed;
            if (System.Enum.TryParse(faceName, true, out parsed))
            {
                SetFace(parsed);
            }
        }
    }
}
