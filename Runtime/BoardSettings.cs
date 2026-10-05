using System;
using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    [CreateAssetMenu(fileName = "BoardDisplay", menuName = "MelodySuite/BoardDisplay")]
    public class BoardSettings : ScriptableObject
    {
        [Range(1, 16)] public int width = 1;
        [Range(1, 16)] public int height = 1;
        
        [SerializeField] public GameObject piecePrefab;
        
        [Header("Grid")]
        public GameObject tilePrefab;
        
        #region DisplaySettings

        [HideInInspector]
        public float xOffset = 0;
        [HideInInspector]
        public float yOffset = 0;
        
        public float tileScale = 1;
        public float pieceScale = 1;
        #endregion

        private void OnValidate()
        {
            tileScale = Math.Max(tileScale, 0.01f);
            pieceScale = Math.Max(pieceScale, 0.01f);
        }
    }
}