﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector3 velocity;
    public GameObject shooter;
    public int damage;
    public float timer;

    void Awake()
    {
    }

    void Start()
    {
        timer = 10;
    }

    void Update ()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
            Destroy(gameObject);

        transform.position += velocity * Time.deltaTime;
	}

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.GetComponent<Health>() != null)
        {
            other.gameObject.GetComponent<Health>().Damage(damage, shooter);

            if (other.transform.GetComponent<Health>().Enemy && shooter.tag == "Player")
                other.transform.GetComponent<Health>().Attacker = shooter;
        }
        Destroy(gameObject);
    }
}
