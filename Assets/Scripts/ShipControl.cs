using UnityEngine;

public class ShipControl : MonoBehaviour
{
    public KeyCode controlKey = KeyCode.Space;
    public Color color;

    public float decayPower = 15.0f;
    public float altitudeModifier = 8.0f;

    public float baseTurningForce = 110.0f;
    public float maxTurningForce = 220.0f;
    public float turningPower = 100.0f;

    public float speed = 2.5f;

    public delegate void ScoreUpdated(int newScore);
    public event ScoreUpdated OnScoreUpdated;

    public int score = 0;

    [HideInInspector]
    public Vector2 Origin;
    [HideInInspector]
    public int ID;

    private Rigidbody2D _body;

    private float _delTheta = 0.0f;
    private bool _powered = false;
    private bool _powerEnabled = true;
    private float _turningForce = 0.0f;
    private bool _powerJustStarted = false;

    private Vector2 _spawnPoint;
    private bool _invulnerable = false;

    private Color _lastColor;

    // Start is called before the first frame update
    void Start()
    {
        if (!_body) _body = GetComponent<Rigidbody2D>();
        if (Origin == null) {
            GameObject originObj = GameObject.Find("Origin");
            Origin = new Vector2(originObj.transform.position.x, originObj.transform.position.y);
        }
    }

    void Awake()
    {
    }

    void Update()
    {
        // Power on/off
        bool newPowered = Input.GetKey(controlKey) && _powerEnabled;

        // Power just became active
        _powerJustStarted = (newPowered && !_powered) || _powerJustStarted;

        _powered = newPowered;

        if (_lastColor != color)
        {
            GetComponent<SpriteRenderer>().color = color;
            _lastColor = color;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_body.simulated) return;

        Vector2 relativePos = Origin - _body.position;
        Vector2 bodyRotationVector = _body.GetRelativeVector(new Vector2(0, 1));
        float relativeAngle = Vector2.SignedAngle(relativePos, bodyRotationVector);

        float baseTurnSpeed = speed * speed * altitudeModifier / (relativePos.magnitude);
        float turnDecay = baseTurnSpeed + decayPower * ((-1.0f * relativePos.magnitude) + 7.5f);
        _delTheta = turnDecay * ((relativeAngle > 0) ? -1.0f : 1.0f) * Time.fixedDeltaTime;

        // POWER //
        ///////////

        if (_powerJustStarted) {
            _turningForce = ((relativeAngle > 0) ? -1.0f : 1.0f) * baseTurningForce;
            _powerJustStarted = false;
        }

        float delTurningForce;
        if (_powerEnabled)
        {
            if (_powered)
            {
                delTurningForce = turningPower / relativePos.magnitude;
            } else
            {
                delTurningForce = 0.0f;
                _turningForce = 0.0f;
            }
        } else
        {
            delTurningForce = -turningPower / relativePos.magnitude;
        }

        delTurningForce *= ((relativeAngle > 0) ? -1.0f : 1.0f) * Time.fixedDeltaTime;

        _turningForce = Mathf.Clamp(_turningForce + delTurningForce, -maxTurningForce, maxTurningForce);

        _delTheta -= _turningForce * Time.fixedDeltaTime;

        //Flip Handling
        float nextRelativeAngle = _delTheta + relativeAngle;
        if (Mathf.Abs(nextRelativeAngle) > 180.0f)
        {
            // Start flip
            _powerEnabled = false;
            _turningForce = baseTurningForce * ((relativeAngle > 0) ? -1.0f : 1.0f);
        } else if (Mathf.Abs(nextRelativeAngle) < 80.0f && !_powerEnabled)
        {
            // End Flip
            _powerEnabled = true;
            _turningForce = 0.0f;
        } else if ((nextRelativeAngle * relativeAngle) < 0)
        {
            // prevent downwards flip
            _delTheta = 0.0f;
        }

        // Apply rotation
        _body.SetRotation(_body.rotation + _delTheta);

        // Apply velocity
        _body.velocity = transform.up * speed;
    }

    void OnCollisionEnter2D (Collision2D collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Enemy":
                Die();
                break;
            case "PointsOrb":
                score += 1;
                OnScoreUpdated(score);
                break;
            case "Ship":
                CheckShipContact(collision);
                break;
        }
    }

    void OnTriggerEnter2D (Collider2D collider)
    {
        if (collider.gameObject.tag == "Origin") Die();
    }

    void Die()
    {
        gameObject.SetActive(false);

        score = Mathf.Max(score -= 1, 0);

        OnScoreUpdated(score);

        Invoke("Respawn", 2.0f);
    }

    void Respawn()
    {
        // TODO: generate spawn point
        Spawn();

        SetInvulnerable();
        Enable();

        Invoke("SetVulnerable", 2.0f);
    }

    public void SetSpawnPoint(Vector2 position)
    {
        _spawnPoint = position;
    }

    public void Spawn()
    {
        Vector2 relativePos = Origin - _spawnPoint;

        Vector2 direction = Vector2.Perpendicular(relativePos);

        transform.position = new Vector3(_spawnPoint.x, _spawnPoint.y, 2.0f);
        transform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, direction));
    }

    void SetInvulnerable()
    {
        _invulnerable = true;

        // Disable collisions with other players
        for (int i = (int) Layers.Player1; i <= (int)Layers.Player4; i++)
        {
            if (i == gameObject.layer) continue;

            Physics.IgnoreLayerCollision(i, gameObject.layer, true);
        }

        Physics.IgnoreLayerCollision((int)Layers.Enemies, gameObject.layer, true);
    }

    void SetVulnerable()
    {
        _invulnerable = false;

        // Reenable collisions with other players
        for (int i = (int)Layers.Player1; i <= (int)Layers.Player4; i++)
        {
            if (i == gameObject.layer) continue;

            Physics.IgnoreLayerCollision(i, gameObject.layer, false);
        }

        Physics.IgnoreLayerCollision((int)Layers.Enemies, gameObject.layer, false);
    }

    public void Enable()
    {
        gameObject.SetActive(true);
        _body.simulated = true;
    }

    void CheckShipContact(Collision2D collision)
    {
        Debug.Log(collision.collider.gameObject.tag);
        if (collision.collider.gameObject.tag == "Body" && collision.otherCollider.gameObject.tag == "Head")
        {
            // This ship hit another's body with its head, add points
            score += 2;

            OnScoreUpdated(score);
        } else if (collision.collider.gameObject.tag == "Head" && collision.otherCollider.gameObject.tag == "Body")
        {
            if (!_invulnerable)
            {
                // This ship hit another's head with its body, remove points and die!
                Die();
            }
        }
    }
}
