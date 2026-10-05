using System;
using MelodySuite.Match3.Runtime.MelodySuite.Match3.Runtime;
using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    public class BoardDisplay2D : BoardDisplayBase
    {
        private Vector3 point;
        
        public override Vector2 GetTileSize(GameObject tilePrefab)
        {
            var collider = tilePrefab.GetComponentInChildren<Collider2D>();

            if (collider == null)
            {
                Debug.LogError($"No Collider2D found on {tilePrefab.name}");
                return Vector2.zero;
            }

            Vector2 size;

            switch (collider)
            {
                case BoxCollider2D box:
                    size = box.size;
                    break;

                case CircleCollider2D circle:
                    size = Vector2.one * (circle.radius * 2f);
                    break;

                case CapsuleCollider2D capsule:
                    size = capsule.size;
                    break;

                case PolygonCollider2D polygon:
                case CompositeCollider2D:
                    size = collider.bounds.size;
                    break;

                default:
                    size = collider.bounds.size;
                    break;
            }

            Debug.Log($"Collider: {collider.GetType().Name}, Size: {size}");

            return size;
        }
        
        public override BoardPosition GetTileFromPoint(Vector3 p)
        {
            this.point = p;
          
            RaycastHit2D[] hits = Physics2D.RaycastAll(this.point, Camera.main.transform.forward * 100);
            foreach (var raycastHit2D in hits)
            {
                if (raycastHit2D.transform != null &&
                    raycastHit2D.transform.TryGetComponent<TileObject>(out var tileObject))
                    return new BoardPosition(tileObject.Row, tileObject.Column);
            }
            
            return null;
            
            // Vector2 worldPosition = GetWorldMousePosition(ray);
            //
            // Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            //
            // if (hit != null &&
            //     hit.TryGetComponent<TileObject>(out var tile))
            // {
            //     return new BoardPosition(tile.Row, tile.Column);
            // }
            //
            // return null;
        }
        
        // private Vector2 GetWorldMousePosition(Ray ray)
        // {
        //     Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
        //
        //     if (boardPlane.Raycast(ray, out float distance))
        //     {
        //         Vector3 worldPosition = ray.GetPoint(distance);
        //
        //         return new Vector2(
        //             worldPosition.x,
        //             worldPosition.z
        //         );
        //     }
        //
        //     return Vector2.zero;
        // }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(point, 0.3f);
            Gizmos.DrawRay(point, Camera.main.transform.forward * 100);
        }
    }
}