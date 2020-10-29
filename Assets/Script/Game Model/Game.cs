using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
    This is where we capture the game logic. This is sort of like a game engine - it 
    contains logic for code that may not be in a particular game, so it can represent
    any game in our design space. Remember, this is just one way to do it! 
*/

//These are enums used for defining bits of the game. You'll see them used elsewhere.
public enum Heading{UP, RIGHT, DOWN, LEFT};
public enum Direction{LINE, ROW, COL, CARDINAL};
public enum Player{CURRENT, OPPONENT, ANY};
public enum TriggeredEffect {DELETE, FLIP, CASCADE};

//This is a handy little class I make so I can quickly bundle two integers together in one object.
//There's probably a nicer way of doing this in C#, this is just a quick solution.
//A 'struct' is like a class but it has a few restrictions in exchange for taking up less memory.
public struct Point{
    public int x; public int y;
    public Point(int _x, int _y){
        this.x = _x; this.y = _y;
    }

    public override bool Equals(object obj)
    {
        if (obj is Point p)
        {
            return p.x == x && p.y == y;
        }
        
        return false;
    }
}

//Finally, an answer to the question: what is a game?
public class Game
{
    //This is a little variable that holds the score for this game. This is useful for algorithms
    //that generate games, because they can remember what score this game got previously to avoid
    //having to re-evaluate it. You can see this used in the Scrappy Game Generator.
    public float evaluatedScore = 0f;

    //Game data    
    public int boardWidth;
    public int boardHeight;

    //Game logic
    public Condition winCondition;
    public Condition loseCondition;
    public List<Effect> updatePhase;
    
    //Deciding how to flag wins always gets me. For this I've gone for:
    //1 = Player 1 win
    //2 = Player 2 win
    //3 = Draw
    //This is nice because the player number is also the win flag.
    public int endStatus = 0;
    public const int END_STATUS_DRAW = 3;

    //A reference to the game state, which we can switch around when we're testing
    public GameState state;

    //A reference to the interactive game UI. Normally I wouldn't weld them together
    //so closely, but for the purposes of this example it's a bit easier to read.
    public BoardManager playableGame;
    //If true, it means we're playing a game with visuals and interaction.
    public bool interactiveMode = false;

    public Game(int w, int h){
        boardWidth = w;
        boardHeight = h;
        state = new GameState(w,h);
        updatePhase = new List<Effect>();
    }

    public int CurrentPlayer(){
        return state.currentPlayer;
    }

    public bool TapAction(int x, int y){
        //! Here we make sure that there's no piece here already before accepting the tap
        //! If you wanted to expand the AGD system to allow for tapping pieces, you'd change this
        if(state.Value(x, y) == 0){
            //! By default, tapping an empty space spawns our piece in
            state.Set(x, y, Player.CURRENT);
            //! If we're in interactive mode, we notify the board that we've placed a piece
            if(interactiveMode){
                playableGame.AddPiece(x, y, CurrentPlayer()-1);
            }
            //! Automatically end the turn after a player has placed a piece
            //! Again, if you wanted to allow multiple actions, you'd change this.
            EndTurn();
            return true;
        }
        else{
            return false;
        }
    }

    public void EndTurn(){
        //! First we apply any update game logic
        foreach(Effect e in updatePhase){
            e.Apply(this);
        }

        //! Then we check for win conditions.
        //! Note that ordering here is quite subtle - checking for win or loss first, 
        //! checking for active player vs opposing player. Swapping these changes many games.
        //! We opt to not make this variable here, and instead do the following:

        // 1. Has the current player won?
        if(winCondition.Check(this, Player.CURRENT)){
            endStatus = state.GetPlayerValue(Player.CURRENT);
        }
        // 2. Has the other player won?
        if(winCondition.Check(this, Player.OPPONENT)){
            if(endStatus == state.GetPlayerValue(Player.CURRENT))
                endStatus = END_STATUS_DRAW;
            else
                endStatus = state.GetPlayerValue(Player.OPPONENT);
        }
        //! Note that this means that someone can win and lose in the same turn, and we
        //! count it as a win (because we only check for losing if endStatus == 0).
        //! You might prefer to have losing take precedence over winning.
        // 3. If there are loss conditions, has the other player lost?
        if(endStatus == 0 && loseCondition != null){
            if(loseCondition.Check(this, Player.OPPONENT)){
                endStatus = state.GetPlayerValue(Player.CURRENT);
            }
            // 4. Or has the current player lost?
            if(loseCondition.Check(this, Player.CURRENT)){
                if(endStatus == state.GetPlayerValue(Player.CURRENT))
                    endStatus = END_STATUS_DRAW;
                else
                    endStatus = state.GetPlayerValue(Player.OPPONENT);
            }
        }

        //! Note that we don't model multiple win conditions, but if you did you'd need to think
        //! about whether they all need to be true (conjunction) or just one of them (disjunction).
        //! There's lots of variation you could try in this area, but think about how it affects
        //! the size, density and traversibility of your design space.

        //? Remember to update the current player!
        state.AdvancePlayerOrder();
    }

