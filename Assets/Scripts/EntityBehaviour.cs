using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public class EntityBehaviour : MonoBehaviour
{
    public EntityType type;
    public MoveMode moveMode;

    // Default remove after 30 seconds
    public float lifeRemaining = 30.0f;

    public float speed = 1.0f;

    private float _turnSpeed = 0;
    private float _direction = 0.0f;
    private float _spriteRotationSpeed;

    private Rigidbody2D _body;

    // Start is called before the first frame update
    void Start()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.simulated = false;

        // Random rotation
        _direction = Random.Range(0.0f, 360.0f);

        switch (moveMode)
        {
            case MoveMode.Random:
                // Random speed between 1.0f and 3.0f or -1.0f and -3.0f
                _spriteRotationSpeed = Random.Range(10.0f, 15.0f) * (Random.value > 0.5f ? -1.0f : 1.0f);

                MoveRandomly();
                break;

            case MoveMode.Homing:
                if (type == EntityType.Enemy) MoveTowards();
                else MoveAway();

                break;
        }

        StartCoroutine("Appear");
    }

    public void Spawn(float lifetime, Vector2 position)
    {
        lifeRemaining = lifetime;

        transform.position = position;
    }

    IEnumerator Appear()
    {
        Vector3 finalScale = transform.localScale;

        float frameCount = 60;

        for (float i = 0; i < frameCount; i++)
        {
            transform.localScale = finalScale * (2.0f - (i / frameCount));
            yield return null;
        }

        _body.simulated = true;
    }

    // Update is called once per frame
    void Update()
    {
        lifeRemaining -= Time.deltaTime;

        if (lifeRemaining < 0)
        {
            Die();
        }

        if (moveMode == MoveMode.Static) return;

        _direction += _turnSpeed * Time.deltaTime;

        Vector2 del = (Vector2)(Quaternion.Euler(0, 0, _direction) * Vector2.up);

        _body.velocity = del * speed;

        if (moveMode == MoveMode.Random)
        {
            if (type == EntityType.Enemy)
            {
                // Point entity forward
                _body.SetRotation(_direction);
            } else
            {
                _body.rotation += _spriteRotationSpeed * Time.deltaTime;
            }
        }
    }

    void MoveRandomly()
    {
        _turnSpeed = (type == EntityType.PointsOrb) ? Random.Range(-15f, 15f) : Random.Range(-25f, 25f);

        Invoke("MoveRandomly", Random.Range(2.0f, 5.0f));
    }

    void MoveTowards()
    {


        //Invoke("MoveTowards", (Random.value * 2.0f) + 1.0f);
    }

    void MoveAway()
    {


        //Invoke("MoveAway", Random.Range(2.0f, 4.0f));
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ship") Pop();
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Origin") Die();
    }

    // Used when entity reaches EoL or hits origin circle
    public void Die()
    {
        GetComponent<Rigidbody2D>().simulated = false;

        StartCoroutine("Disappear");
    }

    // Shrink over 60 frames then self-destruct
    IEnumerator Disappear()
    {
        Vector3 initialScale = transform.localScale;

        float frameCount = 60;

        for (float i = 0; i < frameCount; i++)
        {
            transform.localScale = initialScale * (1.0f - (i / frameCount));
            yield return null;
        }

        Destroy(gameObject);
    }

    // Instantly destroy object and apply a visual effect
    void Pop()
    {
        Destroy(gameObject);

        // TODO: play effects
    }
}

public enum EntityType
{
    Enemy,
    PointsOrb
}

public enum MoveMode
{
    Static,
    Random,
    Homing
}
