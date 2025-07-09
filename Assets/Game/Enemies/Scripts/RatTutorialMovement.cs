using System;
using System.Collections;
using Game.Core.Managers;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Game.Enemies.Scripts
{
    public class RatTutorialMovement : MonoBehaviour
    {
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        [SerializeField] private  float speed = 5f;
        [SerializeField] private  float maxMovement = 5f;
        [SerializeField] private  float minMovement = 2f;


        private Vector2 _selectedDirection;
        private float _targetDistance;
        private Vector2 _startPosition;
        private bool _isMoving;
        
        private float _moveTimer;
        private readonly float _moveInterval = 1f;

        [SerializeField] private float leftMovement = 45f;
        [SerializeField] private float rightMovement = 135f;
        
        [Header("animations")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualTransform;
        [SerializeField] private GameObject frontSkeleton;
        [SerializeField] private GameObject backSkeleton;
        
        [Header("Flip")]
        public SpriteRenderer forwardSpriteRenderer;
        public SpriteRenderer backwardSpriteRenderer;
        
        public SkeletonMecanim forwardSkeletonMecanim;
        public SkeletonMecanim backwardSkeletonMecanim;
        public bool StopedMoving { get; set; }
       

        private void OnEnable()
        {
            GameEvents.OnMouseCatch += OnRatCaught;
        }

        private void OnDisable()
        {
            GameEvents.OnMouseCatch -= OnRatCaught;
        }
        
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            PickNewDirection();
            StartCoroutine(Move());
        }


        private void PickNewDirection()
        {
            _selectedDirection = GetDirection(180+rightMovement);
            _targetDistance = GetRandomDistance();
            _startPosition = transform.position;
            
            UpdateActiveSkeleton(_selectedDirection.y);
            FlipSprite(_selectedDirection.x);
        }
        
        private IEnumerator Move()
        {
            while (true)
            {
                transform.Translate(_selectedDirection * (speed * Time.deltaTime));
                yield return null;
                float distanceMoved = Vector2.Distance(_startPosition, transform.position);
                if (distanceMoved >= _targetDistance)
                {
                    StopedMoving = true;
                    StopMovementAndShowFront();
                    break;
                }
            }
        }



        private Vector2 GetDirection(float direction)
        {
            float[] allowedAngles = { direction };
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
            // Force back skeleton to hide, front to show
            animator.SetBool(IsWalking, false);
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
        }
    }
}
