using UnityEngine;

public class BillboardController : MonoBehaviour
{
    [SerializeField] private bool trackMainCamera = true;
    [SerializeField] public Transform trackingTarget;
    
    [SerializeField] private bool faceAwayFromTarget = true;
    [SerializeField] private bool tiltVertically;
    [SerializeField] private bool slerpRotation = true;
    [SerializeField] private float slerpSpeed = 7f;
    
    private Vector3 _oldForward;

    void Start()
    {
        _oldForward = transform.forward;
        if (trackMainCamera)
            if (Camera.main != null)
                trackingTarget = Camera.main.transform;

        if (trackingTarget == null)
        {
            Debug.Log("No tracking target set for " + gameObject.name);
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        _oldForward = transform.forward;
        var position = transform.position;
        
        var targetPosition = trackingTarget.position;
        if (!tiltVertically)
            targetPosition.y = position.y;
        
        var newForward = faceAwayFromTarget ? position - targetPosition : targetPosition - position;
        
        transform.forward = slerpRotation ? Vector3.Slerp(_oldForward, newForward, slerpSpeed * Time.deltaTime) : newForward;
    }
}