    /*
    * Here I put anything that seems useful to lots of different bits of the codebase.
    * For example, I can imagine adding a lot of rule components that want to check for
    * lines of pieces.
    */

    public List<Point> FindLines(Direction direction, int length, Player p, bool checkOnly = false, bool checkFromLastPos = false){
        int cValue = 0;
        List<Point> matchList = new List<Point>();
        List<Point> results = new List<Point>();
        for(int i=0; i<boardWidth; i++){
            for(int j=0; j<boardHeight; j++){
                cValue = state.Value(i, j);
                //Check for lines in the specified directions
                if(cValue > 0 && cValue == state.GetPlayerValue(p)){
                    if(j <= boardHeight-length 
                        && (direction == Direction.COL || direction == Direction.LINE || direction == Direction.CARDINAL)){
                        //Loop from 1 since we know board[i,j+0] == cValue already
                        bool fullMatch = true;
                        matchList.Add(new Point(i, j));
                        
                        for(int l=1; l<boardHeight - j; l++)
                        {
                            if(state.Value(i, j+l) != cValue){
                                fullMatch = l >= length;
                                
                                break;
                            }

                            matchList.Add(new Point(i, j+l));
                        }
                        if(fullMatch)
                        {
                            //! We can skip j ahead by the match count. 
                            j += matchList.Count;
                            
                            //! If we only need to test the existence of a line (i.e. tic-tac-toe) we
                            //! can return as soon as we find anything.
                            if(checkOnly) return matchList;
                            
                            CheckAndAddResults(checkFromLastPos, matchList, results);
                        }
                        
                        matchList.Clear();
                    }
                    //! Very similar story for horizontal checks
                    if(i <= boardWidth-length 
                        && (direction == Direction.ROW || direction == Direction.LINE || direction == Direction.CARDINAL)){
                        bool fullMatch = true;
                        matchList.Add(new Point(i, j));
                        for(int l=1; l<boardWidth-i; l++){
                            if(state.Value(i+l, j) != cValue){
                                fullMatch = l >= length;
                                break;
                            }
                            
                            matchList.Add(new Point(i+l, j));
                        }
                        
                        if(fullMatch){
                            //! We can't skip i ahead, because we need to check every column.
                            
                            if(checkOnly) return matchList;
                            
                            CheckAndAddResults(checkFromLastPos, matchList, results);
                        }
                        
                        matchList.Clear();
                    }
                    //! And diagonals
                    if(i <= boardWidth-length && j >= length-1
                        && (direction == Direction.LINE)){
                        bool fullMatch = true;
                        matchList.Add(new Point(i, j));
                        for(int l=1; l<boardWidth-i && l<j+1; l++){
                            if(state.Value(i+l, j-l) != cValue){
                                fullMatch = l >= length;
                                break;
                            }
                            
                            matchList.Add(new Point(i+l, j-l));
                        }
                        if(fullMatch){
                            if(checkOnly) return matchList;
                            
                            CheckAndAddResults(checkFromLastPos, matchList, results);
                        }
                        
                        matchList.Clear();
                    }
                    if(i <= boardWidth-length && j <= boardHeight-length
                        && (direction == Direction.LINE)){
                        bool fullMatch = true;
                        matchList.Add(new Point(i, j));
                        for(int l=1; l<boardWidth-i && l < boardHeight-j; l++){
                            if(state.Value(i+l, j+l) != cValue){
                                fullMatch = l >= length;
                                break;
                            }
                            
                            matchList.Add(new Point(i+l, j+l));
                        }
                        if(fullMatch){
                            if(checkOnly) return matchList;
                            
                            CheckAndAddResults(checkFromLastPos, matchList, results);
                        }
                        matchList.Clear();
                    }
                }
            }
        }
        //Return a distinct list of points, to avoid returning the same point multiple times. 
        return results.Distinct().ToList();
    }

    private void CheckAndAddResults(bool checkFromLastPos, List<Point> matchList, List<Point> results)
    {
        if (checkFromLastPos)
        {
            var containsLastPos = false;
            foreach (var point in matchList)
            {
                if (point.x == state.latestMove.x && 
                    point.y == state.latestMove.y)
                {
                    containsLastPos = true;
                    break;
                }
            }

            if (!containsLastPos)
            {
                matchList.Clear();
            }
        }

        results.AddRange(matchList);
    }

    public void MovePiece(int fx, int fy, int tx, int ty){
        //Note we assume that you've done due diligence and checked this is legal
        state.Set(tx, ty, state.Value(fx, fy));
        state.Set(fx, fy, 0);
        if(interactiveMode){
            playableGame.MovePiece(fx, fy, tx, ty);
        }
    }

    public void FlipPiece(int x, int y){
        if(state.Value(x, y) == 0)
            return;
        state.Set(x, y, (state.Value(x, y)%2)+1);
        if(interactiveMode){
            playableGame.FlipPiece(x, y, state.Value(x, y)-1);
        }
    }

