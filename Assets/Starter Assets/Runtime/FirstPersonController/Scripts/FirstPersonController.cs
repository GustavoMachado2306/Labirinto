using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		public float MoveSpeed = 4.0f;
		public float SprintSpeed = 6.0f;
		public float RotationSpeed = 1.0f;
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		public float JumpHeight = 1.2f;
		public float Gravity = -15.0f;

		[Space(10)]
		public float JumpTimeout = 0.1f;
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		public bool Grounded = true;
		public float GroundedOffset = -0.14f;
		public float GroundedRadius = 0.5f;
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		public GameObject CinemachineCameraTarget;
		public float TopClamp = 90.0f;
		public float BottomClamp = -90.0f;

		[Header("Crouch")]
		public float CrouchHeight = 1.0f;
		public float StandHeight = 2.0f;
		public float CrouchSpeed = 2.0f;
		public float CrouchCameraY = 0.5f;
		public float StandCameraY = 1.2f;
		public float CrouchTransitionSpeed = 10.0f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		// crouch
		private bool _isCrouching = false;

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
			}
		}

		private void Awake()
		{
			if (_mainCamera == null)
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError("Starter Assets package is missing dependencies.");
#endif
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			_controller.height = StandHeight;
			_controller.center = new Vector3(0, StandHeight / 2f, 0);
		}

		private void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			Crouch();
			Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
			SmoothCameraHeight();
		}

		private void GroundedCheck()
		{
			Vector3 spherePosition = new Vector3(
				transform.position.x,
				transform.position.y - GroundedOffset,
				transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			if (_input.look.sqrMagnitude >= _threshold)
			{
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Crouch()
		{
			// Le o teclado diretamente - mais confiavel que o Input System para este caso
			bool crouchKeyHeld = Keyboard.current != null && Keyboard.current.cKey.isPressed;

			if (crouchKeyHeld && !_isCrouching)
			{
				_isCrouching = true;
				_controller.height = CrouchHeight;
				_controller.center = new Vector3(0, CrouchHeight / 2f, 0);
			}
			else if (!crouchKeyHeld && _isCrouching)
			{
				_isCrouching = false;
				_controller.height = StandHeight;
				_controller.center = new Vector3(0, StandHeight / 2f, 0);
			}
		}

		private void SmoothCameraHeight()
		{
			float targetY = _isCrouching ? CrouchCameraY : StandCameraY;
			Vector3 current = CinemachineCameraTarget.transform.localPosition;
			CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
				current,
				new Vector3(current.x, targetY, current.z),
				Time.deltaTime * CrouchTransitionSpeed
			);
		}

		private void Move()
		{
			float targetSpeed;
			if (_isCrouching)
				targetSpeed = CrouchSpeed;
			else
				targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			if (_input.move != Vector2.zero)
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;

			_controller.Move(
				inputDirection.normalized * (_speed * Time.deltaTime) +
				new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
			);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				_fallTimeoutDelta = FallTimeout;

				if (_verticalVelocity < 0.0f)
					_verticalVelocity = -2f;

				if (_input.jump && _jumpTimeoutDelta <= 0.0f && !_isCrouching)
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

				if (_jumpTimeoutDelta >= 0.0f)
					_jumpTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				_jumpTimeoutDelta = JumpTimeout;

				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= Time.deltaTime;

				_input.jump = false;
			}

			if (_verticalVelocity < _terminalVelocity)
				_verticalVelocity += Gravity * Time.deltaTime;
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
			Gizmos.color = Grounded ? transparentGreen : transparentRed;
			Gizmos.DrawSphere(
				new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
				GroundedRadius
			);
		}
	}
}