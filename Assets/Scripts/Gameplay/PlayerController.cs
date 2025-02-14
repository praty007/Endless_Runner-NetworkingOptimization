using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    float targetxPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _initialize();

    }
    void _initialize(){
        targetxPosition = transform.position.x;
        if (TryGetComponent(out InputHandler handler))
        {
            handler.OnSwipe += HandleSwipe;
            handler.OnJump += HandleJump;
        }
    }
    // Update is called once per frame
    void  FixedUpdate()
    {
        Vector3 pos = transform.position;
        float x = Mathf.Lerp(pos.x, targetxPosition, Time.fixedDeltaTime);
        float z = pos.z + speed * Time.fixedDeltaTime;
        transform.position = new Vector3(x, pos.y, z);
    }
    private void OnTriggerEnter(Collider other){
        if (other.CompareTag("Obstacle"))
            ObjectSpawner.instance.ObstacleCollided(other.gameObject);
        else if (other.CompareTag("Coin"))
            ObjectSpawner.instance.CoinCollected(other.gameObject);
    }
    //Implement HandleJump and HandleSwipe methods here
    private void HandleSwipe(float direction)
    {
        // Move the player left or right based on the swipe direction
        targetxPosition += direction;
    }
    private void HandleJump(float force)
    {
        // Apply an upward force to the player based on the jump force
        GetComponent<Rigidbody>().AddForce(Vector3.up * force, ForceMode.Impulse);
    }
}