    public void DeletePiece(int x, int y){
        state.Set(x, y, 0);
        if(interactiveMode){
            playableGame.DeletePiece(x, y);
        }
    }

    GameState savedGameState;
    int savedEndStatus = 0;

    public void SaveGameState(){
        savedGameState = state.Copy();
        //This should probably always be 0, but we'll keep it flexible just in case.
        savedEndStatus = 0;
    }

    public void RestoreSavedState(){
        state = savedGameState;
        endStatus = savedEndStatus;
    }

    public void SetState(GameState s, int status){
        this.state = s;
        this.endStatus = status;
    }

    //This is slightly more than just a state reset: we need to reset other stuff like the endStatus
    public void ResetState(){
        state = new GameState(boardWidth, boardHeight);
        endStatus = 0;
    }

    //Useful for testing and automated play. Note that each condition and effect has its own little toString
    //so each rule can explain itself. This isn't always possible, but our test system is simple enough to work.
    public void PrintGame()
    {
        Debug.Log(GameToString());
    }

    public string GameToString(){
        string r = "Players take turns placing pieces on the "+boardWidth+"x"+boardHeight+" board.\n";
        r += "\n";
        foreach(Effect e in updatePhase){
            r += e.Print()+"\n";
        }
        r += "\n";
        //We make sure every condition explanation starts with the phrase "if..."
        r += "A player wins "+winCondition.Print();
        r += "\n\n";
        if(loseCondition != null){
            r += "A player loses "+loseCondition.Print();
        }
        return r;
    }

    public string GameToCode(){
        string game = "";
        game += "BOARD "+boardWidth+" "+boardHeight+"\n";
        foreach(Effect e in updatePhase){
            game += e.ToCode()+"\n";
        }
        game += "WIN "+winCondition.ToCode()+"\n";
        if(loseCondition != null)
            game += "LOSE "+loseCondition.ToCode()+"\n";

        return game;
    }

    //Useful for tracking AI games and debugging their behaviour.
    public void PrintBoard(){
        string board = "";
        char[] printCodes = new char[]{'░', 'O', 'X'};
        for(int j=boardHeight-1; j>=0; j--){
            string line = "";
            for(int i=0; i<boardWidth; i++){
                line += printCodes[state.Value(i, j)];
            }
            board += line+"\n";
        }
        Debug.Log(board);
    }

    /*
        Originally I had planned not to do parsing from strings, because it takes a bit of work to make 'safe'.
        However I thought it might make it easier to test and for you to edit, and it links to some examples
        I wanted to discuss in the tutorial. So I've added it here, but there is absolutely no safety against
        e.g. parse errors and such. So this is a very hacky parser that expects very specific things, and can 
        crash easily. Be warned!
    */
    public static Game FromCode(string code){
        //Assume the code is a series of lines
        string[] lines = code.Split('\n');
        //The first line should ALWAYS define the board dimensions
        string[] gameDim = lines[0].Split(' ');
        //This is an example of what I mean by the way - I'm not checking here or throwing errors, I just assume it's fine.
        Game game = new Game(int.Parse(gameDim[1]), int.Parse(gameDim[2]));
        for(int i=1; i<lines.Length; i++){
            //Skip empty lines, while parsing!
            if(string.IsNullOrWhiteSpace(lines[i])) continue;
            
            string[] line = lines[i].Split(' ');
            if(line[0] == "WIN"){
                game.winCondition = ParseCondition(line);
            }
            else if(line[0] == "LOSE"){
                game.loseCondition = ParseCondition(line);
            }
            else{
                game.updatePhase.Add(ParseEffect(line));
            }
        }
        return game;
    }

    public static Condition ParseCondition(string[] s){
        if(s[1] == "MATCH"){
            return new InARowCondition((Direction)System.Enum.Parse(typeof(Direction), s[2]), int.Parse(s[3]));
        }
        if(s[1] == "COUNT"){
            return new PieceCountCondition(int.Parse(s[2]));
        }
        Debug.LogError("Unknown condition: "+string.Join(" ", s));
        return null;
    }

    public static Effect ParseEffect(string[] s){
        if(s[0] == "MATCH"){
            //n.b. although match codes include a direction, we currently always use Direction.LINE
            //So we ignore it here
            return new InARowEffect(
                //Also note how we have to pass the third array entry first? That's because I ordered
                //the constructor arguments differently to how I represent it in the design language.
                //Little things like this can trip you up! I decided to leave it in here as an example.
                (TriggeredEffect)System.Enum.Parse(typeof(TriggeredEffect), s[3]),
                int.Parse(s[2]));
        }
        if(s[0] == "FALL"){
            return new FallPiecesEffect((Heading)System.Enum.Parse(typeof(Heading), s[1]));
        }
        if(s[0] == "CAP"){
            return new CappedEffect((TriggeredEffect)System.Enum.Parse(typeof(TriggeredEffect), s[1]));
        }
        Debug.LogError("Unknown effect: "+string.Join(" ", s));
        return null;
    }

}
