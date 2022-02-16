using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
    public class PlayerMovement : MonoBehaviour
    {
        private float speed = 50;
        public CharacterController controller;
        public float gravity = -9.81f;
        private float jumpHeight = 15f;
        Vector3 velocity;
        private float x = 0f;
        private float z = 0f;
        private bool isGrounded;
        void Update()
        {
            //MOVEMENT
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -1f;
            }
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
            if (isGrounded)
            {
                Vector3 move = transform.right * x + transform.forward * z;
                controller.Move(move * speed * Time.deltaTime);
            }
            else
            {
                Vector3 move = transform.right * x + transform.forward * z;
                controller.Move(move * (speed / 2) * Time.deltaTime);
            }
            //JUMP
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * (-1.5f) * gravity);
            }
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
    }
}
