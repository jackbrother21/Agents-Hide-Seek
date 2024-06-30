
using UnityEngine; // Unity core functionality

public class Block : MonoBehaviour
{
    public int? currentlyHeldByPlayer = null;
    public Team? currentlyLockedByTeam = null;

    public bool IsHeldOrLocked
    {
        get
        {
            return currentlyHeldByPlayer.HasValue || currentlyLockedByTeam.HasValue;
        }
    }


    public MySceneBehavior mySceneBehavior;
    public GridManager gridManager;

    public int width_length;
    public int height_length;


    [SerializeField] private Transform environmentLocation;

    // Object color Variables
    Material blockMaterial;  // 
    public GameObject env;

    [HideInInspector] public Renderer blockRenderer;

    private float yPosition;

    public void BlockInitialize()
    {
        blockRenderer = GetComponent<Renderer>();
        yPosition = transform.localPosition.y;     
    }

    public void Update()
    {
        Vector3 rotation = transform.eulerAngles;
        rotation.x = 0;
        rotation.z = 0;
        transform.eulerAngles = rotation;

        Vector3 position = transform.position;
        position.y = yPosition;

        transform.position = position;
        TagTheBlock();
    }

    public void TagTheBlock()
    {
        this.gameObject.tag = "Block";
    }

    public void placeMe(float x, float y, float Rotation)
    {
        transform.localPosition = new Vector3(x, 0f, y);
        //Debug.Log(x + y + Rotation);

        transform.localRotation = Quaternion.Euler(0f, Rotation, 0f);
    }
    public void BlockOnEpisodeBegin()
    {
        currentlyHeldByPlayer = null;
        currentlyLockedByTeam = null;
        // Reset the objects's position randomly within a range

        //int randomYMultiplier = Random.Range(0, 4); // 4 is exclusive, so it will never actually be picked
        //int yRotation = randomYMultiplier * 90; // Random Y rotation at 90 degree increments
        Vector3 localEulerAngles = transform.localEulerAngles;

        // Extract the Y angle and cast to int
        int yRotation = (int)localEulerAngles.y;
        (float x, float y, int rotation) = gridManager.PlaceItem(width_length, height_length, yRotation);
        //Debug.Log("block placed");

        placeMe(x, y, rotation);




        // Retain the x position as set in the UI, and reset the y and z positions randomly within a range
        float xPosition = Random.Range(-8f, 8f);   // Retain the current x position
        float zPosition = Random.Range(-8f, 8f);     // Random z position

        //transform.localPosition = new Vector3(xPosition, yPosition, zPosition);


        AddRigidbody();
        // reset colour
        blockRenderer.material = mySceneBehavior.unlockedColor;

    }

    public void kinematicTrue()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    public void kinematicFalse()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
    }
    public void AddRigidbody()
    {
        // Check if the Rigidbody already exists
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            TagTheBlock();
        }

        // Set Rigidbody properties
        rb.mass = 3f;
        rb.drag = 4f;
        rb.angularDrag = 0.5f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Freezing position and rotation as per your requirements
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void RemoveRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
        /*/ Add a Mesh Collider to this GameObject
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();

        // Optional: Configure the Mesh Collider
        meshCollider.convex = true;  // Set to true if you need a collider for a non-static Rigidbody
        meshCollider.isTrigger = false;  // Set to true if it should be used as a trigger

        // If you need to assign a specific mesh (uncomment and set appropriately)
        // meshCollider.sharedMesh = someMesh; */
    }

}

