using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}
public class board : MonoBehaviour
{

    [Header("Pretty")]

    [SerializeField] private Material tileMat;


    [SerializeField] private Material hoverMat;

    [SerializeField] private Material WhiteHover;

    [SerializeField] private Material ACTIVE_HOVER;

    [SerializeField] private Material AvalMovesMat;

    [SerializeField] private float TILE_SIZE = 1.0f;

    [SerializeField] private float yOffset = 1.0f; 

    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    private Vector3 bounds;
    [SerializeField] private GameObject victoryScreen;

    public TextMeshProUGUI victoryText;

    [Header("Prefabs and Mats")]

    [SerializeField] private GameObject[] prefab;
    [SerializeField] private Material[] mats;



    //Game logic
    private ChessPiece ACTIVE;
    private ChessPiece[,] chessPieces;


    private List<Vector2Int> avalMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhite = new List<ChessPiece>();
    private List<ChessPiece> deadBlack = new List<ChessPiece>();


    private const int TILE_C_X = 8;
    private const int TILE_C_Y = 8;
    private GameObject[,] tiles; //2 dem array 

    private Camera currentCamera;
    private Vector2Int currentHover;
    private bool end = false;
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> movesList = new List<Vector2Int[]>();
    public void Awake()
    {
        isWhiteTurn = true;
        GenrateGrid(TILE_SIZE, TILE_C_X, TILE_C_Y);

        //test

        SpawnAllPieces();
        PositionAllPieces();
    }

    private void GenrateGrid(float tileSize, int tileCountX, int tileCountY)
    {  //generate grid
        yOffset += transform.position.y; //offset for the board
        bounds = new Vector3((tileCountX /2) * tileSize, 0, (tileCountY / 2) * tileSize) + boardCenter; //center of the board
        tiles = new GameObject[tileCountX, tileCountY]; //2 dem array
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenrateTile(tileSize, x, y); //call genrate tile
    }

    private void Update()
    {
        if (!currentCamera)
        { //if no camera is set
            currentCamera = Camera.main;  //set the main camera
            return;
        }
        // foreach (GameObject tile in tiles) //loop through all tiles **this is terrible code**
        //     tile.GetComponent<Renderer>().material = tileMat; //set material to default

        if (!end)
        {
            RaycastHit info;
            Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out info, 100))
            { //if raycast hits a tile
              //get index of hit tile
                Vector2Int hitPostion = LookTileIndex(info.transform.gameObject); //get index of hit tile

                if (currentHover == -Vector2Int.one)
                { //if no hover
                    currentHover = hitPostion; //set hover to hit tile
                    tiles[hitPostion.x, hitPostion.y].GetComponent<Renderer>().material = hoverMat;
                }
                if (currentHover != hitPostion)
                {
                    tiles[currentHover.x, currentHover.y].GetComponent<Renderer>().material = tileMat;
                    currentHover = hitPostion; //set new hover to hit tile
                    tiles[hitPostion.x, hitPostion.y].GetComponent<Renderer>().material = hoverMat;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (chessPieces[hitPostion.x, hitPostion.y] != null)
                    { //if there is a piece
                        UnhighlightTiles();
                        if ((chessPieces[hitPostion.x, hitPostion.y].team == 0 && isWhiteTurn) || (chessPieces[hitPostion.x, hitPostion.y].team == 1 && !isWhiteTurn))
                        { //is turn
                            ACTIVE = chessPieces[currentHover.x, currentHover.y]; //set active
                            tiles[hitPostion.x, hitPostion.y].GetComponent<Renderer>().material = ACTIVE_HOVER;
                            avalMoves = ACTIVE.GetAvalMoves(ref chessPieces, TILE_C_X, TILE_C_Y); //get aval moves

                            specialMove = ACTIVE.GetSpecialMove(ref chessPieces, ref movesList, ref avalMoves);

                            HighlightTiles(); //highlight aval moves
                        }
                    }
                    else
                    {
                        UnhighlightTiles();
                    }
                }


                if (ACTIVE != null && Input.GetMouseButtonUp(0))
                { //mouse release 
                    Vector2Int prevPos = new Vector2Int(ACTIVE.currX, ACTIVE.currY); //prev pos
                    Debug.Log("prevPos: " + prevPos);
                    bool validMove = MoveTo(ACTIVE, hitPostion.x, hitPostion.y); //move piece
                    if (!validMove)
                    { //if move is invalid
                        ACTIVE.SetPosition(GetTileCenter(prevPos.x, prevPos.y)); //move piece back
                        ACTIVE = null; //reset active
                    }
                    else
                    {
                        ACTIVE = null; //reset active
                    }
                }
                if (end)
                {
                    //set material of all tiles to tilemat
                    for (int x = 0; x < TILE_C_X; x++)
                        for (int y = 0; y < TILE_C_Y; y++)
                            tiles[x, y].GetComponent<Renderer>().material = tileMat;
                }

            }
            else
            {
                if (currentHover != -Vector2Int.one)
                {
                    foreach (GameObject tile in tiles) //loop through all tiles **this is terrible code**
                        tile.GetComponent<Renderer>().material = tileMat;
                    currentHover = -Vector2Int.one;
                }

                if (ACTIVE && Input.GetMouseButtonUp(0))
                {
                    ACTIVE.SetPosition(GetTileCenter(ACTIVE.currX, ACTIVE.currY));
                    ACTIVE = null;
                }
            }

