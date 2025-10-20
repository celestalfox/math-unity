using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bouncerscript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var _character = other.GetComponent<PlayerCharacter>(); 
        if (_character != null)
        {
            _character.StartJump();
        }
    }
}
