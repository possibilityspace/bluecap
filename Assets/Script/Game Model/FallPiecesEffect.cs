using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallPiecesEffect : Effect
{
    
    public Heading fallDirection;

    public FallPiecesEffect(Heading h){
        fallDirection = h;
    }

    override public string ToCode(){
        return "FALL "+fallDirection.ToString();
    }

    override public void Apply(Game g){
        Point drop;
        
        //We need slightly different methods for each direction, 
        //because we need to start from a different end each time
        if(fallDirection == Heading.DOWN){
            for(int i=0; i<g.boardWidth; i++){
                for(int j=g.boardHeight-1; j>=0; j--){
                    if(g.state.Value(i, j) > 0){
                        drop = FindDrop(g, i, j, fallDirection);
                        //Update the piece, assuming it needs to
                        if(drop.x != i || drop.y != j)
                            g.MovePiece(i,j,drop.x, drop.y);
                    }
                }
            }
        }
        if(fallDirection == Heading.UP || fallDirection == Heading.LEFT){
            for(int i=0; i<g.boardWidth; i++){
                for(int j=0; j<g.boardHeight; j++){
                    if(g.state.Value(i, j) > 0){
                        drop = FindDrop(g, i, j, fallDirection);
                        //Update the piece, assuming it needs to
                        if(drop.x != i || drop.y != j)
                            g.MovePiece(i,j,drop.x, drop.y);
                    }
                }
            }
        }
        if(fallDirection == Heading.RIGHT){
            for(int i=g.boardWidth-1; i>=0; i--){
                for(int j=0; j<g.boardHeight; j++){
                    if(g.state.Value(i, j) > 0){
                        drop = FindDrop(g, i, j, fallDirection);
                        //Update the piece, assuming it needs to
                        if(drop.x != i || drop.y != j)
                            g.MovePiece(i,j,drop.x, drop.y);
                    }
                }
            }
        }

    }

    public Point FindDrop(Game game, int x, int y, Heading dir){
        if(fallDirection == Heading.UP){
            for(int i=y+1; i<game.boardHeight; i++){
                if(game.state.Value(x, i) != 0){
                    return new Point(x, i-1);
                }
            }
            return new Point(x, game.boardHeight-1);
        }
        else if(fallDirection == Heading.DOWN){
            for(int i=y-1; i>=0; i--){
                if(game.state.Value(x, i) != 0){
                    return new Point(x, i+1);
                }
            }
            return new Point(x, 0);
        }
        else if(fallDirection == Heading.RIGHT){
            for(int i=x+1; i<game.boardWidth; i++){
                if(game.state.Value(i, y) != 0){
                    return new Point(i-1, y);
                }
            }
            return new Point(game.boardWidth-1, y);
        }
        else if(fallDirection == Heading.LEFT){
            for(int i=x-1; i>=0; i--){
                if(game.state.Value(i, y) != 0){
                    return new Point(i+1, y);
                }
            }
            return new Point(0, y);
        }
        return new Point(x, y);
    }

    public override string Print(){
        return "All pieces on the board fall "+fallDirection.ToString().ToLower()+".";
    }

}
