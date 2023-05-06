using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public Rigidbody rigidbody;
    public Animator animator;
    public float speed;
    public float rotationSpeed;
    public float jumpSpeed;
    public float grabAxisLerpSpeed = 1f;
    public float pushPullSpeed;

    private bool bIsGrab = false;
    private float grabAxis = 0f;

    private bool bJumping = false;

    private Rigidbody interactedRigidbody;
    private IInteraction interaction;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!bIsGrab)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 direction = Vector3.forward * v + Vector3.right * h;
            //direction.Normalize();

            //Vector3 translation = direction * speed * Time.deltaTime;
            Vector3 velocity = direction * speed;

            //transform.Translate(translation, Space.World);
            rigidbody.velocity = new Vector3(velocity.x, rigidbody.velocity.y, velocity.z);

            float axis = direction.magnitude;
            axis = (axis < 0) ? -axis : axis;
            animator.SetFloat("Axis", axis);

            if (axis > 0.1)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
            }

            if(Input.GetKeyDown(KeyCode.Space))
            {
                rigidbody.AddForce(jumpSpeed * Vector3.up, ForceMode.VelocityChange);

                bJumping = true;
                animator.SetTrigger("Jumping");
            }

            if(bJumping && rigidbody.velocity.y < 0)
            {
                if(rigidbody.SweepTest(Vector3.down, out RaycastHit hit, Mathf.Abs(rigidbody.velocity.y) * Time.deltaTime))
                {
                    bJumping = false;
                    animator.SetTrigger("Landing");
                }
            }
        }
        else
        {
            float h = Input.GetAxis("Horizontal");

            grabAxis = Mathf.Lerp(grabAxis, h, Time.deltaTime * grabAxisLerpSpeed);

            if(grabAxis > .99f)
            {
                grabAxis = 1f;
            }
            else if(grabAxis < -.99f)
            {
                grabAxis = -1f;
            }
            else if(grabAxis > -.01f && grabAxis < .01f)
            {
                grabAxis = 0f;
            }

            animator.SetFloat("HorizontalAxis", grabAxis);

            Vector3 direction = Vector3.right * grabAxis;

            Vector3 velocity = direction * pushPullSpeed;

            rigidbody.velocity = new Vector3(velocity.x, rigidbody.velocity.y, velocity.z);

            if (Input.GetKeyUp(KeyCode.Space))
            {
                Grab(false);

                interaction.Deactivate();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != null && other.gameObject.tag == "Interaction")
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                Grab(true);
                interaction = other.transform.GetComponent<IInteraction>();
                interaction.Interact(rigidbody);
            }
        }
    }

    private void Grab(bool value)
    {
        bIsGrab = value;
        animator.SetBool("IsGrab", value);

        //pushPullCollider.enabled = value;
    }
}
