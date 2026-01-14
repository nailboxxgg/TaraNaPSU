using UnityEngine;
using UnityEngine.EventSystems;

public class Map2DController : MonoBehaviour
{
    private static Map2DController _instance;
    public static Map2DController Instance
    {
        get
        {
            if (_instance == null) _instance = FindObjectOfType<Map2DController>(true);
            return _instance;
        }
    }

    [Header("Camera Settings")]
    public Camera mapCamera;
    public float minZoom = 10f;
    public float maxZoom = 50f;
    public float zoomSpeed = 5f;
    public float panSpeed = 0.3f;

    [Header("Map Bounds")]
    public Vector2 mapMinBounds = new Vector2(-50f, -50f);
    public Vector2 mapMaxBounds = new Vector2(100f, 100f);

    [Header("Floor Management")]
    public GameObject[] floorContainers;
    public int currentFloor = 0;

    [Header("Follow Settings")]
    public Transform followTarget;
    public bool isFollowing = false;

    [Header("Markers")]
    public GameObject userMarkerPrefab;
    public GameObject destinationMarkerPrefab;

    private GameObject userMarker;
    private GameObject destinationMarker;
    private Vector3 userPosition;
    private Vector3 destinationPosition;

    private Vector3 lastMousePosition;
    private bool isDragging = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (mapCamera == null)
            mapCamera = Camera.main;
    }

    void OnEnable()
    {
        // When the panel is shown, refresh the view
        if (userMarker != null && userMarker.activeSelf)
        {
            CenterOnPosition(userPosition);
        }
        Debug.Log("[Map2D] Panel Enabled: View Refreshed.");
    }

    void Start()
    {
        SetupOrthographicCamera();
        ShowFloor(currentFloor);
    }

    void Update()
    {
        HandlePanInput();
        HandleZoomInput();
    }

    void LateUpdate()
    {
        if (isFollowing && followTarget != null && !isDragging)
        {
            CenterOnPosition(followTarget.position);
        }
    }

    void SetupOrthographicCamera()
    {
        if (mapCamera != null)
        {
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = 25f; // Default starting zoom
            mapCamera.transform.position = new Vector3(mapCamera.transform.position.x, 20f, mapCamera.transform.position.z);
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    void HandlePanInput()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            isFollowing = false; // Stop following if user starts dragging
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0) && mapCamera != null)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * mapCamera.orthographicSize / 50f;
            
            Vector3 newPos = mapCamera.transform.position + move;
            newPos.x = Mathf.Clamp(newPos.x, mapMinBounds.x, mapMaxBounds.x);
            newPos.z = Mathf.Clamp(newPos.z, mapMinBounds.y, mapMaxBounds.y);
            
            mapCamera.transform.position = newPos;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.touchCount == 1 && mapCamera != null)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                isFollowing = false; // Stop following if user touches
                lastMousePosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector3 delta = (Vector3)touch.position - lastMousePosition;
                Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * mapCamera.orthographicSize / 50f * 0.01f;
                
                Vector3 newPos = mapCamera.transform.position + move;
                newPos.x = Mathf.Clamp(newPos.x, mapMinBounds.x, mapMaxBounds.x);
                newPos.z = Mathf.Clamp(newPos.z, mapMinBounds.y, mapMaxBounds.y);
                
                mapCamera.transform.position = newPos;
                lastMousePosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
    }

    void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && mapCamera != null)
        {
            mapCamera.orthographicSize -= scroll * zoomSpeed;
            mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize, minZoom, maxZoom);
        }

        if (Input.touchCount == 2 && mapCamera != null)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            mapCamera.orthographicSize -= difference * 0.01f * zoomSpeed;
            mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize, minZoom, maxZoom);
        }
    }

    public void ShowFloor(int floor)
    {
        currentFloor = floor;
        
        for (int i = 0; i < floorContainers.Length; i++)
        {
            if (floorContainers[i] != null)
            {
                // Floor 0 (Campus Grounds) stays ON always if it's the base layer
                if (i == 0) 
                    floorContainers[i].SetActive(true);
                else
                    floorContainers[i].SetActive(i == floor);
            }
        }

        Debug.Log($"[Map2D] Switched to floor {floor}. Base layer (Floor 0) remains active.");
    }

    public void SetUserPosition(Vector3 position)
    {
        userPosition = position;

        if (userMarker == null && userMarkerPrefab != null)
        {
            userMarker = Instantiate(userMarkerPrefab);
        }

        if (userMarker != null)
        {
            userMarker.transform.position = new Vector3(position.x, position.y + 0.5f, position.z);
            userMarker.SetActive(true);
        }

        CenterOnPosition(position);
    }

    public void SetDestination(Vector3 position, string name)
    {
        destinationPosition = position;

        if (destinationMarker == null && destinationMarkerPrefab != null)
        {
            destinationMarker = Instantiate(destinationMarkerPrefab);
        }

        if (destinationMarker != null)
        {
            destinationMarker.transform.position = new Vector3(position.x, position.y + 0.5f, position.z);
            destinationMarker.SetActive(true);
        }
    }

    public void ClearMarkers()
    {
        if (userMarker != null)
            userMarker.SetActive(false);
        if (destinationMarker != null)
            destinationMarker.SetActive(false);
    }

    public void CenterOnPosition(Vector3 position)
    {
        if (mapCamera != null)
        {
            Vector3 camPos = mapCamera.transform.position;
            mapCamera.transform.position = new Vector3(position.x, camPos.y, position.z);
        }
    }

    public void ZoomIn()
    {
        if (mapCamera != null)
            mapCamera.orthographicSize = Mathf.Max(minZoom, mapCamera.orthographicSize - zoomSpeed);
    }

    public void ZoomOut()
    {
        if (mapCamera != null)
            mapCamera.orthographicSize = Mathf.Min(maxZoom, mapCamera.orthographicSize + zoomSpeed);
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        isFollowing = (target != null);
    }

    public Vector3 GetUserPosition() => userPosition;
    public Vector3 GetDestinationPosition() => destinationPosition;
}
