using System.Collections.Generic;
using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    public class BasicGamePieceGenerator : MonoBehaviour, IGamePieceGenerator
    {
        [SerializeField]
        private List<PieceDefinition> pieces;
        
        public GamePiece Generate()
        {
            return new GamePiece(pieces[Random.Range(0, pieces.Count)]);
        }
    }
}