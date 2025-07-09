using Game.Core.Audio;
using Game.Core.Managers;
using Spine.Unity;
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

        private readonly float _leftMovement = 225f;
        private readonly float _rightMovement = 315f;
        
        [Header("animations")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private GameObject frontSkeleton;
        [SerializeField] private GameObject backSkeleton;
        private bool _isDead;
        
        [Header("Flip")]
        public SpriteRenderer forwardSpriteRenderer;
        public SpriteRenderer backwardSpriteRenderer;
        
        public SkeletonMecanim forwardSkeletonMecanim;
        public SkeletonMecanim backwardSkeletonMecanim;
        
        [Header("just created")]
        private bool _isJustCreated;
        private int _justCreatedDirection;



        private void OnEnable()
        {
            GameEvents.OnMouseCatch += OnRatCaught;
            _isJustCreated = true;
            _isDead = false;
        }

        private void OnDisable()
        {
            GameEvents.OnMouseCatch -= OnRatCaught;
        }
        
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            PickNewDirection();
            _isDead = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (_isDead) return;
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
            if (_isJustCreated)
            {
                Debug.Log(" Distance: " + _targetDistance + "  Direction: " + _randomDirection);
            }
            
            _isJustCreated = false;
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
            float[] allowedAngles = { _leftMovement, _rightMovement, 180+_leftMovement, 180+_rightMovement };
            var angle = _isJustCreated ? JustCreatedDirection() : allowedAngles[Random.Range(0, allowedAngles.Length)];
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
            if (_isJustCreated)
            {
                return 10f;
            }
            
            return Random.Range(minMovement, maxMovement);
        }
        
        private bool CheckIfWall()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _randomDirection, 1f, wallLayer);
            return hit.collider != null;
        }
        
        private void OnRatCaught(Vector3 pos)
        {
            // Only affect this rat
            if (Vector3.Distance(transform.position, pos) > 0.1f) return;

            StopMovementAndShowFront();
        }
        
        private void StopMovementAndShowFront()
        {
            string nextLayer = "Platform";
            if (forwardSpriteRenderer != null)
            {
                forwardSpriteRenderer.sortingLayerName = nextLayer;
            }

            // Backwards SpriteRenderer
            if (backwardSpriteRenderer != null)
            {
                backwardSpriteRenderer.sortingLayerName = nextLayer;
            }

            // Forwards SkeletonMecanim
            if (forwardSkeletonMecanim != null)
            {
                var meshRenderer = forwardSkeletonMecanim.GetComponent<Renderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingLayerName = nextLayer;
                }
            }

            // Backwards SkeletonMecanim
            if (backwardSkeletonMecanim != null)
            {
                var meshRenderer = backwardSkeletonMecanim.GetComponent<Renderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingLayerName = nextLayer;
                }
            }
            _isMoving = false;
            _isDead = true;

            // Force back skeleton to hide, front to show
            frontSkeleton.SetActive(true);
            backSkeleton.SetActive(false);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.isTrigger || !other.CompareTag("BackCollider"))
                return;

            // Forwards SpriteRenderer
            if (forwardSpriteRenderer != null)
            {
                string nextLayer = forwardSpriteRenderer.sortingLayerName != "Enemy" ? "Enemy" : "Background";
                forwardSpriteRenderer.sortingLayerName = nextLayer;
            }

            // Backwards SpriteRenderer
            if (backwardSpriteRenderer != null)
            {
                string nextLayer = backwardSpriteRenderer.sortingLayerName != "Enemy" ? "Enemy" : "Background";
                backwardSpriteRenderer.sortingLayerName = nextLayer;
            }

            // Forwards SkeletonMecanim
            if (forwardSkeletonMecanim != null)
            {
                var meshRenderer = forwardSkeletonMecanim.GetComponent<Renderer>();
                if (meshRenderer != null)
                {
                    string current = meshRenderer.sortingLayerName;
                    string nextLayer = current != "Enemy" ? "Enemy" : "Background";
                    meshRenderer.sortingLayerName = nextLayer;
                }
            }

            // Backwards SkeletonMecanim
            if (backwardSkeletonMecanim != null)
            {
                var meshRenderer = backwardSkeletonMecanim.GetComponent<Renderer>();
                if (meshRenderer != null)
                {
                    string current = meshRenderer.sortingLayerName;
                    string nextLayer = current != "Enemy" ? "Enemy" : "Background";
                    meshRenderer.sortingLayerName = nextLayer;
                }
            }
        }
        private float JustCreatedDirection()
        {
            int finalDirection = -1; // -1 means none found

            // Define static positions for each spawn hole
            Vector2 hole1 = new Vector2(25, 0);
            Vector2 hole2 = new Vector2(2, 7);
            Vector2 hole3 = new Vector2(0, -15);
            Vector2[] spawnHoles = { hole1, hole2, hole3 };

            if (_isJustCreated)
            {
                float closestDistance = Mathf.Infinity;
                Vector2 currentPos = transform.position;

                for (int i = 0; i < spawnHoles.Length; i++)
                {
                    float distance = Vector2.Distance(currentPos, spawnHoles[i]);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        finalDirection = i;
                    }
                }
            }

            // Return a movement angle depending on the closest hole
            switch (finalDirection)
            {
                case 0:
                    return _leftMovement;
                case 1:
                    return _rightMovement;
                case 2:
                    return 180 + _rightMovement;
                default:
                    return 0f; // fallback angle
            }
        }
    }
}
