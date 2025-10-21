using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerCharacter : MonoBehaviour
{
    #region DataStructure

    public enum PhysicState
    {
        Ground,
        Air
    }

    [Serializable]
    private struct MovementValues
    {
        public float MaxSpeed;
        public float Acceleration;
        public float MaxAcceleration;
        [Tooltip("Range [-1, 1]")] public AnimationCurve AccelerationRemapFromVelocityDot;
        
    }



    [Serializable]
    private struct GravityValues
    {
        public float MaxForce;
        public float Acceleration;
        public float MaxAcceleration;
        public float CoyoteTime;
        [Tooltip("Range [0, 1]")] public AnimationCurve GravityRemapFromCoyoteTime;
    }

    [Serializable]
    private struct JumpValues
    {
        public float ImpulseForce;
        public float Deceleration;
        public float MaxDeceleration;
        [Tooltip("Range [0, 1]")] public AnimationCurve DecelerationFromAirTime;
        public float Height;
        public float BufferTime;
    }

    #endregion DataStructure


    [Header("Gameplay")]
    [SerializeField] private MovementValues _SprintPhysic = new MovementValues();
    [SerializeField] private MovementValues _groundPhysic = new MovementValues();
    [SerializeField] private MovementValues _airPhysic = new MovementValues();
    [SerializeField] private GravityValues _gravityParameters = new GravityValues();
    [SerializeField] private JumpValues _jumpParameters = new JumpValues();
    [SerializeField] private ContactFilter2D _groundContactFilter = new ContactFilter2D();

    [Header("Setup")]
    [SerializeField] private Transform _mesh = null;
    [SerializeField] private float _meshRotationSpeed = 10.0f;

    

    //Components
    private Rigidbody2D _rigidbody = null;

    //Force
    private Vector2 _forceToAdd = Vector2.zero;

    //Horizontal movement
    private float _currentHorizontalVelocity = 0.0f;
    private float _movementInput = 0.0f;
    private MovementValues _horizontalPhysic = new MovementValues();
    public bool isSprinting = false;

    //Gravity
    private float _currentGravity = 0.0f;
    private float _gravityDirection = 1f;

    //Ground
    private bool _isGrounded = true;

    //Air
    private float _airTime = 0.0f;
    private bool _isInCoyoteTime = false;

    //Jump (n'est marcha pas gravity inversé)
    private float _currentJumpForce = 0.0f;
    private bool _isJumping = false;
    private float _jumpTime = 0.0f;
    private float _startJumpTime = 0.0f;
    private bool _bufferJump = false;

    //Event appele quand on touche ou quitte le sol
    public event Action<PhysicState> OnPhysicStateChanged;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _horizontalPhysic = _groundPhysic;
        CalculateJumpTime();
        //On enregistre le changement de physic a l'event qui detecte le changement d'�tat du sol
        OnPhysicStateChanged += ChangePhysic;
        OnPhysicStateChanged += ResetGravity;
        OnPhysicStateChanged += CancelJump;
        OnPhysicStateChanged += TryJumpBuffer;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CalculateJumpTime();
    }
