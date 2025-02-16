using UnityEngine;

/// <summary>
/// Spawns the ground in a loop. Attach it to the player object.
/// </summary>
public class InfiniteGround : MonoBehaviour
{
    [SerializeField] private GameObject Ground;
    private GameObject CurrentGround, NextGround;
    private float groundLength;
    private MeshRenderer currentGroundRenderer;

    void Start()
    {
        CurrentGround = Ground;
        currentGroundRenderer = CurrentGround.GetComponent<MeshRenderer>();
        groundLength = currentGroundRenderer.bounds.size.z;
        NextGround = Instantiate(Ground, new Vector3(Ground.transform.position.x, Ground.transform.position.y, groundLength), Quaternion.identity, transform.parent);

    }

    // Update is called once per frame
    void Update()
    {
        // Check if player has passed the current ground
        if (transform.position.z > currentGroundRenderer.bounds.center.z + currentGroundRenderer.bounds.extents.z)
        {
            // Move the old current ground to the next position
            CurrentGround.transform.position = new Vector3(Ground.transform.position.x, Ground.transform.position.y, NextGround.transform.position.z + groundLength);
            
            // Swap ground references
            GameObject temp = CurrentGround;
            CurrentGround = NextGround;
            NextGround = temp;

            // Update the mesh renderer reference for the new current ground
            currentGroundRenderer = CurrentGround.GetComponent<MeshRenderer>();
         }
    }
}
