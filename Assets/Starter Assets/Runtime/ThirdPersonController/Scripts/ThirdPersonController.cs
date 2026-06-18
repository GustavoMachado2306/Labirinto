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
	public class ThirdPersonController : MonoBehaviour
	{
		// ─────────────────────────────────────────────
		// MOVIMENTO
		// ─────────────────────────────────────────────
		[Header("Player Movement")]
		[Tooltip("Velocidade de caminhada em m/s")]
		public float MoveSpeed = 2.0f;

		[Tooltip("Velocidade de corrida em m/s")]
		public float SprintSpeed = 5.335f;

		[Tooltip("Quão rápido o personagem gira para encarar a direção do movimento")]
		[Range(0.0f, 0.3f)]
		public float RotationSmoothTime = 0.12f;

		[Tooltip("Aceleração e desaceleração")]
		public float SpeedChangeRate = 10.0f;

		// ─────────────────────────────────────────────
		// AGACHAR
		// ─────────────────────────────────────────────
		[Header("Crouch")]
		[Tooltip("Altura do CharacterController ao agachar")]
		public float CrouchHeight = 1.0f;

		[Tooltip("Altura do CharacterController em pé (valor padrão do componente)")]
		public float StandHeight = 1.8f;

		[Tooltip("Velocidade de movimento ao agachar")]
		public float CrouchSpeed = 1.5f;

		[Tooltip("Posição Y da câmera ao agachar")]
		public float CrouchCameraY = 0.8f;

		[Tooltip("Posição Y da câmera em pé")]
		public float StandCameraY = 1.4f;

		[Tooltip("Velocidade de transição da câmera ao agachar/levantar")]
		public float CrouchTransitionSpeed = 10.0f;

		[Tooltip("Raio da checagem de teto (usa o raio do CharacterController)")]
		public float CeilingCheckRadius = 0.3f;

		[Tooltip("Layer do teto para checagem")]
		public LayerMask CeilingLayers;

		private bool _isCrouching = false;
		private bool _wantsToCrouch = false;

		// ─────────────────────────────────────────────
		// AUDIO
		// ─────────────────────────────────────────────
		[Header("Player Audio")]
		public AudioClip LandingAudioClip;
		public AudioClip[] FootstepAudioClips;
		[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

		// ─────────────────────────────────────────────
		// PULO E GRAVIDADE
		// ─────────────────────────────────────────────
		[Header("Player Jump & Gravity")]
		[Tooltip("Altura máxima do pulo em metros")]
		public float JumpHeight = 1.2f;

		[Tooltip("Gravidade aplicada ao personagem")]
		public float Gravity = -15.0f;

		[Tooltip("Tempo até poder pular novamente (0 = imediato)")]
		public float JumpTimeout = 0.50f;

		[Tooltip("Tempo até entrar na animação de queda (útil para escadas)")]
		public float FallTimeout = 0.15f;

		// ─────────────────────────────────────────────
		// CHÃO
		// ─────────────────────────────────────────────
		[Header("Player Grounded")]
		public bool Grounded = true;

		[Tooltip("Offset da checagem de chão (útil para terrenos irregulares)")]
		public float GroundedOffset = -0.14f;

		[Tooltip("Raio da esfera de checagem de chão")]
		public float GroundedRadius = 0.28f;

		[Tooltip("Layers consideradas chão")]
		public LayerMask GroundLayers;

		// ─────────────────────────────────────────────
		// CÂMERA (PRIMEIRA PESSOA)
		// ─────────────────────────────────────────────
		[Header("Cinemachine / Camera")]
		[Tooltip("O objeto filho CameraRoot — posicione na altura dos olhos")]
		public GameObject CinemachineCameraTarget;

		[Tooltip("Ângulo máximo de olhar para cima")]
		public float TopClamp = 70.0f;

		[Tooltip("Ângulo máximo de olhar para baixo")]
		public float BottomClamp = -30.0f;

		[Tooltip("Graus extras para rotação da câmera (lock override)")]
		public float CameraAngleOverride = 0.0f;

		[Tooltip("Travar a câmera em todos os eixos")]
		public bool LockCameraPosition = false;

		// ─────────────────────────────────────────────
		// PRIVADOS — CINEMACHINE
		// ─────────────────────────────────────────────
		private float _cinemachineTargetYaw;
		private float _cinemachineTargetPitch;

		// ─────────────────────────────────────────────
		// PRIVADOS — PLAYER
		// ─────────────────────────────────────────────
		private float _speed;
		private float _animationBlend;
		private float _targetRotation = 0.0f;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// Timers
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		// Animator IDs
		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;

		// Referências
#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private Animator _animator;
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;
		private bool _hasAnimator;

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

		// ─────────────────────────────────────────────
		// UNITY LIFECYCLE
		// ─────────────────────────────────────────────
		private void Awake()
		{
			if (_mainCamera == null)
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}

		private void Start()
		{
			_cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

			_hasAnimator = TryGetComponent(out _animator);
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#endif

			AssignAnimationIDs();

			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			// Garante altura inicial correta
			_controller.height = StandHeight;
			_controller.center = new Vector3(0, StandHeight / 2f, 0);

			// Trava cursor
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		private void Update()
		{
			_hasAnimator = TryGetComponent(out _animator);

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

		// ─────────────────────────────────────────────
		// ANIMATOR IDs
		// ─────────────────────────────────────────────
		private void AssignAnimationIDs()
		{
			_animIDSpeed = Animator.StringToHash("Speed");
			_animIDGrounded = Animator.StringToHash("Grounded");
			_animIDJump = Animator.StringToHash("Jump");
			_animIDFreeFall = Animator.StringToHash("FreeFall");
			_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
		}

		// ─────────────────────────────────────────────
		// CHÃO
		// ─────────────────────────────────────────────
		private void GroundedCheck()
		{
			Vector3 spherePosition = new Vector3(
				transform.position.x,
				transform.position.y - GroundedOffset,
				transform.position.z
			);

			Grounded = Physics.CheckSphere(
				spherePosition,
				GroundedRadius,
				GroundLayers,
				QueryTriggerInteraction.Ignore
			);

			if (_hasAnimator)
				_animator.SetBool(_animIDGrounded, Grounded);
		}

		// ─────────────────────────────────────────────
		// CÂMERA (PRIMEIRA PESSOA)
		// ─────────────────────────────────────────────
		private void CameraRotation()
		{
			if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
			{
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetYaw   += _input.look.x * deltaTimeMultiplier;
				_cinemachineTargetPitch -= _input.look.y * deltaTimeMultiplier;
			}

			_cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw,   float.MinValue, float.MaxValue);
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
				_cinemachineTargetPitch + CameraAngleOverride,
				_cinemachineTargetYaw,
				0.0f
			);

			// Corpo do personagem segue o yaw da câmera (modo FPS)
			transform.rotation = Quaternion.Euler(0f, _cinemachineTargetYaw, 0f);
		}

		// ─────────────────────────────────────────────
		// AGACHAR
		// ─────────────────────────────────────────────
		private void Crouch()
		{
			_wantsToCrouch = _input.crouch;

			if (_wantsToCrouch && !_isCrouching)
			{
				// Agachar
				_isCrouching = true;
				_controller.height = CrouchHeight;
				_controller.center = new Vector3(0, CrouchHeight / 2f, 0);
			}
			else if (!_wantsToCrouch && _isCrouching)
			{
				// Verifica se tem teto antes de levantar
				if (!CeilingAbove())
				{
					_isCrouching = false;
					_controller.height = StandHeight;
					_controller.center = new Vector3(0, StandHeight / 2f, 0);
				}
			}
		}

		/// <summary>
		/// Verifica se há algo acima do player impedindo de levantar.
		/// </summary>
		private bool CeilingAbove()
		{
			Vector3 top = transform.position + Vector3.up * (StandHeight - CeilingCheckRadius);
			return Physics.CheckSphere(top, CeilingCheckRadius, CeilingLayers, QueryTriggerInteraction.Ignore);
		}

		/// <summary>
		/// Suaviza a transição da câmera ao agachar/levantar (chamado no LateUpdate).
		/// </summary>
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

		// ─────────────────────────────────────────────
		// MOVIMENTO
		// ─────────────────────────────────────────────
		private void Move()
		{
			// Velocidade alvo: agachado < normal < corrida
			float targetSpeed;
			if (_isCrouching)
				targetSpeed = CrouchSpeed;
			else
				targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// Sem input → parar
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			float currentHorizontalSpeed = new Vector3(
				_controller.velocity.x, 0.0f, _controller.velocity.z
			).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// Acelera ou desacelera até a velocidade alvo
			if (currentHorizontalSpeed < targetSpeed - speedOffset ||
			    currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
				                    Time.deltaTime * SpeedChangeRate);
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
			if (_animationBlend < 0.01f) _animationBlend = 0f;

			// Direção do input normalizada
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			if (_input.move != Vector2.zero)
			{
				_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z)
				                  * Mathf.Rad2Deg
				                  + _mainCamera.transform.eulerAngles.y;
			}

			Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

			_controller.Move(
				targetDirection.normalized * (_speed * Time.deltaTime) +
				new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
			);

			if (_hasAnimator)
			{
				_animator.SetFloat(_animIDSpeed, _animationBlend);
				_animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
			}
		}

		// ─────────────────────────────────────────────
		// PULO E GRAVIDADE
		// ─────────────────────────────────────────────
		private void JumpAndGravity()
		{
			if (Grounded)
			{
				_fallTimeoutDelta = FallTimeout;

				if (_hasAnimator)
				{
					_animator.SetBool(_animIDJump, false);
					_animator.SetBool(_animIDFreeFall, false);
				}

				if (_verticalVelocity < 0.0f)
					_verticalVelocity = -2f;

				// Pulo (não pode pular agachado)
				if (_input.jump && _jumpTimeoutDelta <= 0.0f && !_isCrouching)
				{
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

					if (_hasAnimator)
						_animator.SetBool(_animIDJump, true);
				}

				if (_jumpTimeoutDelta >= 0.0f)
					_jumpTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				_jumpTimeoutDelta = JumpTimeout;

				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= Time.deltaTime;
				else if (_hasAnimator)
					_animator.SetBool(_animIDFreeFall, true);

				_input.jump = false;
			}

			if (_verticalVelocity < _terminalVelocity)
				_verticalVelocity += Gravity * Time.deltaTime;
		}

		// ─────────────────────────────────────────────
		// UTILIDADES
		// ─────────────────────────────────────────────
		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle >  360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		// ─────────────────────────────────────────────
		// GIZMOS (debug visual no editor)
		// ─────────────────────────────────────────────
		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed   = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			Gizmos.color = Grounded ? transparentGreen : transparentRed;

			// Chão
			Gizmos.DrawSphere(
				new Vector3(transform.position.x,
				            transform.position.y - GroundedOffset,
				            transform.position.z),
				GroundedRadius
			);

			// Teto
			Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 0.35f);
			Gizmos.DrawSphere(
				transform.position + Vector3.up * (StandHeight - CeilingCheckRadius),
				CeilingCheckRadius
			);
		}

		// ─────────────────────────────────────────────
		// AUDIO (chamados por Animation Events)
		// ─────────────────────────────────────────────
		private void OnFootstep(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight > 0.5f)
			{
				if (FootstepAudioClips.Length > 0)
				{
					int index = Random.Range(0, FootstepAudioClips.Length);
					AudioSource.PlayClipAtPoint(
						FootstepAudioClips[index],
						transform.TransformPoint(_controller.center),
						FootstepAudioVolume
					);
				}
			}
		}

		private void OnLand(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight > 0.5f)
			{
				AudioSource.PlayClipAtPoint(
					LandingAudioClip,
					transform.TransformPoint(_controller.center),
					FootstepAudioVolume
				);
			}
		}
	}
}