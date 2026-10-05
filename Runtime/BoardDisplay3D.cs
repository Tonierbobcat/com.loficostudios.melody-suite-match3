using MelodySuite.Match3.Runtime.MelodySuite.Match3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MelodySuite.Match3.Runtime
{
    public class BoardDisplay3D : BoardDisplayBase
    {
        private Vector3 point;
        public override Vector2 GetTileSize(GameObject tilePrefab)
        {
            
            var collider = tilePrefab.GetComponent<SphereCollider>();

            float diameter = collider.radius * 2f;

            Debug.Log("Diameter: " + diameter);

            return new Vector2(diameter, diameter);
        }

        public override BoardPosition GetTileFromPoint(Vector3 p)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform != null &&
                    hit.transform.TryGetComponent<TileObject>(out var tileObject))
                    return new BoardPosition(tileObject.Row, tileObject.Column);
            }
            
            // Debug.Log("GetTileFromPoint: " + p);
            // p.z = Camera.main.transform.position.z;
            // point = p;
            //
            //
            // var hits = Physics.RaycastAll(this.point, Camera.main.transform.forward * 100);
            // foreach (var hit in hits)
            // {
            //     if (hit.transform != null &&
            //         hit.transform.TryGetComponent<TileObject>(out var tileObject))
            //         return new BoardPosition(tileObject.Row, tileObject.Column);
            // }
            
            return null;
        }
        
        // private void OnDrawGizmos()
        // {
        //     Gizmos.DrawSphere(point, 0.3f);
        //     Gizmos.DrawRay(point, Camera.main.transform.forward * 100);
        // }
    }
}