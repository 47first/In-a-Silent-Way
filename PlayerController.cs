using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Vector3 LastFixedUpdatedPosition { get; private set; }
    private bool _isPlayerMoving;
    public bool IsPlayerMoving
    {
        get { return _isPlayerMoving; }
        private set
        {
            if (_isPlayerMoving == value)
                return;

            _isPlayerMoving = value;
        }
    }

    public float Speed {
        get { return _resultSpeed; }
        private set {
            _speed = value;
            _resultSpeed = _speed * _speedMultiplier;
        }
    }
    private float SpeedMultiplier
    {
        set {
            _speedMultiplier = value;
            _resultSpeed = _speed * _speedMultiplier;
        }
    }

    private float _resultSpeed;
    private float _speed;
    private float _speedMultiplier;

    private Vector3 targetPos;

    private CharacterController playerCC;
    public float Gravity { get; private set; }

    #region Grab

    [Header("Grab")]
    [SerializeField] private float grabCooldown;
    [SerializeField] private LayerMask grabLayerMask;
    private float lastTimeGrab;

    [SerializeField] private List<Transform> freeSpaceDirections;
    [SerializeField] private List<Transform> canGrabDirections;

    [SerializeField] private float freeSpaceDistance;
    [SerializeField] private float canGrabDistance;
    [SerializeField] private float minGrabAngle;

    [SerializeField] private Transform fixedTransform;
    private Quaternion fixedRotation;
    private Vector3 fixedPoint;
    [SerializeField] private float fixedPointRelativeHeight;

    public event System.Action OnFixedPosition;

    private bool _isFixedPos;
    public bool IsFixedPos
    {
        get { return _isFixedPos; }
        private set
        {
            _isFixedPos = value;

            if (value)
            {
                OnFixedPosition();
                lastTimeGrab = 0;
                fixedRotation = transform.rotation;
            }
        }
    }

    #endregion

    #region Is Any Obstacle Raycasting
    [Header("Is Any Obstacle Raycasting")]
    [SerializeField] private LayerMask raycastLayerMask;

    public event PlayerLandedDelegate OnPlayerLanded;
    public delegate void PlayerLandedDelegate(float gravity);
    private bool _isOnGround;
    private bool isUpAnyObstacle;
    public bool IsOnGround
    {
        get { return _isOnGround; }
        private set
        {
            if (_isOnGround == value)
                return;

            _isOnGround = value;

            if (_isOnGround)
            {
                OnPlayerLanded(Gravity);
                Gravity = -10;
            }
        }
    }

    private float groundDistance;
    private float fromHeadDistance;

    #endregion

    #region Jump
    [Header("Jump")]
    [SerializeField] private float jumpCooldown;
    [SerializeField] private AnimationCurve JumpCurve;

    private float lastTimeJump;

    private float jumpCurveMaxTime;
    private float jumpCurveCurTime;
    #endregion

    #region Crouch
    [Header("Counch")]
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchHeight;
    [SerializeField] private Transform cameraTarget;
    private float normalSpeed;
    private float normalHeight;

    private float targetHeight;
    [SerializeField] private float heightChangeSpeed;

    private bool _isCrouching;
    private bool IsCrouching { get { return _isCrouching; }
        set
        {
            _isCrouching = value;
            if (value)
            {
                IsRuning = false;
                targetHeight = crouchHeight;
                Speed = crouchSpeed;
                cameraTarget.localPosition = new Vector3(0, crouchHeight / 2 - 0.5f, 0);
            }
            else
            {
                targetHeight = normalHeight;
                Speed = normalSpeed;
                cameraTarget.localPosition = new Vector3(0, normalHeight / 2 - 0.5f, 0);
            }
        }
    }
    #endregion

    #region Run

    private bool _isRuning;
    private bool IsRuning {
        get { return _isRuning; }
        set {
            _isRuning = value && !IsCrouching;
            if (_isRuning)
                SpeedMultiplier = 1.75f;
            else
                SpeedMultiplier = 1;
        }
    }

    #endregion

    private void Start()
    {
        playerCC = GetComponent<CharacterController>();

        IsFixedPos = false;
        lastTimeGrab = 0;
        lastTimeJump = 0;

        Gravity = JumpCurve.keys[0].value;
        jumpCurveCurTime = jumpCurveMaxTime = JumpCurve.keys[JumpCurve.length - 1].time;

        _isCrouching = false;
        normalHeight = playerCC.height;
        targetHeight = normalHeight;

        IsRuning = false;
        Speed = 8;
        normalSpeed = Speed;

        IsPlayerMoving = false;
        LastFixedUpdatedPosition = this.transform.position;
    }

    private void Update()
    {
        if (targetHeight != playerCC.height)
            playerCC.height = Mathf.Lerp(targetHeight, playerCC.height, heightChangeSpeed * Time.deltaTime);

        float isOnGroundDistance = playerCC.height / 2 + 0.2f;

        groundDistance = Check9Positions(Vector3.down, playerCC.radius, 5);
        fromHeadDistance = Check5Positions(Vector3.up, playerCC.radius, 5);

        IsOnGround = groundDistance < (IsCrouching ? isOnGroundDistance + 0.5f : isOnGroundDistance);
        isUpAnyObstacle = fromHeadDistance < isOnGroundDistance;

        GrabOnInput();
        if (!IsFixedPos)
        {
            lastTimeGrab += Time.deltaTime;

            Vector3 value = GetVector3ByInput();
            IsPlayerMoving = value != Vector3.zero;
            if (IsPlayerMoving)
                RunOnInput();
            targetPos = Vector3.Lerp(targetPos, value, 10 * Time.deltaTime);

            Move(targetPos, transform);
            CrouchOnInput();
        }
        else if (!CheckCanGrab())
        {
            IsFixedPos = false;
        }
        else if (Input.GetKeyDown(GameManager.InputSettings.playerJump))
        {
            IsFixedPos = false;
            lastTimeJump = 0;
            jumpCurveCurTime = 0;
        }

        #region Raycast Debug
        //isOnGroundDistance
        Debug.DrawRay(this.transform.position + Vector3.left * (playerCC.radius + 2), this.transform.TransformDirection(Vector3.up) * isOnGroundDistance, Color.yellow);
        #endregion
    }

    private void FixedUpdate()
    {
        LastFixedUpdatedPosition = this.transform.position;
    }
    private void RunOnInput()
    {
        if (!IsRuning && Input.GetKey(GameManager.InputSettings.playerRun))
        {
            IsRuning = true;
        }
        else if (IsRuning && !Input.GetKey(GameManager.InputSettings.playerRun))
        {
            IsRuning = false;
        }
    }
    private void CrouchOnInput()
    {
        bool isHoldMode = GameManager.InputSettings.crouchMode == InputSettings.CrouchMode.Hold;

        if (IsCrouching)
        {
            bool isKeyPressed = isHoldMode ? !Input.GetKey(GameManager.InputSettings.playerCrouch) :
                (Input.GetKeyDown(GameManager.InputSettings.playerJump) || Input.GetKeyDown(GameManager.InputSettings.playerCrouch));

            bool isHeadDistanceMoreThanNormal = fromHeadDistance > normalHeight / 2;
            if (isHeadDistanceMoreThanNormal && isKeyPressed)
            {
                IsCrouching = false;
            }
        }

        else if (IsOnGround && (isHoldMode ? Input.GetKey(GameManager.InputSettings.playerCrouch) : Input.GetKeyDown(GameManager.InputSettings.playerCrouch)))
        {
            IsCrouching = true;
        }
    }
    private void Move(Vector3 direction, Transform relative)
    {
        playerCC.Move(Time.deltaTime * ((Speed * relative.TransformDirection(direction)) + Vector3.up * GetYValue()));
    }

    /*
    private void Move(Vector3 direction, Transform relative)
    {
        if (!IsFixedPos)
        {
            lastTimeGrab += Time.deltaTime;
            playerCC.Move(Time.deltaTime * ((Speed * relative.TransformDirection(direction)) + Vector3.up * GetYValue()));
        }
        else if (!TryToGrab()) 
        {
            IsFixedPos = false;
        }
        else if (Input.GetKeyDown(GameManager.InputSettings.playerJump))
        {
            IsFixedPos = false;
            lastTimeJump = 0;
            jumpCurveCurTime = 0;
        }
    }
    */

    private Vector3 GetVector3ByInput()
    {
        Vector3 res = Vector3.zero;

        if (Input.GetKey(GameManager.InputSettings.playerMoveForward))
            res.z += 1;
        if (Input.GetKey(GameManager.InputSettings.playerMoveBack))
            res.z -= 1;
        if (Input.GetKey(GameManager.InputSettings.playerMoveRight))
            res.x += 1;
        if (Input.GetKey(GameManager.InputSettings.playerMoveLeft))
            res.x -= 1;

        return res;
    }
    private float GetYValue()
    {
        if (IsOnGround)
        {
            lastTimeJump += Time.deltaTime;
            if (!IsCrouching && (fromHeadDistance > 1.3f) && lastTimeJump > jumpCooldown && Input.GetKeyDown(GameManager.InputSettings.playerJump))
            {
                lastTimeJump = jumpCurveCurTime = 0;
            }
        }

        if (isUpAnyObstacle)
            jumpCurveCurTime = jumpCurveMaxTime;

        if (jumpCurveCurTime <= jumpCurveMaxTime)
        {
            Gravity = JumpCurve.Evaluate(jumpCurveCurTime);
            jumpCurveCurTime += Time.deltaTime;
        }
        else if (!IsOnGround) Gravity -= 25 * Time.deltaTime;

        return Gravity;
    }

    private void GrabOnInput() 
    {
        if (!IsOnGround && !IsCrouching && lastTimeGrab > grabCooldown && Input.GetKey(GameManager.InputSettings.playerJump))
        {
            if (CheckCanGrab())
            {
                IsFixedPos = true;
                jumpCurveCurTime = jumpCurveMaxTime;
            }
        }
    }

    private bool CheckCanGrab()
    {
        bool isFreeSpace = true;

        fixedTransform.position = transform.position;
        if (IsFixedPos)
            fixedTransform.rotation = fixedRotation;
        else
            fixedTransform.rotation = transform.rotation;

        #region Debug
        foreach (Transform dirD in freeSpaceDirections)
            Debug.DrawRay(dirD.position, dirD.TransformDirection(Vector3.forward) * freeSpaceDistance, Color.red);

        foreach (Transform dirD in canGrabDirections)
            Debug.DrawRay(dirD.position, dirD.TransformDirection(Vector3.forward) * canGrabDistance, Color.green);
        #endregion

        foreach (Transform dir in freeSpaceDirections)
        {
            if (Physics.Raycast(dir.position, dir.TransformDirection(Vector3.forward),
                freeSpaceDistance, grabLayerMask))
            {
                isFreeSpace = false;
                break;
            }
        }

        if (!isFreeSpace)
            return false;

        bool canGrab = true;
        foreach (Transform dir in canGrabDirections)
        {
            RaycastHit hit;
            if (!Physics.Raycast(dir.position, dir.TransformDirection(Vector3.forward), out hit, canGrabDistance, grabLayerMask) ||
            (Mathf.Asin(hit.normal.y) * Mathf.Rad2Deg < minGrabAngle))
            {
                canGrab = false;
                break;
            }

            //Debug.Log(Mathf.Asin(hit.normal.y) * Mathf.Rad2Deg);
        }

        if (!canGrab)
            return false;

        return true;
    }

    /*
     
    */

    /*
    private bool CheckCanGrab() 
    {
        
    }
    */

    //Is Up or Down any colliders check
    private float Check5Positions(Vector3 direction, float radius, float maxDistance)
    {
        RaycastHit hit1, hit2, hit3, hit4, hit5;

        bool isOneHit = Physics.Raycast(transform.position, direction, out hit1, maxDistance, raycastLayerMask);
        bool isTwoHit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.forward) * radius, direction, out hit2, maxDistance, raycastLayerMask);
        bool isThreeHit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.back) * radius, direction, out hit3, maxDistance, raycastLayerMask);
        bool isFourHit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.right) * radius, direction, out hit4, maxDistance, raycastLayerMask);
        bool isFiveHit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.left) * radius, direction, out hit5, maxDistance, raycastLayerMask);

        bool isAnyHit = isOneHit || isTwoHit || isThreeHit || isFourHit || isFiveHit;

        float firstDistance = isOneHit ? hit1.distance : maxDistance;
        float secondDistance = isTwoHit ? hit2.distance : maxDistance;
        float thirdDistance = isThreeHit ? hit3.distance : maxDistance;
        float fourthDistance = isFourHit ? hit4.distance : maxDistance;
        float fifthDistance = isFiveHit ? hit5.distance : maxDistance;

        float minDistance = isAnyHit ? Mathf.Min(firstDistance, secondDistance, thirdDistance, fourthDistance, fifthDistance) : maxDistance;

        #region Debug

        Debug.DrawRay(transform.position, transform.TransformDirection(direction) * minDistance, Color.blue);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.forward) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.blue);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.back) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.blue);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.right) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.blue);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.left) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.blue);

        #endregion

        return minDistance;
    }
    private float Check9Positions(Vector3 direction, float radius, float maxDistance)
    {
        RaycastHit hit1, hit2, hit3, hit4, hit5, hit6, hit7, hit8, hit9;

        bool is1Hit = Physics.Raycast(transform.position, direction, out hit1, maxDistance, raycastLayerMask);
        bool is2Hit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.forward) * radius, direction, out hit2, maxDistance, raycastLayerMask);
        bool is3Hit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.back) * radius, direction, out hit3, maxDistance, raycastLayerMask);
        bool is4Hit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.right) * radius, direction, out hit4, maxDistance, raycastLayerMask);
        bool is5Hit = Physics.Raycast(transform.position + transform.TransformDirection(Vector3.left) * radius, direction, out hit5, maxDistance, raycastLayerMask);

        bool is6Hit = Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0.5f, 0, 0.5f)) * radius, direction, out hit6, maxDistance, raycastLayerMask);
        bool is7Hit = Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(-0.5f, 0, 0.5f)) * radius, direction, out hit7, maxDistance, raycastLayerMask);
        bool is8Hit = Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(-0.5f, 0, -0.5f)) * radius, direction, out hit8, maxDistance, raycastLayerMask);
        bool is9Hit = Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0.5f, 0, -0.5f)) * radius, direction, out hit9, maxDistance, raycastLayerMask);

        bool isAnyHit = is1Hit || is2Hit || is3Hit || is4Hit || is5Hit || is6Hit || is7Hit || is8Hit || is9Hit;

        float firstDistance = is1Hit ? hit1.distance : maxDistance;
        float secondDistance = is2Hit ? hit2.distance : maxDistance;
        float thirdDistance = is3Hit ? hit3.distance : maxDistance;
        float fourthDistance = is4Hit ? hit4.distance : maxDistance;
        float fifthDistance = is5Hit ? hit5.distance : maxDistance;
        float sixthDistance = is6Hit ? hit6.distance : maxDistance;
        float seventhDistance = is7Hit ? hit7.distance : maxDistance;
        float eighthDistance = is8Hit ? hit8.distance : maxDistance;
        float ninethDistance = is9Hit ? hit9.distance : maxDistance;

        float minDistance = isAnyHit ? Mathf.Min(firstDistance, secondDistance, thirdDistance, fourthDistance, fifthDistance,
            sixthDistance, seventhDistance, eighthDistance, ninethDistance) : maxDistance;

        #region Debug

        Debug.DrawRay(transform.position, transform.TransformDirection(direction) * minDistance, Color.blue);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.forward) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.back) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.right) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);
        Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.left) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0.5f, 0, 0.5f)) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);
        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(-0.5f, 0, 0.5f)) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);
        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(-0.5f, 0, -0.5f)) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);
        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0.5f, 0, -0.5f)) * playerCC.radius, this.transform.TransformDirection(direction) * minDistance, Color.cyan);

        #endregion

        return minDistance;
    }
}