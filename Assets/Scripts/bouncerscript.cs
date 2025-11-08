using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bouncerscript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCharacter player = other.GetComponent<PlayerCharacter>(); 
        if (player != null)
        {
            player.StartJump();
        }
    }
}
