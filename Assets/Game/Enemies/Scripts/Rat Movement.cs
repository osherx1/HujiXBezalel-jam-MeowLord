using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Enemies.Scripts
{
    public class RatMovement : MonoBehaviour
    {
        [SerializeField] private  float speed = 5f;
        [SerializeField] private  float maxMovement = 5f;
        [SerializeField] private  float minMovement = 2f;
        [SerializeField] private LayerMask wallLayer;


        private Vector2 _randomDirection;
        private float _targetDistance;
        private Vector2 _startPosition;
        private bool _isMoving;
        
        private float _moveTimer;
        private readonly float _moveInterval = 1f;

        [SerializeField] private float leftMovement = 45f;
        [SerializeField] private float rightMovement = 135f;
        
        [Header("animations")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private GameObject frontSkeleton;
        [SerializeField] private GameObject backSkeleton;

        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            PickNewDirection();
        }

        // Update is called once per frame
        void Update()
        {
            _moveTimer += Time.deltaTime;

            if (!_isMoving && _moveTimer >= _moveInterval)
            {
                PickNewDirection();
                _moveTimer = 0f; // reset the timer after starting movement
            }

            if (_isMoving)
            {
                Move();
            }
        }
        
        
        private void PickNewDirection()
        {
            _randomDirection = GetRandomDirection();
            _targetDistance = GetRandomDistance();
            _startPosition = transform.position;

            // Wall check: pick a new direction if there's a wall
            while (CheckIfWall())
            {
                _randomDirection = GetRandomDirection();
            }

            _isMoving = true;
            UpdateActiveSkeleton(_randomDirection.y);
            FlipSprite(_randomDirection.x);
        }
        
        private void Move()
        {
            if (!_isMoving) return;

            // Check for wall in the current direction before moving
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _randomDirection, speed * Time.deltaTime + 0.1f, wallLayer);
            if (hit.collider != null)
            {
                _isMoving = false; // Stop if there's a wall ahead
                return;
            }

            // Move normally
            transform.Translate(_randomDirection * (speed * Time.deltaTime));

            float distanceMoved = Vector2.Distance(_startPosition, transform.position);
            if (distanceMoved >= _targetDistance)
            {
                _isMoving = false;
            }
        }
        
        private Vector2 GetRandomDirection()
        {
            float[] allowedAngles = { leftMovement, rightMovement, 180+leftMovement, 180+rightMovement };
            float angle = allowedAngles[Random.Range(0, allowedAngles.Length)];
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }
        
        private void UpdateActiveSkeleton(float yDirection)
        {
            bool isMovingForward = yDirection < 0;

            frontSkeleton.SetActive(isMovingForward);
            backSkeleton.SetActive(!isMovingForward);
        }

        
        private void FlipSprite(float xDirection)
        {
            if (visualTransform == null) return;

            bool isBackActive = backSkeleton.activeSelf;

            // Normally, front faces right, back faces left (or vice versa depending on your art)
            float flipDirection = xDirection < 0 ? -1f : 1f;

            // If the active skeleton is flipped in design, invert the flip
            if (isBackActive)
            {
                // Invert flip if front skeleton is mirrored in the art
                flipDirection *= -1f;
            }

            Vector3 scale = visualTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * flipDirection;
            visualTransform.localScale = scale;
        }

        
        private float GetRandomDistance()
        {
            return Random.Range(minMovement, maxMovement);
        }
        
        private bool CheckIfWall()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _randomDirection, 1f, wallLayer);
            return hit.collider != null;
        }
    }
}
