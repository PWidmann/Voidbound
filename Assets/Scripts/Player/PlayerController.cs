﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Transform playerPosition;

    public bool isGrounded;
    public bool isSprinting;

    private Transform cam;
    private World world;

    public float walkSpeed = 4f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.15f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical = 0;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    //Camera
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public Transform highlightBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.01f;
    public float reach = 8f;

    public Toolbar toolbar;
    public GameObject flashLight;
    

    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        world.inUI = false;

    }

    private void FixedUpdate()
    {
        if (!world.inUI)
        {
            CalculateVelocity();
            if (jumpRequest)
                Jump();


            transform.Translate(velocity, Space.World);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            world.inUI = !world.inUI;
        }
        if (!world.inUI)
        {
            GetPlayerInputs();

            placeCursorBlocks();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            flashLight.SetActive(!flashLight.activeSelf);
        }
        
    }

    private void CalculateVelocity()
    {
        // Affect vertical momentum with gravity
        if (verticalMomentum > gravity)
            verticalMomentum += gravity * Time.deltaTime;

        // if we're sprinting, use the sprint multiplier
        if (isSprinting)
            velocity = ((transform.forward * vertical) + transform.right * horizontal) * sprintSpeed * Time.fixedDeltaTime;
        else
            velocity = ((transform.forward * vertical) + transform.right * horizontal) * walkSpeed * Time.fixedDeltaTime;

        //Apply vertical momentum (fallig and jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
            velocity.z = 0;
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
            velocity.x = 0;
        if (velocity.y < 0)
            velocity.y = checkDownSpeed(velocity.y);
        else if (velocity.y > 0)
            velocity.y = checkUpspeed(velocity.y);
    }

    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void GetPlayerInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical += Input.GetAxis("Mouse Y");


        if (Input.GetButtonDown("Sprint"))
            isSprinting = true;
        if (Input.GetButtonUp("Sprint"))
            isSprinting = false;


        if (isGrounded && Input.GetButtonDown("Jump"))
            jumpRequest = true;


        if (highlightBlock.gameObject.activeSelf)
        {
            // Destroy block
            if (Input.GetMouseButtonDown(0))
                world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);

            // Place block
            if (Input.GetMouseButtonDown(1))
            {

                Vector3 playerCoord = new Vector3(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y), Mathf.FloorToInt(transform.position.z));
                if (playerCoord != placeBlock.position && (playerCoord + new Vector3(0, 1, 0)) != placeBlock.position)
                {
                    if (toolbar.slots[toolbar.slotIndex].HasItem)
                    {
                        world.GetChunkFromVector3(placeBlock.position).EditVoxel(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                        toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                    } 
                }
            }         
        }
    }

    private void placeCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 pos = cam.position + (cam.forward * step);

            if (world.CheckForVoxel(pos))
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;

                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            step += checkIncrement;
        }

        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);

    }

    private float checkDownSpeed(float downSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) && (!left && !back) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) && (!right && !back) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) && (!right && !front) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) && (!left && !front)
            )
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float checkUpspeed(float upSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) && (!left && !back) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) && (!right && !back) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) && (!right && !front) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) && (!left && !front)
            )
        {
            verticalMomentum = 0;
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool front
    {
        get {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))
                )
                return true;
            else
                return false;
        }
    }

    public bool back
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))
                )
                return true;
            else
                return false;
        }
    }

    public bool left
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))
                )
                return true;
            else
                return false;
        }
    }

    public bool right
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))
                )
                return true;
            else
                return false;
        }
    }
}