            // set tiles with chess pieces to have a WhiteTile matriel

            //if dragging piece 
            if (ACTIVE)
            {
                Plane horzPlan = new Plane(Vector3.up, Vector3.up);
                float distance = 0.0f;
                if (horzPlan.Raycast(ray, out distance))
                {
                    ACTIVE.SetPosition(ray.GetPoint(distance));
                }
            }
        }
    }

    //board gen
    private GameObject GenrateTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("x:{0}, y{1}", x, y)); //prints values
        tileObject.transform.parent = transform; //set tile transform to board transform 

        Mesh mesh = new Mesh(); //create mesh
        tileObject.AddComponent<MeshFilter>().mesh = mesh;  //add mesh filter
        tileObject.AddComponent<MeshRenderer>().material = tileMat;  //add mesh renderer

        //init verts for tiles
        Vector3[] verts = new Vector3[4];  //4 verts
        verts[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds; //top left
        verts[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds; //bottom left
        verts[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds; //top right
        verts[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds; //bottom right

        int[] triangle = new int[] { 0, 1, 2, 1, 3, 2 };  //triangles for mesh 

        //set
        mesh.vertices = verts; //set verts
        mesh.triangles = triangle; //set triangles
        mesh.RecalculateNormals(); //recalculate normals
        tileObject.layer = LayerMask.NameToLayer("Tile"); //set layer to tile
        tileObject.AddComponent<BoxCollider>(); //aprl box collier is more ef then a plane idk why i didn't make the rules your mom did
        return tileObject;
    }

    // Spawn Piece
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_C_X, TILE_C_Y];
        int white = 0;
        int black = 1;

        //thank you copilot i love you 
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, white);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, white);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, white);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, white);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, white);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, white);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, white);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, white);

        for (int i = 0; i < TILE_C_X; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, white);

        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, black);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, black);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, black);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, black);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, black);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, black);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, black);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, black);



        for (int i = 0; i < TILE_C_X; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, black);
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {  //spawn single piece

        ChessPiece piece = Instantiate(prefab[(int)type - 1], transform).GetComponent<ChessPiece>(); //instantiate and get piece
        piece.type = type;
        piece.team = team;
        piece.GetComponent<MeshRenderer>().material = mats[team]; //set material
        // piece.GetComponent<MeshRenderer>().material = teamMaterials[((team == 0) ? 0 : 6)] + ((int)type - 1)];
        return piece;
    }

    private void PositionAllPieces()
    { //position all pieces
        for (int x = 0; x < TILE_C_X; x++)
            for (int y = 0; y < TILE_C_Y; y++)
                if (chessPieces[x, y] != null)
                    PostionSinglePiece(x, y, true);
    }

    private void PostionSinglePiece(int x, int y, bool force = false)
    { //position single piece
        chessPieces[x, y].currX = x;
        chessPieces[x, y].currY = y;
        // chessPieces[x,y].transform.position = new Vector3(x * TILE_SIZE, 0, y * TILE_SIZE);
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force); //get tile center

    }

    private Vector3 GetTileCenter(int x, int y)
    { //get tile center
        Vector3 halfSize = Vector3.one * (TILE_SIZE / 2); //get half size
        Vector3 tilePos = new Vector3(x, -.4f, y); //get tile pos
        return tilePos + halfSize; //return tile pos + half size
    }
    //ops
    private Vector2Int LookTileIndex(GameObject hitInfo)
    { //look tile index
        for (int x = 0; x < TILE_C_X; x++)
            for (int y = 0; y < TILE_C_Y; y++)
                if (tiles[x, y] == hitInfo) //if tile is hit
                    return new Vector2Int(x, y); //return tile index

        return -Vector2Int.one; //return -1
    }

    private void HighlightTiles()
    {
        for (int i = 0; i < avalMoves.Count; i++)
            tiles[avalMoves[i].x, avalMoves[i].y].GetComponent<MeshRenderer>().material = AvalMovesMat;
    }

    private void UnhighlightTiles()
    {
        for (int i = 0; i < avalMoves.Count; i++)
            tiles[avalMoves[i].x, avalMoves[i].y].GetComponent<MeshRenderer>().material = tileMat;
        avalMoves.Clear();
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        return false;
    }

    private bool MoveTo(ChessPiece ACTIVE, int x, int y)
    { //move to
        if (!ContainsValidMove(ref avalMoves, new Vector2Int(x, y)))
        {
            return false;
        }
        Vector2Int prevPos = new Vector2Int(ACTIVE.currX, ACTIVE.currY); //get prev pos
        //is there another piece on target pos
        if (chessPieces[x, y] != null)
        { //if there is a piece
            ChessPiece ocp = chessPieces[x, y]; //get other piece

            if (ACTIVE.team == ocp.team) //if same team
                return false; //return false

            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                    checkMate(1);

                deadWhite.Add(ocp);
                Destroy(ocp.gameObject);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                    checkMate(0);
                deadBlack.Add(ocp);
                Destroy(ocp.gameObject);
            }
        }
        chessPieces[x, y] = ACTIVE; //set piece
        chessPieces[prevPos.x, prevPos.y] = null; //set prev pos to null

        PostionSinglePiece(x, y); //position piece

        isWhiteTurn = !isWhiteTurn; //change turn
        // change all tiles to tile mat
        for (int penis = 0; penis < TILE_C_X; penis++)
            for (int pp = 0; pp < TILE_C_Y; pp++)
                tiles[penis, pp].GetComponent<MeshRenderer>().material = tileMat;
        movesList.Add(new Vector2Int[] { prevPos, new Vector2Int(x, y) }); //add move to list

        ProcessSpecialMove();

        return true;

    }

    //Special Moves
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = movesList[movesList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPos = movesList[movesList.Count - 2];
            ChessPiece targetPawn = chessPieces[targetPawnPos[1].x, targetPawnPos[1].y];

            if (myPawn.currX == targetPawn.currX)
            {
                if (myPawn.team == 0)
                {
                    deadWhite.Add(targetPawn);
                    Destroy(targetPawn.gameObject);
                }
                else
                {
                    deadBlack.Add(targetPawn);
                    Destroy(targetPawn.gameObject);
                }
            }

            chessPieces[targetPawn.currX, targetPawn.currY] = null;
        }
        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = movesList[movesList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if (targetPawn.type == ChessPieceType.Pawn)
            {
                if (targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PostionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
                if (targetPawn.team == 0 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PostionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }
        if (specialMove == SpecialMove.Castling)
        {

            Vector2Int[] lastMove = movesList[movesList.Count - 1];

            //Left
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) // White side
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PostionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;

                }
                if (lastMove[1].y == 7) // Black side
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PostionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }

            if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // White side
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PostionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                if (lastMove[1].y == 7) // Black side
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PostionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
        if (specialMove == SpecialMove.Promotion)
        {

        }
    }

    //Checkmate
    private void checkMate(int team)
    {
        end = true;
        DisplayVictory(team);
    }

    private void DisplayVictory(int team)
    {
        victoryScreen.SetActive(true);
        if (team == 0)
            victoryText.text = "Black Wins!";
        else
            victoryText.text = "White Wins!";

    }

    public void onExitButton()
    {
        Application.Quit();
        Debug.Log("Exit");
    }
}

