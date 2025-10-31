using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PlayerMove : MonoBehaviour
{
    private SteamVR_Action_Vector2 touchpad = null;
    private SteamVR_Action_Boolean m_Boolean = null;

    private CharacterController controller = null;
    public float speed = 1f;
    private bool checkWalk = false;

    private void Awake()
    {
        touchpad = SteamVR_Actions._default.Touchpad;
        m_Boolean = SteamVR_Actions._default.TouchClick;
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (m_Boolean.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            checkWalk = true;
        }


        if (m_Boolean.GetStateUp(SteamVR_Input_Sources.RightHand))
        {
            checkWalk = false;

        }
        if (touchpad.axis.magnitude > 0.1f);
		{
            if (checkWalk)
            {
                Vector3 direction = Player.instance.hmdTransform.TransformDirection(new Vector3(touchpad.axis.x, 0, touchpad.axis.y));
                controller.Move(speed * Time.deltaTime * Vector3.ProjectOnPlane(direction, Vector3.up) - new Vector3(0, 9.81f, 0) * Time.deltaTime);
            }

        }

    }
}