using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    public class PieceObject : MonoBehaviour
    {
   
        public GamePiece Piece { get; private set; }

        public bool Animating { get; set; }
    
        public void AnimateUpdatePosition(Vector2 localPosition)
        {
            Animating = true;
            // UpdatePosition(row, column);
            LeanTween.cancel(gameObject);
        
            var target = transform.parent.TransformPoint(localPosition);
            var distance = Vector3.Distance(transform.position, target);

            LeanTween.moveLocal(gameObject, localPosition, distance / 4f)
                .setOnComplete(() => Animating = false);
        }
    
        public void Init(GamePiece piece)
        {
            // Row = row;
            // Column = column;
            Piece = piece;
        
            var render = GetComponent<SpriteRenderer>();
            render.sprite = piece.Type.icon;
        }

        private Vector3 initScale;

        private void Awake()
        {
            initScale = transform.localScale;
        }

        // private void Start()
        // {
        //     GetComponent<SpriteRenderer>().material = _normalMat;
        // }

        bool highlighted = false;
        public void Highlight(bool b)
        {
            var wasHighlighted = highlighted;
            highlighted = b;

            if (wasHighlighted == highlighted)
                return;
            // Debug.Log("Highlighted: " + highlighted);
            if (!b)
            {
                // transform.localScale = initScale;
                GetComponent<SpriteRenderer>().material.SetFloat("_Highlighted", 0);
            }
            else
            {
                GetComponent<SpriteRenderer>().material.SetFloat("_Highlighted", 1f);
                // transform.localScale = initScale / 2;
            }
        }
    }
}
