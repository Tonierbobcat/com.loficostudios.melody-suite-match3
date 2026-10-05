using System.Collections;
using Unity.Plastic.Antlr3.Runtime.Misc;
using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    public class PieceAnimator : MonoBehaviour
    {
        public void Match(Action callback)
        {
            StartCoroutine(PlayMatchAnimation(callback));
        }

        private IEnumerator PlayMatchAnimation(Action action)
        {
            var complete = false;
            LeanTween.scale(gameObject, transform.localScale * 3, 0.2f)
                .setOnComplete(() => complete = true);
            LeanTween.alpha(gameObject, 0, 0.2f);
            yield return new WaitUntil(() => complete);
            action?.Invoke();
        }
    }
}