using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class grav_rev : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var _character = other.GetComponent<PlayerCharacter>(); 
        if (_character != null)
        {
            _character.ToggleGravity();
        }
    }
}
