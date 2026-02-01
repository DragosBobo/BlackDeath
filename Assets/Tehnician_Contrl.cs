using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tehnician_Contrl : MonoBehaviour
{
    private float move_speed = 8f;
    private float rotate_speed = 90f;
    private CharacterController player_controller;

    void Start()
    {
        player_controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float Horizontal_in = Input.GetAxis("Horizontal");
        float Vertical_in = Input.GetAxis("Vertical");

        Vector3 move_Direction = transform.forward * Vertical_in * move_speed;

        transform.Rotate(Vector3.up, Horizontal_in * rotate_speed * Time.deltaTime);


        player_controller.SimpleMove(move_Direction);

    }

    private void OnTriggerEnter(Collider other)
    {
        //if (other.CompareTag("Door"))
    }
}
