using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Schema;
using UnityEngine;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{

    /*
     * This class handles playing games in an interactive way, and provides some handy help
     * for customising a game or quickly testing something. It's used in one of the example scenes.
     * It's a little tangled, because it's quite a centrally important class.
    */

    public enum AGENT_TYPE {MCTS, Conservative, Greedy, Random};

    [Header("AI")]
    public bool aiOpponent = false;
    [Header("Allotted AI turn taking time in seconds", order = 1)]
    [Space(-10, order = 2)]
    [Header("Longer time leads to better evaluations for Greedy and MCTS", order = 3)]
    public float TimeAllottedPerTurn = 1f;
    [Header("AI Versus Mode")]
    public bool aiVersus = false;
    public AGENT_TYPE agentOne;
    public AGENT_TYPE agentTwo;
    BaseAgent botPlayer1;
    BaseAgent botPlayer2;
    public float timeBetweenActions = 1f;
    [Header("Loading Games")]
    public bool loadFromString = false;
    [TextArea(3, 10)]
    public string stringToLoad = @"BOARD 3 5
FALL DOWN
WIN COUNT 10
LOSE MATCH LINE 3";
    [Header("Prefabs")]
    public BoardPiece templateBoardPiece;
    public PlayerPiece templatePlayerPiece;
    [Header("Colors")]
    public Color backgroundColor;
    public Color boardOffColor;
    public Color boardOnColor;
    public Color playerOneColor;
    public Color playerOneAccent;
    public Color playerTwoColor;
    public Color playerTwoAccent;
    [Header("User Interface")]
    public TMPro.TextMeshProUGUI endText;
    public TMPro.TextMeshProUGUI rulesText;
    public TMPro.TextMeshProUGUI player1Text;
    public TMPro.TextMeshProUGUI player2Text;
    public UnityEngine.UI.Image player1Image;
    public UnityEngine.UI.Image player2Image;
    public TMPro.TextMeshProUGUI agent1Text;
    public TMPro.TextMeshProUGUI agent2Text;
    MCTSAgent npcPlayer;

    //Model data
    Game currentGame;
    bool gameOver = false;
    PlayerPiece[,] pieces;

    void Start(){
        if(loadFromString){
            currentGame = Game.FromCode(stringToLoad);
            SetupBoard(currentGame);
        }
    }

    public void SetupBoard(Game game){
        currentGame = game;
        
        Camera.main.backgroundColor = backgroundColor;

        pieces = new PlayerPiece[game.boardWidth, game.boardHeight];

        GameObject board = new GameObject("Board");

        //! Render the board. By default we start at (0,0) and reposition the camera after
        BoardPiece boardPiece;
        for(int i=0; i<game.boardWidth; i++){
            for(int j=0; j<game.boardHeight; j++){
                boardPiece = Instantiate(templateBoardPiece);

                boardPiece.x = i; boardPiece.y = j;
                boardPiece.transform.position = new Vector3(i,j,1);
                if((i+j)%2 == 0)
                    boardPiece.sprite.color = boardOffColor;
                else
                    boardPiece.sprite.color = boardOnColor;

                boardPiece.transform.parent = board.transform;
            }
        }

        player1Text.color = playerOneColor;
        player1Image.color = playerOneColor;
        player2Text.color = playerTwoColor;
        player2Image.color = playerTwoColor;

        if (rulesText != null)
        {
            //Setup the little sidebar user interface bit
            rulesText.text = game.GameToString();    
        }
        
        if(aiVersus && agent1Text != null && agent2Text != null){
            //If it's an AI game, set up the little labels
            agent1Text.text = agentOne.ToString()+" Agent";
            agent2Text.text = agentTwo.ToString()+" Agent";
        }

        //Set camera centre
        float cx = -0.5f + (float)game.boardWidth/2f; 
        float cy = -0.5f+(float)game.boardHeight/2f;
        Camera.main.transform.position = new Vector3(cx, cy, -10);

        //Set zoom
        //With thanks to: https://pressstart.vip/tutorials/2018/06/6/37/understanding-orthographic-size.html
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = game.boardWidth / game.boardHeight;

        if(screenRatio >= targetRatio){
            Camera.main.orthographicSize = 1.5f+(game.boardHeight / 2);
        }
        else{
            float differenceInSize = targetRatio / screenRatio;
            Camera.main.orthographicSize = 0.5f+(game.boardWidth / 2 * differenceInSize);
        }

        //Attach our view to the game itself
        game.playableGame = this;
        game.interactiveMode = true;

        if(aiOpponent){
            npcPlayer = new MCTSAgent(2);
        }

        //Again, very messy, a lot of the UI code especially is rushed here.
        if(aiVersus){
            switch(agentOne){
                case AGENT_TYPE.Greedy:
                    botPlayer1 = new GreedyAgent(1); break;
                case AGENT_TYPE.Random:
                    botPlayer1 = new RandomAgent(1); break;
                case AGENT_TYPE.MCTS:
                    botPlayer1 = new MCTSAgent(1); break;
                case AGENT_TYPE.Conservative:
                    botPlayer1 = new MCTSAgent(1){rolloutLength = 2}; break;
            }
            switch(agentTwo){
                case AGENT_TYPE.Greedy:
                    botPlayer2 = new GreedyAgent(2); break;
                case AGENT_TYPE.Random:
                    botPlayer2 = new RandomAgent(2); break;
                case AGENT_TYPE.MCTS:
                    botPlayer2 = new MCTSAgent(2); break;
                case AGENT_TYPE.Conservative:
                    botPlayer2 = new MCTSAgent(2){rolloutLength = 2}; break;
            }

            StartCoroutine(VersusMode());
        }
    }
    
    //Coroutines let you run code asynchronously in Unity (i.e. while other code is running)
    //This lets you, for instance, tell a bit of code to run in the background while the game
    //keeps going. We use it here to start a process that alternates asking each AI to take a
    //turn, and then waits for a second so the viewer (that's you) can see what happens.
    IEnumerator VersusMode(){
        //No humans allowed
        allowInteraction = false;
        while(true){
            botPlayer1.TakeTurn(currentGame, TimeAllottedPerTurn);
            yield return new WaitForSeconds(timeBetweenActions);
            yield return new WaitUntil(()=> !animatingAction);

            if(currentGame.endStatus != 0)
                break;

            botPlayer2.TakeTurn(currentGame, TimeAllottedPerTurn);
            yield return new WaitForSeconds(timeBetweenActions);
            yield return new WaitUntil(()=> !animatingAction);

            if(currentGame.endStatus != 0)
                break;
        }
        SetEndState();
        Debug.Log("done");
    }

    public void TapTile(int x, int y){
        if(!gameOver && allowInteraction && currentGame.TapAction(x, y)){
            if(currentGame.endStatus != 0){
                SetEndState();
            }
            else if(aiOpponent){
                StartCoroutine(PauseForOpponent());
            }
        }
        else if(gameOver){
            ResetGame();
        }
    }

    bool allowInteraction = true;

    IEnumerator PauseForOpponent(){
        allowInteraction = false;

        //Wait briefly for the AI to take their turn
        //Brendon Chung tweeted this lovely breakdown of the perception of time 
        //as like a rule of thumb, and it's stuck with me - it's easy to overestimate
        //https://twitter.com/BlendoGames/status/1105587512297185280
        yield return new WaitForSeconds(0.3f);

        npcPlayer.TakeTurn(currentGame, TimeAllottedPerTurn);
        if(currentGame.endStatus != 0){
            SetEndState();
        }

        allowInteraction = true;
    }

    private bool animatingAction;
    private Queue<Action> actionQueue = new Queue<Action>();
    
    IEnumerator PauseForAnimation()
    {
        allowInteraction = false;
        
        //First action will usually be the AddPiece action, so we'll pause for half a second after that. 
        var firstAction = true;
        
        do
        {
            var action = actionQueue.Dequeue();
            action.Invoke();

            if (firstAction)
            {
                firstAction = false;
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);    
            }
        } while (actionQueue.Count > 0);

        yield return new WaitForSeconds(0.3f);
        animatingAction = false;
        allowInteraction = true;
    }
    
    public void ResetGame(){
        gameOver = false;
        Color c = endText.color;
        c.a = 0f;
        endText.color = c;
        currentGame.ResetState();
        for(int i=0; i<pieces.GetLength(0); i++){
            for(int j=0; j<pieces.GetLength(1); j++){
                if(pieces[i,j] == null) continue;
                Destroy(pieces[i,j].gameObject);
                pieces[i,j] = null;
            }
        }

        if (aiVersus)
        {
            StartCoroutine(VersusMode());
        }
    }

    public void SetEndState(){
        gameOver = true;
        //Show the text - incidentally I recommend DOTween for this kind of thing, but 
        //this isn't an important system so we're just going to make it non-transparent
        Color c = endText.color;
        c.a = 1f;
        endText.color = c;
        //Set it properly
        if(currentGame.endStatus < Game.END_STATUS_DRAW){
            endText.text = "Player "+currentGame.endStatus+" Wins!";
        }
        else{
            //I guess you'd call it a "draw", tie game feels like an American way of putting it?
            endText.text = "Tie Game!";
        }
    }

    public void QueueAddPiece(int x, int y, int player)
    {
        actionQueue.Enqueue(()=>AddPiece(x,y, player));
        
        if (!animatingAction)
        {
            animatingAction = true;
            StartCoroutine(PauseForAnimation());
        }
    }
    
    public void AddPiece(int x, int y, int player){
        Debug.Log("adding: "+x+","+y);
        PlayerPiece p = Instantiate(templatePlayerPiece);
        if(player == 0){
            p.mainSprite.color = playerOneColor;
            p.outlineSprite.color = playerOneAccent;
        }
        else{
            p.mainSprite.color = playerTwoColor;
            p.outlineSprite.color = playerTwoAccent;
        }

        pieces[x,y] = p;

        p.transform.position = new Vector3(x, y, 1);

        p.SetShape(player);
        //GFEE.Instance.TriggerCustomEvent("AddPiece"+Player.id, gameObject, pos, direction);
    }

    public void QueueMovePiece(int fx, int fy, int tx, int ty)
    {
        actionQueue.Enqueue(()=>MovePiece(fx,fy, tx, ty));
        
        if (!animatingAction)
        {
            animatingAction = true;
            StartCoroutine(PauseForAnimation());
        }
    }
    
    //! Board update stuff
    public void MovePiece(int fx, int fy, int tx, int ty){
        Debug.Assert(pieces[fx,fy] != null);
        Debug.Assert(pieces[tx,ty] == null);
        //! DOTween is a really nice plugin for tweening and more! 
        pieces[fx,fy].transform.DOMove(new Vector3(tx, ty, 0), 0.3f);
        pieces[tx,ty] = pieces[fx,fy];
        pieces[fx,fy] = null;
    }
    
    public void QueueDeletePiece(int x, int y)
    {
        actionQueue.Enqueue(()=>DeletePiece(x,y));
        
        if (!animatingAction)
        {
            animatingAction = true;
            StartCoroutine(PauseForAnimation());
        }
    }

    public void DeletePiece(int x, int y){
        Debug.Assert(pieces[x,y] != null);

        PlayerPiece p = pieces[x,y];
        //Clear the entry in the array
        pieces[x,y] = null;
        //Little animation to make it go tiny, and then destroy it
        p.transform.DOScale(Vector3.zero, 0.25f).OnComplete(() => Destroy(p.gameObject));
    }

    public void QueueFlipPiece(int x, int y, int newPlayerCode)
    {
        actionQueue.Enqueue(()=>FlipPiece(x,y,newPlayerCode));
        
        if (!animatingAction)
        {
            animatingAction = true;
            StartCoroutine(PauseForAnimation());
        }
    }
    
    public void FlipPiece(int x, int y, int newPlayerCode){
        Debug.Assert(pieces[x,y] != null);

        PlayerPiece p = pieces[x,y];

        var punchTween = p.transform.DOScale(new Vector3(0, 1.5f, 1f), 0.15f);
        // //After the punch make sure it resets to the original scale.
        punchTween.OnComplete(() =>
        {
            if(newPlayerCode == 0){
                p.mainSprite.color = playerOneColor;
                p.outlineSprite.color = playerOneAccent;
            }
            else{
                p.mainSprite.color = playerTwoColor;
                p.outlineSprite.color = playerTwoAccent;
            }
            p.SetShape(newPlayerCode);
            
            p.transform.DOScale(Vector3.one, 0.15f);
        });
    }


    //! Singleton pattern I use a lot. When this object wakes up in a scene, it 
    //! sets itself as the static reference to BoardManager. Very handy if a bit clunky.
    public static BoardManager instance;
    void Awake(){
        BoardManager.instance = this;
    }

}
