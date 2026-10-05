namespace MelodySuite.Match3.Runtime
{
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace MelodySuite.Match3.Runtime
{
    public abstract class BoardDisplayBase : MonoBehaviour, IBoardHandler
    {

        [SerializeField] private bool interactable = true;

        [SerializeField] private bool animateDrag = false;
        
        [SerializeField] private BoardSettings boardSettings;
        
        [Header("Optional")]
        [SerializeField] private SpriteRenderer border;
        public float borderPadding = 0;
        
        [SerializeField] private bool generateOnAwake;
        
        private Transform tileParent;
        
        private Transform piecesParent;

        private Board mainBoard;

        private Board displayBoard;

        private PieceObject[][] displayed;
        
        private Dictionary<PieceObject, BoardPosition> originalPositions = new();
        
        private PieceObject dragObject;

        private BoardPosition clickedTile;

        // private SpriteRenderer tilePrefabSpriteRenderer;

        public abstract Vector2 GetTileSize(GameObject tilePrefab);
        
        public readonly UnityEvent<List<Match>> OnMatch = new();
        public readonly UnityEvent OnMoveStart = new();
        public readonly UnityEvent OnMoveResolved = new();
        
        public UnityEvent OnVisualSwap = new();
        
        public int width => boardSettings.width;
        public int height => boardSettings.height;
        
        public bool Interactable
        {
            get => interactable;
            set
            {
                var last = interactable;
                interactable = value;
                if (last != value)
                    Debug.Log("Setting input to: " + value);
                // input = value;
                // if (input != last && !input)
                // {
                //     SyncBoard();
                // }
            }
        }

        // public Vector2 CenterWorldPosition()
        // {
        //     return border.transform.position;
        // }


        private void OnValidate()
        {
            if (!gameObject.activeInHierarchy)
                return;

            // if (tilePrefabSpriteRenderer == null)
            //     tilePrefabSpriteRenderer = boardSettings.tilePrefab.GetComponent<SpriteRenderer>();


            tileParent = transform.Find("tiles");
            if (tileParent == null)
            {
                var obj = new GameObject("tiles");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                tileParent = obj.transform;
            }

            piecesParent = transform.Find("pieces");
            if (piecesParent == null)
            {
                var obj = new GameObject("pieces");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                piecesParent = obj.transform;
            }

            StartCoroutine(SpawnTilesEditor());
        }

        private void InitializeDisplay()
        {
            if (!gameObject.activeInHierarchy)
                return;
            
            // if (tilePrefabSpriteRenderer == null)
            //     tilePrefabSpriteRenderer = boardSettings.tilePrefab.GetComponent<SpriteRenderer>();


            tileParent = transform.Find("tiles");
            if (tileParent == null)
            {
                var obj = new GameObject("tiles");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                tileParent = obj.transform;
            }

            piecesParent = transform.Find("pieces");
            if (piecesParent == null)
            {
                var obj = new GameObject("pieces");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                piecesParent = obj.transform;
            }
  
            ComputeInvalid(DestroyImmediate);
            SpawnTiles();
        }
        
        private IEnumerator SpawnTilesEditor()
        {
            ComputeInvalid(go => StartCoroutine(DestroyOnSync(go)));
            yield return null;
            SpawnTiles();
        }

        private void ComputeInvalid(Action<GameObject> callback)
        {
            for (int i = tileParent.childCount - 1; i >= 0; i--)
            {
                Transform child = tileParent.GetChild(i);
                var split = child.name.Split(',');

                if (split.Length == 2 &&
                    int.TryParse(split[0], out int row) &&
                    int.TryParse(split[1], out int col))
                {
                    if (row >= 0 && row < height && col >= 0 && col < width)
                    {
                        continue;
                    }
                }

                callback.Invoke(child.gameObject);
            }
        }

        private void Awake()
        {
            InitializeDisplay();
            if (generateOnAwake)
                GenerateBoard();
        }

        private void SpawnTiles()
        {
            // var spriteRenderer = tilePrefabSpriteRenderer;
            // Vector2 spriteSize = spriteRenderer.bounds.size;
            var spriteSize = GetTileSize(boardSettings.tilePrefab);
            float xSpacing = spriteSize.x * boardSettings.tileScale;
            float ySpacing = spriteSize.y * boardSettings.tileScale;
            
            string GetBoardPosStr(BoardPosition pos)
            {
                return pos.Row + "," + pos.Column;
            }
            
            List<BoardPosition> total = new List<BoardPosition>();

            for (int column = 0; column < width; column++)
            {
                for (int row = 0; row < height; row++)
                {
                    total.Add(new BoardPosition(row, column));
                }
            }
            
            foreach (var boardPosition in total)
            {
                var str = GetBoardPosStr(boardPosition);
                var child = tileParent.Find(str);
                var spawnPos = GetTileLocalSpawnPosition(boardPosition.Row, boardPosition.Column, xSpacing, ySpacing);

                if (!child)
                {
                    var obj = Instantiate(boardSettings.tilePrefab, Vector3.zero, Quaternion.identity, tileParent);
                    obj.transform.name = str;
                    child = obj.transform;
                }

                child.GetComponent<TileObject>().Init(boardPosition.Row, boardPosition.Column);

                child.transform.localScale = new Vector3(boardSettings.tileScale, boardSettings.tileScale, boardSettings.tileScale);
                child.transform.localPosition = spawnPos;
            }

            if (!border)
                return;
            
            Vector3 bScale = border.transform.localScale;
            float scaleX = bScale.x == 0 ? 1f : bScale.x;
            float scaleY = bScale.y == 0 ? 1f : bScale.y;

            float totalGridWidth = width * xSpacing + borderPadding;
            float totalGridHeight = height * ySpacing + borderPadding;

            border.drawMode = SpriteDrawMode.Tiled;
            border.size = new Vector2(totalGridWidth / scaleX, totalGridHeight / scaleY);

            float borderCenterX = (boardSettings.xOffset - (xSpacing / 2f)) + ((totalGridWidth / 2f) - (borderPadding / 2f));
            float borderCenterY = boardSettings.yOffset + ((totalGridHeight / 2f) - (borderPadding / 2f));

            border.transform.position = tileParent.position + new Vector3(borderCenterX, borderCenterY, 0);
        }

        public Vector2 GetTileWorldSpawnPosition(int row, int column)
        {
            return tileParent.TransformPoint(GetTileLocalSpawnPosition(row, column));
        }

        public Vector2 GetTileLocalSpawnPosition(int row, int column)
        {
            // var spriteRenderer = tilePrefabSpriteRenderer;
            // Vector2 spriteSize = spriteRenderer.bounds.size;
            var spriteSize = GetTileSize(boardSettings.tilePrefab);
            float xSpacing = spriteSize.x * boardSettings.tileScale;
            float ySpacing = spriteSize.y * boardSettings.tileScale;

            return GetTileLocalSpawnPosition(row, column, xSpacing, ySpacing);
        }

        private Vector2 GetTileLocalSpawnPosition(int row, int column, float xSpacing, float ySpacing)
        {
            float xStartOffset = boardSettings.xOffset;
            float yStartOffset = boardSettings.yOffset + (ySpacing / 2f);
            float posX = (column * xSpacing) + xStartOffset;
            float posY = (row * ySpacing) + yStartOffset;

            return new Vector2(posX, posY);
        }

        private static IEnumerator DestroyOnSync(GameObject go)
        {
            yield return null;
            DestroyImmediate(go);
        }
        
        public BoardPosition ConvertToBoardPosition(Vector2 worldPos)
        {
            var localPosition = piecesParent.InverseTransformPoint(worldPos);
            var row = Mathf.RoundToInt(localPosition.y);
            var column = Mathf.RoundToInt(localPosition.x);
            return new BoardPosition(row, column);
        }

        private bool InRange(BoardPosition boardPos)
        {
            var row = boardPos.Row;
            var column = boardPos.Column;
            if (row < 0 || column < 0)
                return false;

            return !(row >= height) && !(column >= width);
        }

        [NotNull]
        public PieceObject GetPieceObject(BoardPosition position)
        {
            if (displayed == null)
                throw new ArgumentOutOfRangeException();

            return !InRange(position) ? throw new ArgumentOutOfRangeException() : displayed[position.Row][position.Column];
        }


        private void SyncBoard(Board board, bool destroyOld = true)
        {
            if (destroyOld)
                DestroyDisplayed();

            displayed = new PieceObject[board.rows][];

            for (var i = displayed.Length - 1; i >= 0; i--)
            {
                displayed[i] = new PieceObject[board.columns];
            }

            originalPositions.Clear();

            board.ComputeIfPresent((row, column) =>
            {
                var piece = SpawnPiece(GetTileLocalSpawnPosition(row, column), board.GetPiece(row, column));

                displayed[row][column] = piece;
                originalPositions[piece] = new BoardPosition(row, column);
            });

            displayBoard = CreateDummyBoard(board);
        }

        private PieceObject SpawnPiece(Vector2 localSpawnPosition, GamePiece piece)
        {
            // Debug.Log("Spawn Pos: " + localSpawnPosition);
            var obj = Instantiate(boardSettings.piecePrefab, Vector3.zero, Quaternion.identity, piecesParent);
            obj.transform.localPosition = localSpawnPosition;
            obj.transform.localScale = new Vector3(boardSettings.pieceScale, boardSettings.pieceScale, boardSettings.pieceScale);
            obj.SetActive(true);
            var pieceObj = obj.GetComponent<PieceObject>();
            pieceObj.Init(piece);
            return pieceObj;
        }

        private Board CreateDummyBoard(Board board)
        {
            var dummy = new Board(board.rows, board.columns, board.gamePieceGenerator);

            for (var row = 0; row < displayed.Length; row++)
            {
                for (var column = 0; column < displayed[row].Length; column++)
                {
                    dummy.SetPiece(row, column, displayed[row][column] ? displayed[row][column].Piece : null);
                }
            }

            return dummy;
        }

        private void SyncMainBoardToDisplay()
        {
            SyncBoard(mainBoard);
        }

        public IEnumerator HandleMatches(List<Match> matches, Action onComplete = null)
        {
            SyncBoard(displayBoard);
            OnMatch.Invoke(matches);

            yield return null;
            foreach (var match in matches)
            {
                foreach (var vector2Int in match.Tiles)
                {
                    var piece = displayed[vector2Int.Row][vector2Int.Column];
                    piece.transform.SetParent(null);
                    if (piece.TryGetComponent<PieceAnimator>(out var animator) && animator.isActiveAndEnabled)
                    {
                        animator.Match(() =>
                        {
                            Destroy(piece.gameObject);
                        });
                    }
                    else
                    {
                        Destroy(piece.gameObject);
                    }
                }
            }

            SyncMainBoardToDisplay();
        }

        public IEnumerator HandleFall(FallData fallData, Action onComplete = null)
        {
            if (TryGetComponent<PiecePhysicsController>(out var piecePhysics) && piecePhysics.isActiveAndEnabled)
            {
                yield return piecePhysics.Fall(fallData, this, (spawnPos, tile) =>
                {
                    var obj = SpawnPiece(spawnPos, mainBoard.GetPiece(tile.Row, tile.Column));
                    displayed[tile.Row][tile.Column] = obj;
                    return obj;
                });
            }

            SyncMainBoardToDisplay();
        }

        private void DestroyDisplayed()
        {
            for (var i = piecesParent.childCount - 1; i >= 0; i--)
            {
                Destroy(piecesParent.GetChild(i).gameObject);
            }

            displayed = null;
        }

        private bool VisualSwap(BoardPosition current, BoardPosition target)
        {
            var movements = new List<(BoardPosition from, BoardPosition to)>();
            // if (current == null || !displayBoard.InLine(clickedPiece, current))
            //     return false;
            // if (target == null || !displayBoard.InLine(clickedPiece, target))
            //     return false;
            //

            if (!displayBoard.Swap(current, target, Board.SwapType.ShiftTiles, movements, checkIfMatches: false))
                return false;
         
            foreach (var (from, to) in movements)
            {
                var toPiece = GetPieceObject(to);
                var fromPiece = GetPieceObject(from);


                toPiece.AnimateUpdatePosition(GetTileLocalSpawnPosition(from.Row, from.Column));
                displayed[to.Row][to.Column] = fromPiece;

                fromPiece.AnimateUpdatePosition(GetTileLocalSpawnPosition(to.Row, to.Column));
                displayed[from.Row][from.Column] = toPiece;
            }

            OnVisualSwap.Invoke();
            
            dragObject.Highlight(false);

            foreach (var pieceComponentse in displayed)
            {
                foreach (var pieceComponent in pieceComponentse)
                {
                    if (!pieceComponent)
                        continue;
                    pieceComponent.Highlight(false);
                }
            }

            foreach (var match in displayBoard.FindMatches(clear: false))
            {
                foreach (var tile in match.Tiles)
                {
                    var hoveredTile = target;
                    if (hoveredTile.Row == tile.Row && hoveredTile.Column == tile.Column)
                    {
                        dragObject.Highlight(true);
                    }

                    var p = GetPieceObject(new BoardPosition(tile.Row, tile.Column));
                    p.Highlight(true);
                }
            }

            return true;
        }

        private IEnumerator WaitForPiecesToAnimateThenExecute(Action onComplete)
        {
            
            for (var i = 0; i < displayed.Length; i++)
            {
                for (int j = 0; j < displayed[i].Length; j++)
                {
                    var piece = displayed[i][j];
                    yield return new WaitUntil(() => !piece.Animating);
                }
            }
            onComplete();
        } 
        
        private void RestoreBoard(BoardPosition ignore = null)
        {
            if (dragObject)
            {
                dragObject.Highlight(false);
                if (originalPositions.TryGetValue(dragObject, out var originalPosition))
                {
                    dragObject.transform.localPosition = new Vector2(originalPosition.Column, originalPosition.Row);
                }
            }

            // ignore the dragObject
            foreach (var (piece, pos) in originalPositions)
            {
                displayed[pos.Row][pos.Column] = piece;
                displayBoard.SetPiece(pos.Row, pos.Column, piece.Piece);
                piece.AnimateUpdatePosition(GetTileLocalSpawnPosition(pos.Row, pos.Column));
                piece.Highlight(false);
            }
        }

        public void OnDrag(BoardPosition current, BoardPosition last, Vector2 mousePosition)
        {
            if (ResolvingMove || !Interactable)
                return;
            if (dragObject == null)
                return;
            
            // add drag here
            
            if (clickedTile == null)
                return;

            if (!animateDrag)
                return;
            
            var updated = current != last;
            if (!updated)
                return;
            
            // todo add drag

            if (current != null && displayBoard.InLine(clickedTile, current))
            {
            }
            else
            {
                RestoreBoard();
                return;
            }

            var origin = clickedTile;

            var from = origin;

            if (last != null && displayBoard.InLine(origin, last))
                from = last;

            if (!VisualSwap(from, current))
                RestoreBoard();
        }

        public void OnDragStart(BoardPosition current)
        {
            if (ResolvingMove || !Interactable)
                return;
            if (current == null)
                return;

            clickedTile = current;
            var piece = GetPieceObject(clickedTile);

            var clone = Instantiate(piece!.gameObject, null);
            dragObject = clone.GetComponent<PieceObject>();
            dragObject.transform.position = piece.transform.position;
            dragObject.transform.rotation = piece.transform.rotation;

            dragObject.GetComponent<SpriteRenderer>().sortingOrder = 99;
            piece.gameObject.SetActive(false);
        }


        public void OnDragEnd(BoardPosition currentHoveredTile)
        {
            if (ResolvingMove || !Interactable)
                return;
            if (clickedTile == null || dragObject == null)
                return;
            var isValidMove = currentHoveredTile != null && mainBoard.InLine(clickedTile, currentHoveredTile) &&
                              mainBoard.GetEstimatedMatches(clickedTile, currentHoveredTile).Count > 0;

            if (!isValidMove)
            {
                RestoreBoard(clickedTile);
                var obj = GetPieceObject(clickedTile).gameObject;
                obj.SetActive(false);
                LeanTween.cancel(obj);
                var target = piecesParent.TransformPoint(GetTileLocalSpawnPosition(clickedTile.Row, clickedTile.Column));
                obj.transform.position = target;

                var temp = dragObject.gameObject;

                LeanTween.move(temp, target, 0.2f).setOnComplete(() =>
                {
                    obj.SetActive(true);
                    Destroy(temp);
                });
            }
            else
            {
                Destroy(dragObject.gameObject);
                ResolvingMove = true;
                OnMoveStart.Invoke();
                StartCoroutine(mainBoard.Move(clickedTile, currentHoveredTile, Board.SwapType.ShiftTiles, () =>
                {
                    OnMoveResolved.Invoke();
                    ResolvingMove = false;
                }));

                // mainBoard.Swap(clickedTile, currentHoveredTile, Board.SwapType.ShiftTiles);
                // SyncToMainBoard()
                // StartCoroutine(ResolveMove());
            }

            dragObject = null;
            clickedTile = null;
        }

        public IEnumerator HandleSwap(Action onComplete = null)
        {
            SyncMainBoardToDisplay();
            yield return null;
        }

        public bool ResolvingMove { get; private set; }

        // private IEnumerator ResolveMove()
        // {
        //     ResolvingMove = true;
        //     
        //     var initialMatches = mainBoard.FindMatches();
        //     if (initialMatches.Count > 0)
        //     {
        //         
        //         // OnMatch.Invoke(initialMatches);
        //         yield return HandleMatches(initialMatches);
        //
        //         FallData fallData;
        //         while ((fallData = mainBoard.Fall()).tilesToFill.Count > 0)
        //         {
        //             foreach (var vector2Int in fallData.tilesToFill)
        //             {
        //                 mainBoard.SetPiece(vector2Int.y, vector2Int.x, mainBoard.Generate());
        //             }
        //         
        //             yield return HandleFall(fallData);
        //             
        //             var matches = mainBoard.FindMatches();
        //             if (matches.Count == 0)
        //                 continue;
        //
        //             // OnMatch.Invoke(matches);
        //             yield return HandleMatches(matches);
        //         }
        //     }
        //     
        //     ResolvingMove = false;
        //     OnMoveResolved.Invoke();
        // }

        public abstract BoardPosition GetTileFromPoint(Vector3 point);

        [ContextMenu("generate")]
        public void GenerateBoard()
        {
            GenerateBoard(pieceGenerator: null);
        }
        
        public void GenerateBoard(IGamePieceGenerator pieceGenerator = null)
        {
            if (pieceGenerator == null)
            {
                pieceGenerator = GetComponent<IGamePieceGenerator>();
            }

            if (pieceGenerator == null)
            {
                throw new Exception();
            }
            
            var board = new Board(height, width, pieceGenerator);
            board.GenerateBoard();
            mainBoard = board;
            mainBoard.Handler = this;
            SyncMainBoardToDisplay();
        }

        public void Clear()
        {
            mainBoard = null;
            displayed = null;
            displayBoard = null;
            DestroyDisplayed();
        }

        private BoardPosition currentHoverTile;


        public bool manualUpdate = false;

        private void Update()
        {
            if (manualUpdate)
                return;
            
            Tick();
        }

        public void Tick()
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 screenPoint = screenPosition;
            screenPoint.z = Mathf.Abs(Camera.main.transform.position.z);

            var point = Camera.main.ScreenToWorldPoint(screenPoint);
            
            var lastHoverTile = currentHoverTile;
            currentHoverTile = GetTileFromPoint(point);
            
            // Debug.Log("Tile: " + curentHoverPiece);
                
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnDragStart(currentHoverTile);
            }
        
            if (Mouse.current.leftButton.isPressed)
            {
                OnDrag(currentHoverTile, lastHoverTile, point);
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                OnDragEnd(currentHoverTile);
            }
        }

        public void SetPiece(int row, int column, GamePiece pieces)
        {
            mainBoard.SetPiece(row, column, pieces);
            SyncMainBoardToDisplay();
        }

        public void CheckMatches()
        {
            StartCoroutine(mainBoard.Matches());
        }
    }
}
}