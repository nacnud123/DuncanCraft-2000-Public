using DuncanCraft.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DuncanCraft.PlayerScript
{
    public class Player
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Camera Camera { get; private set; }

        // Player physics
        private const float GRAVITY = -24.0f;
        private const float JUMP_VELOCITY = 10.0f;
        private const float WALK_SPEED = 6.0f;
        private const float SPRINT_SPEED = 10.0f;
        private const float GROUND_FRICTION = 8.0f;
        private const float AIR_RESISTANCE = 2.0f;

        // Player dimensions
        private const float PLAYER_WIDTH = 0.6f;
        private const float PLAYER_HEIGHT = 1.8f;
        private const float PLAYER_DEPTH = 0.6f;

        // Player state
        public bool IsOnGround { get; private set; }
        public bool IsSprinting { get; private set; }

        private readonly GameWorld _mWorld;

        public Player(Vector3 startPosition, GameWorld world)
        {
            Position = startPosition;
            Velocity = Vector3.Zero;
            _mWorld = world;
            Camera = new Camera(Position + new Vector3(0, PLAYER_HEIGHT - 0.2f, 0)); // Camera at eye level
            IsOnGround = false;
            IsSprinting = false;
        }

        public void Update(float deltaTime, KeyboardState keyboardState)
        {
            Vector3 inputDirection = getMovementInput(keyboardState);

            IsSprinting = keyboardState.IsKeyDown(Keys.LeftShift) && inputDirection.LengthSquared > 0;

            applyMovement(inputDirection, deltaTime);

            Gravity(deltaTime);

            if (keyboardState.IsKeyPressed(Keys.Space) && IsOnGround)
                Jump();

            if (keyboardState.IsKeyPressed(Keys.R))
                resetPosition();

            Physics(deltaTime);

            checkGroundState();

            updateCamera();
        }

        private Vector3 getMovementInput(KeyboardState keyboardState)
        {
            var input = Vector3.Zero;

            if (keyboardState.IsKeyDown(Keys.W)) 
                input += Vector3.UnitZ;

            if (keyboardState.IsKeyDown(Keys.S)) 
                input -= Vector3.UnitZ;

            if (keyboardState.IsKeyDown(Keys.A)) 
                input -= Vector3.UnitX;

            if (keyboardState.IsKeyDown(Keys.D)) 
                input += Vector3.UnitX;

            return input.LengthSquared > 0 ? input.Normalized() : input;
        }

        private void applyMovement(Vector3 inputDirection, float deltaTime)
        {
            var currentVelocity = Velocity;

            if (inputDirection.LengthSquared > 0)
            {
                var worldDirection = getWorldSpaceMovment(inputDirection);

                var speed = IsSprinting ? SPRINT_SPEED : WALK_SPEED;

                var targetVelocity = worldDirection * speed;

                var acceleration = IsOnGround ? GROUND_FRICTION : AIR_RESISTANCE;

                currentVelocity.X = MathHelper.Lerp(currentVelocity.X, targetVelocity.X, acceleration * deltaTime);
                currentVelocity.Z = MathHelper.Lerp(currentVelocity.Z, targetVelocity.Z, acceleration * deltaTime);
            }
            else
            {
                currentVelocity.X = 0;
                currentVelocity.Z = 0;
            }

            Velocity = currentVelocity;
        }

        private Vector3 getWorldSpaceMovment(Vector3 inputDirection)
        {
            var forward = Camera.Front;
            var right = Vector3.Cross(forward, Vector3.UnitY).Normalized();

            forward.Y = 0;
            forward = forward.Normalized();

            return (forward * inputDirection.Z + right * inputDirection.X).Normalized();
        }

        private void Gravity(float deltaTime)
        {
            if (IsOnGround)
                return;

            var currentVelocity = Velocity;

            currentVelocity.Y += GRAVITY * deltaTime;
            currentVelocity.Y *= 1.0f - 0.5f * deltaTime;
            currentVelocity.Y = Math.Max(currentVelocity.Y, -20.0f);

            Velocity = currentVelocity;
        }

        private void Jump()
        {
            var currentVelocity = Velocity;

            currentVelocity.Y = JUMP_VELOCITY;

            Velocity = currentVelocity;
            IsOnGround = false;
        }

        private void Physics(float deltaTime)
        {
            Vector3 oldPosition = Position;
            Vector3 newPosition = Position + Velocity * deltaTime;

            newPosition = Collision(oldPosition, newPosition);

            Position = newPosition;
        }

        private Vector3 Collision(Vector3 oldPos, Vector3 newPos)
        {
            IsOnGround = false;
            var resolvedPos = oldPos;

            resolvedPos = axisCollision(resolvedPos, newPos, oldPos, 0); // X-axis
            resolvedPos = axisCollision(resolvedPos, newPos, oldPos, 2); // Z-axis
            resolvedPos = verticalCollision(resolvedPos, newPos);        // Y-axis

            additionalGroundState(resolvedPos);

            return resolvedPos;
        }

        private Vector3 axisCollision(Vector3 resolvedPos, Vector3 newPos, Vector3 oldPos, int axis)
        {
            var testPos = resolvedPos;
            testPos[axis] = newPos[axis];

            if (!isPositionColliding(testPos))
            {
                resolvedPos[axis] = newPos[axis];
            }
            else
            {
                var currentVelocity = Velocity;
                currentVelocity[axis] = 0;
                Velocity = currentVelocity;
            }

            return resolvedPos;
        }

        private Vector3 verticalCollision(Vector3 resolvedPos, Vector3 newPos)
        {
            var testPos = new Vector3(resolvedPos.X, newPos.Y, resolvedPos.Z);

            if (!isPositionColliding(testPos))
            {
                resolvedPos.Y = newPos.Y;
            }
            else
            {
                if (Velocity.Y < 0) 
                    IsOnGround = true;

                var currentVelocity = Velocity;
                currentVelocity.Y = 0;
                Velocity = currentVelocity;
            }

            return resolvedPos;
        }

        private void additionalGroundState(Vector3 resolvedPos)
        {
            if (!IsOnGround && Velocity.Y >= -0.1f)
            {
                var groundTestPos = resolvedPos - new Vector3(0, 0.02f, 0);
                if (isPositionColliding(groundTestPos))
                {
                    IsOnGround = true;
                }
            }
        }

        private bool isPositionColliding(Vector3 position)
        {
            Vector3 min = position - new Vector3(PLAYER_WIDTH / 2, 0, PLAYER_DEPTH / 2);
            Vector3 max = position + new Vector3(PLAYER_WIDTH / 2, PLAYER_HEIGHT, PLAYER_DEPTH / 2);

            int minX = (int)Math.Floor(min.X);
            int maxX = (int)Math.Ceiling(max.X);
            int minY = (int)Math.Floor(min.Y);
            int maxY = (int)Math.Ceiling(max.Y);
            int minZ = (int)Math.Floor(min.Z);
            int maxZ = (int)Math.Ceiling(max.Z);

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    for (int z = minZ; z < maxZ; z++)
                    {
                        if (_mWorld.IsBlockSolid(x, y, z))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void checkGroundState()
        {
            if (Velocity.Y > 0.1f) 
                return;

            var groundTestPos = Position - new Vector3(0, 0.01f, 0);
            var groundBelow = isPositionColliding(groundTestPos);

            if (groundBelow && Velocity.Y <= 0.1f)
            {
                IsOnGround = true;
            }
        }

        private void updateCamera()
        {
            Vector3 eyePosition = Position + new Vector3(0, PLAYER_HEIGHT - 0.2f, 0);
            Camera.Position = eyePosition;
        }

        private void resetPosition()
        {
            Position = new Vector3(128, 50, 128);
            Velocity = Vector3.Zero;
            IsOnGround = false;
        }

        public void ProcessMouseMovement(Vector2 mouseDelta)
        {
            Camera.ProcessMouseMovement(mouseDelta);
        }
    }
}