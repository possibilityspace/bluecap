using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardPiece : MonoBehaviour
{

    public SpriteRenderer sprite;

    [HideInInspector]
    public int x;
    [HideInInspector]
    public int y;

    //TODO: Click detection

    public void OnMouseEnter(){

    }

    public void OnMouseExit(){

    }

    public void OnMouseDown(){
        BoardManager.instance.TapTile(x, y);
    }


}
