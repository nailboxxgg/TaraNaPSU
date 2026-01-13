using UnityEngine;
using System.Collections;

public class PlayerGuideController : MonoBehaviour
{
    public static PlayerGuideController Instance { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float heightOffset = 0.1f;

    [Header("Visuals")]
    public GameObject avatarModel; // Drag your 3D model or Sprite here
    public Animator animator;      // Optional: link your animator for walk animations

    private Coroutine walkCoroutine;
    private bool isWalking = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Instantly place the player at a position
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        StopAutoWalk();
        transform.position = new Vector3(position.x, position.y + heightOffset, position.z);
    }

    /// <summary>
    /// Starts a simulated walk along the calculated path
    /// </summary>
    public void StartAutoWalk(Vector3[] pathCorners)
    {
        StopAutoWalk();
        if (pathCorners == null || pathCorners.Length < 2) return;
        walkCoroutine = StartCoroutine(FollowPathRoutine(pathCorners));
    }

    public void StopAutoWalk()
    {
        if (walkCoroutine != null) StopCoroutine(walkCoroutine);
        isWalking = false;
        if (animator != null) animator.SetBool("isWalking", false);
    }

    private IEnumerator FollowPathRoutine(Vector3[] corners)
    {
        isWalking = true;
        if (animator != null) animator.SetBool("isWalking", true);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 targetPoint = new Vector3(corners[i].x, corners[i].y + heightOffset, corners[i].z);

            while (Vector3.Distance(transform.position, targetPoint) > 0.1f)
            {
                // Move towards corner
                transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

                // Rotate towards corner
                Vector3 direction = (targetPoint - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    direction.y = 0; // Keep horizontal
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                yield return null;
            }
        }

        isWalking = false;
        if (animator != null) animator.SetBool("isWalking", false);
        Debug.Log("[PlayerGuide] Simulation complete.");
    }

    public bool IsWalking => isWalking;
}
