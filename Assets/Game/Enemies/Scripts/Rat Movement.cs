using UnityEngine;

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
        private float _moveInterval = 5f;
        
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
            transform.Translate(_randomDirection * speed * Time.deltaTime);

            float distanceMoved = Vector2.Distance(_startPosition, transform.position);
            if (distanceMoved >= _targetDistance)
            {
                _isMoving = false;
            }
        }
        
        private Vector2 GetRandomDirection()
        {
            float angle = Random.Range(0f, 360f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
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
