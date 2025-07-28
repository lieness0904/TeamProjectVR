namespace Fusion
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using UnityEngine;

    [StructLayout(LayoutKind.Explicit)]
    [NetworkStructWeaved(WORDS + 4)]
    public unsafe struct NetworkCCData : INetworkStruct
    {
        public const int WORDS = NetworkTRSPData.WORDS + 4;
        public const int SIZE = WORDS * 4;

        [FieldOffset(0)]
        public NetworkTRSPData TRSPData;

        [FieldOffset((NetworkTRSPData.WORDS + 0) * Allocator.REPLICATE_WORD_SIZE)]
        int _grounded;

        [FieldOffset((NetworkTRSPData.WORDS + 1) * Allocator.REPLICATE_WORD_SIZE)]
        Vector3Compressed _velocityData;

        public bool Grounded
        {
            get => _grounded == 1;
            set => _grounded = (value ? 1 : 0);
        }

        public Vector3 Velocity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _velocityData;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _velocityData = value;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [NetworkBehaviourWeaved(NetworkCCData.WORDS)]
    // ReSharper disable once CheckNamespace
    public sealed unsafe class NetworkCharacterController : NetworkTRSP, INetworkTRSPTeleport, IBeforeAllTicks, IAfterAllTicks, IBeforeCopyPreviousState
    {
        new ref NetworkCCData Data => ref ReinterpretState<NetworkCCData>();

        [Header("Character Controller Settings")]
        public float gravity = -20.0f;
        public float jumpImpulse = 8.0f;
        public float acceleration = 10.0f;
        public float braking = 10.0f;
        public float maxSpeed = 2.0f;
        public float rotationSpeed = 15.0f;

        Tick _initial;
        CharacterController _controller;

        public Vector3 Velocity
        {
            get => Data.Velocity;
            set => Data.Velocity = value;
        }

        public bool Grounded
        {
            get => Data.Grounded;
            set => Data.Grounded = value;
        }

        public void Teleport(Vector3? position = null, Quaternion? rotation = null)
        {
            _controller.enabled = false;
            NetworkTRSP.Teleport(this, transform, position, rotation);
            _controller.enabled = true;
        }


        public void Jump(bool ignoreGrounded = false, float? overrideImpulse = null)
        {
            if (Data.Grounded || ignoreGrounded)
            {
                var newVel = Data.Velocity;
                newVel.y += overrideImpulse ?? jumpImpulse;
                Data.Velocity = newVel;
            }
        }

        public void Move(Vector3 direction)
        {
            var deltaTime = Runner.DeltaTime;
            var previousPos = transform.position;
            var moveVelocity = Data.Velocity;

            // --- [수정된 부분] ---
            // 이동하기 전, 현재 바닥에 붙어 있었는지 상태를 저장합니다.
            bool wasGrounded = Grounded;

            direction = direction.normalized;

            if (wasGrounded && moveVelocity.y < 0)
            {
                moveVelocity.y = 0f;
            }

            moveVelocity.y += gravity * Runner.DeltaTime;

            var horizontalVel = default(Vector3);
            horizontalVel.x = moveVelocity.x;
            horizontalVel.z = moveVelocity.z;

            if (direction == default)
            {
                horizontalVel = Vector3.Lerp(horizontalVel, default, braking * deltaTime);
            }
            else
            {
                horizontalVel = Vector3.ClampMagnitude(horizontalVel + direction * acceleration * deltaTime, maxSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Runner.DeltaTime);
            }

            moveVelocity.x = horizontalVel.x;
            moveVelocity.z = horizontalVel.z;

            _controller.Move(moveVelocity * deltaTime);

            // 실제 이동 결과를 바탕으로 새로운 속도를 계산합니다.
            Vector3 newVelocity = (transform.position - previousPos) * Runner.TickRate;

            // 만약 이동 전에 바닥에 있었는데, 이동 후 수직 속도가 양수(위로 솟구침)가 되었다면
            // 이를 강제로 0으로 만들어 튕김 현상을 억제합니다.
            if (wasGrounded && newVelocity.y > 0)
            {
                newVelocity.y = 0;
            }

            // 최종적으로 계산된 값을 네트워크 데이터에 저장합니다.
            Data.Velocity = newVelocity;
            Data.Grounded = _controller.isGrounded;
        }

        public override void Spawned()
        {
            _initial = default;
            TryGetComponent(out _controller);
            _controller.enabled = false;
            _controller.enabled = true;
            CopyToBuffer();
        }

        public override void Render()
        {
            NetworkTRSP.Render(this, transform, false, false, false, ref _initial);
        }

        void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
        {
            CopyToEngine();
        }

        void IAfterAllTicks.AfterAllTicks(bool resimulation, int tickCount)
        {
            CopyToBuffer();
        }

        void IBeforeCopyPreviousState.BeforeCopyPreviousState()
        {
            CopyToBuffer();
        }

        void Awake()
        {
            TryGetComponent(out _controller);
        }

        void CopyToBuffer()
        {
            Data.TRSPData.Position = transform.position;
            Data.TRSPData.Rotation = transform.rotation;
        }

        void CopyToEngine()
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(Data.TRSPData.Position, Data.TRSPData.Rotation);
            _controller.enabled = true;
        }
    }
}