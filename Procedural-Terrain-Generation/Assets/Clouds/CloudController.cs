using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudController : MonoBehaviour
{
    private ParticleSystem cloudSystem;
    public Color color;
    public Color lining;
    private bool painted = false;
    public int numberOfParticles;
    public float minSpeed;
    public float maxSpeed;
    public float distance;
    private Vector3 startPosition;
    private float speed;

    // Start is called before the first frame update
    void Start()
    {
        cloudSystem = this.GetComponent<ParticleSystem>();
        Spawn();
    }
    
    // Update is called once per frame
    void Update()
    {
        this.transform.Translate(0,0,speed);

        if (Vector3.Distance(this.transform.position,startPosition) > distance)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        float xpos = UnityEngine.Random.Range(-0.5f, 0.5f);
        float ypos = UnityEngine.Random.Range(-0.5f, 0.5f);
        float zpos = UnityEngine.Random.Range(-0.5f, 0.5f);
        this.transform.localPosition = new Vector3(xpos, ypos, zpos);
        speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        startPosition = this.transform.position;
    }
}
