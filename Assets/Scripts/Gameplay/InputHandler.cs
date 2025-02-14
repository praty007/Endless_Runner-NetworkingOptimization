using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [Header("Touch Settings")]
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float jumpForceThreshold = 100f;

    private Vector2 touchStartPosition;
    private bool isTouching;

    public delegate void OnSwipeEvent(float direction);
    public delegate void OnJumpEvent(float force);

    public event OnSwipeEvent OnSwipe;
    public event OnJumpEvent OnJump;

    void Update()
    {
        HandleTouchInput();
        HandleKeyboardInput();
    }

    private void HandleTouchInput()
    {
        // Handle touch input for mobile devices
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    isTouching = true;
                    break;

                case TouchPhase.Ended:
                    if (isTouching)
                    {
                        Vector2 swipeDelta = touch.position - touchStartPosition;
                        float swipeDistance = swipeDelta.magnitude;

                        if (swipeDistance >= minSwipeDistance)
                        {
                            // Normalize the swipe direction
                            Vector2 swipeDirection = swipeDelta.normalized;

                            // Check if it's a horizontal swipe (left/right)
                            if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
                            {
                                OnSwipe?.Invoke(swipeDirection.x);
                            }
                            // Check if it's a vertical swipe (jump)
                            else if (swipeDirection.y > 0 && swipeDistance >= jumpForceThreshold)
                            {
                                float jumpForce = Mathf.Min(swipeDistance / jumpForceThreshold, 2f);
                                OnJump?.Invoke(jumpForce);
                            }
                        }
                    }
                    isTouching = false;
                    break;
            }
        }
    }

    private void HandleKeyboardInput()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
        // Handle horizontal movement with arrow keys and WASD
        float horizontalInput = 0f;
        
        // Arrow keys
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            horizontalInput = -1f;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            horizontalInput = 1f;
        }

        if (horizontalInput != 0f)
        {
            OnSwipe?.Invoke(horizontalInput);
        }

        // Handle jumping with Up Arrow and W key
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            OnJump?.Invoke(4f); // Use a default jump force for keyboard input
        }
        #endif
    }
}
