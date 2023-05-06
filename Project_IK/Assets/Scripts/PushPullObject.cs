using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushPullObject : MonoBehaviour, IInteraction
{
    public Rigidbody rigidbody;
    public FixedJoint fixedJoint;

    public void Interact(Rigidbody rigidbody)
    {
        this.rigidbody.isKinematic = false;
        fixedJoint.connectedBody = rigidbody;
    }

    public void Deactivate()
    {
        rigidbody.isKinematic = true;
        fixedJoint.connectedBody = null;
    }
}
