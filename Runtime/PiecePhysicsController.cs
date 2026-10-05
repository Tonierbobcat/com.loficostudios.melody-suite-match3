using System;
using System.Collections;
using System.Collections.Generic;
using MelodySuite.Match3.Runtime.MelodySuite.Match3.Runtime;
using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    public class PiecePhysicsController : MonoBehaviour
    {
        [SerializeField] private LeanTweenType fallEase = LeanTweenType.linear;
        [SerializeField] private float fallSpeed = 10;

        [Range(0.001f, 1f)] [SerializeField] private float fallCompleteThreshHoldPercent = 0.985f;

        [SerializeField] private float fallDelaySeconds = 0.1f; 
        
        public IEnumerator Fall(FallData fallData, BoardDisplayBase boardDisplay, Func<Vector2, BoardPosition, PieceObject> spawnPiece)
        {
            var width = boardDisplay.width;
            var height = boardDisplay.height;
            
            List<(PieceObject piece, Vector2Int origin, Vector2Int destination)> piecesToMove = new();
            
            foreach (var (origin, destination) in fallData.movements)
            {
                var piece = boardDisplay.GetPieceObject(new BoardPosition(origin.y, origin.x));

                piecesToMove.Add((piece, origin, destination));
            }

            foreach (var (piece, origin, destination) in piecesToMove)
            {
                piece.Init(piece.Piece);

                var destinationPos = boardDisplay.GetTileLocalSpawnPosition(destination.y, destination.x);

                var distance = Vector2.Distance(boardDisplay.GetTileLocalSpawnPosition(origin.y, origin.x), destinationPos);

                LeanTween.moveLocal(
                    piece.gameObject,
                    destinationPos,
                    distance / fallSpeed
                ).setMoveLocal().setDelay(fallDelaySeconds).setEase(fallEase);
            }

            yield return new WaitForSeconds(fallDelaySeconds);

            var step = new int[width];
            for (var i = 0; i < width; i++)
            {
                step[i] = 0;
            }

            var completed = 0;

            var possiblePieces = new List<(GameObject obj, float distance, Vector2 targetLocalPos)>[width];

            for (var column = 0; column < width; column++)
            {
                possiblePieces[column] = new List<(GameObject obj, float distance, Vector2 targetLocalPos)>();
            }

            fallData.tilesToFill.Sort((i, i1) => i.Row < i1.Row ? -1 : 1);
            foreach (var vector2Int in fallData.tilesToFill)
            {
                var row = vector2Int.Row;
                var column = vector2Int.Column;

                var spawnPos = boardDisplay.GetTileLocalSpawnPosition(height + step[column]++, column);
                
                var obj = spawnPiece(spawnPos, new BoardPosition(row, column));

                var targetLocalPos = boardDisplay.GetTileLocalSpawnPosition(row, column);

                var distance = Vector2.Distance(spawnPos, targetLocalPos);
                LeanTween.moveLocal(
                    obj.gameObject,
                    targetLocalPos,
                    distance / fallSpeed
                ).setMoveLocal().setEase(fallEase).setOnComplete(() => completed++);

                possiblePieces[column].Add((obj.gameObject, distance, targetLocalPos));

                yield return new WaitForSeconds(fallDelaySeconds);
            }

            yield return new WaitUntil(() => completed >= fallData.tilesToFill.Count);
        }
    }
}