#endif

    private void CalculateJumpTime()
    {
        _jumpTime = _jumpParameters.Height / _jumpParameters.ImpulseForce;
    }

    private void Update()
    {
        RotateMesh();
    }

    private void FixedUpdate()
    {
        //On reset la force a ajouter cette boucle de fixed update
        _forceToAdd = Vector2.zero;

        //Fonction qui detecte si on touche le sol ou non
        //Et appelle les events associes
        GroundDetection();
        ManageAirTime();
        ManageCoyoteTime();

        //On effectue tous les calculs physiques
        Movement();
        Gravity();
        JumpForce();

        //On ajoute la force au rigidbody
        _rigidbody.velocity += _forceToAdd;
    }

    private void GroundDetection()
    {
        //On utilise le filtre qui contient l'inclinaison du sol pour savoir si le rigidbody touche le sol ou non
        bool isTouchingGround = _rigidbody.IsTouching(_groundContactFilter);

        //Si le rigidbody touche le sol mais on a en memoire qu'il ne le touche pas, on est sur la frame ou il touche le sol
        if (isTouchingGround && !_isGrounded)
        {
            _isGrounded = true;
            //On invoque l'event en passant true pour signifier que le joueur arrive au sol
            OnPhysicStateChanged.Invoke(PhysicState.Ground);
        }
        //Si le rigidbody ne touche pas le sol mais on a en memoire qu'il le touche, on est sur la frame ou il quitte le sol
        else if (!isTouchingGround && _isGrounded)
        {
            _isGrounded = false;
            if (!_isJumping)
                _isInCoyoteTime = true;
            //On invoque l'event en passant false pour signifier que le joueur quitte au sol
            OnPhysicStateChanged.Invoke(PhysicState.Air);
        }
    }

    private void ManageAirTime()
    {
        if (!_isGrounded)
            _airTime += Time.fixedDeltaTime;
    }

    private void ManageCoyoteTime()
    {
        if (_airTime > _gravityParameters.CoyoteTime)
            _isInCoyoteTime = false;
    }

    private void ChangePhysic(PhysicState groundState)
    {
        //On change la physique en fonction de si le joueur est au sol ou non
        if (groundState == PhysicState.Ground)
            _horizontalPhysic = _groundPhysic;
        else if (groundState == PhysicState.Air)
            _horizontalPhysic = _airPhysic;
    }

    private void Movement()
    {
        // Base values from current physics (ground/air)
        float baseMaxSpeed = _horizontalPhysic.MaxSpeed;
        float baseAcceleration = _horizontalPhysic.Acceleration;
        float baseMaxAcceleration = _horizontalPhysic.MaxAcceleration;

        // Apply sprint multipliers if sprinting
        if (isSprinting)
        {
            baseMaxSpeed = _SprintPhysic.MaxSpeed;
            baseAcceleration = _SprintPhysic.Acceleration;
            baseMaxAcceleration = _SprintPhysic.MaxAcceleration;
        }

        // Target speed
        float targetSpeed = baseMaxSpeed * _movementInput;

        // Remap acceleration using curve
        float velocityDot = Mathf.Clamp(_rigidbody.velocity.x * targetSpeed, -1f, 1f);
        velocityDot = _horizontalPhysic.AccelerationRemapFromVelocityDot.Evaluate(velocityDot);

        float appliedAcceleration = baseAcceleration * velocityDot * Time.fixedDeltaTime;

        // Move current horizontal velocity towards target
        _currentHorizontalVelocity = Mathf.MoveTowards(_currentHorizontalVelocity, targetSpeed, appliedAcceleration);

        // Calculate velocity delta and clamp it
        float velocityDelta = _currentHorizontalVelocity - _rigidbody.velocity.x;
        velocityDelta = Mathf.Clamp(velocityDelta, -baseMaxAcceleration, baseMaxAcceleration);

        // Apply force
        _forceToAdd.x += velocityDelta;
    }

    private void RotateMesh()
    {
        if (_currentHorizontalVelocity == 0.0f)
            return;

        //On r�cup�re la rotation acutelle du mesh
        float currentRotation = _mesh.eulerAngles.y;

        //On d�finit la rotation cible en fonction de la v�locit� du personnage
        //90 � droite / 270 � gauche
        float targetRotation = _currentHorizontalVelocity > 0.0f ? 90.0f : 270f;
        //float targetRotation2 = 270.0f;
        //if (_currentHorizontalVelocity > 0.0f)
        //    targetRotation2 = 90.0f;

        //On interpole les rotations
        float newRotation = Mathf.MoveTowards(currentRotation, targetRotation, _meshRotationSpeed * Time.deltaTime);

        //On applique la nouvelle rotation au mesh
        _mesh.rotation = Quaternion.Euler(0.0f, newRotation, 0.0f);
    }

    private void Gravity()
    {
        if (_isGrounded || _isJumping)
            return;

        float coyoteTimeRatio = Mathf.Clamp01(_airTime / _gravityParameters.CoyoteTime);
        float coyoteTimeFactor = _isInCoyoteTime ? _gravityParameters.GravityRemapFromCoyoteTime.Evaluate(coyoteTimeRatio) : 1.0f;
        float acceleration = _gravityParameters.Acceleration * coyoteTimeFactor * Time.fixedDeltaTime;

        _currentGravity = Mathf.MoveTowards(_currentGravity, _gravityParameters.MaxForce, acceleration);

        float velocityDelta = _currentGravity - _rigidbody.velocity.y;
        velocityDelta = Mathf.Clamp(velocityDelta, -_gravityParameters.MaxAcceleration, 0.0f);

        if (_gravityDirection > 0)
        {

            velocityDelta = Mathf.Clamp(velocityDelta, -_gravityParameters.MaxAcceleration, 0.0f);
        }
        else
        {

            velocityDelta = Mathf.Clamp(velocityDelta, 0.0f, _gravityParameters.MaxAcceleration);
        }

        _forceToAdd.y += velocityDelta * _gravityDirection;
    }

        public void ToggleGravity()
    {

        _gravityDirection *= -1f;
        _isJumping = false;
        _isGrounded = true;
        


        _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 1f);
        _currentGravity = 1f;
        _airTime = 0f;

        _mesh.localScale = new Vector3(_mesh.localScale.x, _mesh.localScale.y * -1f, _mesh.localScale.z);
        float flipOffset = -0.7f; 
        _mesh.localPosition += new Vector3(0f, flipOffset * _gravityDirection, 0f);
        StartJump();

    }

    private void ResetGravity(PhysicState physicState)
    {
        if (physicState != PhysicState.Air)
        {
            _currentGravity = 0.0f;
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 0.0f);
            _airTime = 0.0f;
        }
    }

    public void GetMovementInput(float input)
    {
        _movementInput = input;
    }

    public void StartJump()
    {
        if ((!_isGrounded && !_isInCoyoteTime) || _isJumping)
        {
            _bufferJump = true;
            Invoke(nameof(StopJumpBuffer), _jumpParameters.BufferTime);
            return;
        }

        Debug.Log("jumped");

        _currentJumpForce = _jumpParameters.ImpulseForce;
        _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, _currentJumpForce);
        _isJumping = true;
        _isInCoyoteTime = false;
        _startJumpTime = _airTime;
    }

    private void StopJumpBuffer()
    {
        _bufferJump = false;
    }

    private void JumpForce()
    {
        if (!_isJumping)
            return;

        float jumpTimeRatio = Mathf.Clamp01((_airTime - _startJumpTime) / _jumpTime);
        float deceleration = _jumpParameters.Deceleration * _jumpParameters.DecelerationFromAirTime.Evaluate(jumpTimeRatio) * Time.fixedDeltaTime;

        _currentJumpForce = Mathf.MoveTowards(_currentJumpForce, 0.0f, deceleration);

        float velocityDelta = _currentJumpForce - _rigidbody.velocity.y;
        velocityDelta = Mathf.Clamp(velocityDelta, -_jumpParameters.MaxDeceleration, 0.0f);

        _forceToAdd.y += velocityDelta;

        if (jumpTimeRatio >= 1.0f)
        {
            _isJumping = false;
            _currentJumpForce = 0.0f;
        }
    }

    private void CancelJump(PhysicState state)
    {
        if (state != PhysicState.Air)
        {
            _isJumping = false;
            _currentJumpForce = 0.0f;
        }
    }

    private void TryJumpBuffer(PhysicState state)
    {
        if (state != PhysicState.Air && _bufferJump)
        {
            StartJump();
            _bufferJump = false;
            CancelInvoke(nameof(StopJumpBuffer));
        }
    }

    public void ActionOne()
    {
        if (_movementInput != 0) 
        {
            isSprinting = !isSprinting;
        }
    }

    public void ActionTwo()
    {

    }
}