using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteraction
{
    void Interact(Rigidbody rigidbody);
    void Deactivate();
}
