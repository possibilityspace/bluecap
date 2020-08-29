using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPiece : MonoBehaviour
{
    
    public SpriteRenderer mainSprite;
    public SpriteRenderer outlineSprite;

    public Sprite[] shapes;

    public void SetShape(int shapeCode){
        mainSprite.sprite = shapes[shapeCode];
        outlineSprite.sprite = shapes[shapeCode];
    }

    public void PieceMoved(int tox, int toy){
        //todo
        //huh guess i just left this one huh? that's cool. that happens.
    }
}
