using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    [CreateAssetMenu(fileName = "PieceDefinition", menuName = "Scriptable Objects/PieceDefinition")]
    public class PieceDefinition : ScriptableObject
    {
        public Sprite icon;
    }
}