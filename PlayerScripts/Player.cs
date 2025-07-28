using OpenTK.Mathematics;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.PlayerScripts
{
    public class Player
    {
        #region Variables
        #region Movement constants
        public const float MOVE_SPEED = 5f;
        public const float SPRINT_SPEED = 20f;
        public const float GRAVITY = 9.8f;
        public const float JUMP_FORCE = 5f;
        public const float MAX_STEP_HEIGHT = 0.6f;
        #endregion

        #region  Components
        public Camera _Camera;
        public InputManager inputManager;
        public ChunkManager chunkManager;
        public TerrainModifier terrainModifier;
        #endregion

        #region  Physics state
        private bool mIsOnGround;
        private Vector3 mVelocity;
        private Vector3 mPosition;
        #endregion

        #region  Player properties
        private readonly Vector3 mSize = new Vector3(.6f, 1.8f, .6f);
        private readonly Vector3 mSpawnPosition = new Vector3(5, 130, 5);
        #endregion

        #region Public properties
        public Vector3 Velocity { get => mVelocity; set => mVelocity = value; }
        public Vector3 Position { get => mPosition; set => mPosition = value; }
        #endregion
        #endregion

        public Player(ChunkManager world, Vector2i screenSize)
        {
            mPosition = mSpawnPosition;
            chunkManager = world;
            _Camera = new Camera(mSpawnPosition, (float)screenSize.X / screenSize.Y);
            inputManager = new InputManager(this);
            terrainModifier = new TerrainModifier(chunkManager);
        }

        public void Update(float deltaTime)
        {
            updatePhysics(deltaTime);
            handleFallReset();

            Vector3 targetPosition = mPosition + mVelocity * deltaTime;
            moveWithCollision(targetPosition);

            updateCamera();
            updateChunkPosition();

            // Reset horizontal velocity
            mVelocity.X = mVelocity.Z = 0f;
        }

        private void updatePhysics(float deltaTime)
        {
            mIsOnGround = isOnGround();

            if (!mIsOnGround)
            {
                mVelocity.Y -= GRAVITY * deltaTime;
            }
            else if (mVelocity.Y < 0)
            {
                mVelocity.Y = 0;
            }
        }

        private void handleFallReset()
        {
            if (mPosition.Y < -10)
            {
                mPosition = mSpawnPosition;
                mVelocity = Vector3.Zero;
            }
        }

        private void moveWithCollision(Vector3 targetPosition)
        {
            Vector3 movement = targetPosition - mPosition;

            tryMoveAxis(ref mPosition.X, movement.X, Vector3.UnitX);
            tryMoveAxis(ref mPosition.Y, movement.Y, Vector3.UnitY);
            tryMoveAxis(ref mPosition.Z, movement.Z, Vector3.UnitZ);
        }

        private void tryMoveAxis(ref float currentPos, float movement, Vector3 axis)
        {
            if (Math.Abs(movement) < 0.001f) return;

            Vector3 testPos = mPosition + axis * movement;

            if (!hasCollision(testPos))
            {
                currentPos = testPos[(int)getAxisIndex(axis)];
                return;
            }

            // try stepping up
            if (axis.Y == 0 && mIsOnGround)
            {
                float stepHeight = getStepUpHeight(testPos);
                if (stepHeight > 0)
                {
                    currentPos = testPos[(int)getAxisIndex(axis)];
                    mPosition.Y += stepHeight;
                    return;
                }
            }

            if (axis.X != 0) mVelocity.X = 0;
            if (axis.Y != 0) mVelocity.Y = 0;
            if (axis.Z != 0) mVelocity.Z = 0;
        }

        private int getAxisIndex(Vector3 axis)
        {
            if (axis.X != 0) return 0;
            if (axis.Y != 0) return 1;
            return 2;
        }

        private float getStepUpHeight(Vector3 blockedPosition)
        {
            for (float stepHeight = 0.1f; stepHeight <= MAX_STEP_HEIGHT; stepHeight += 0.1f)
            {
                Vector3 stepPos = blockedPosition + Vector3.UnitY * stepHeight;
                if (!hasCollision(stepPos))
                    return stepHeight;
            }
            return 0;
        }

        private bool isOnGround()
        {
            Vector3 groundCheck = mPosition - Vector3.UnitY * 0.1f;
            return hasCollision(groundCheck);
        }

        private bool hasCollision(Vector3 pos)
        {
            Vector3 halfSize = mSize * 0.5f;
            Vector3 minWorld = pos - halfSize;
            Vector3 maxWorld = pos + halfSize;

            Vector3i minBlock = new Vector3i(
                (int)Math.Floor(minWorld.X),
                (int)Math.Floor(minWorld.Y),
                (int)Math.Floor(minWorld.Z)
            );

            Vector3i maxBlock = new Vector3i(
                (int)Math.Floor(maxWorld.X),
                (int)Math.Floor(maxWorld.Y),
                (int)Math.Floor(maxWorld.Z)
            );

            for (int x = minBlock.X; x <= maxBlock.X; x++)
            {
                for (int y = minBlock.Y; y <= maxBlock.Y; y++)
                {
                    for (int z = minBlock.Z; z <= maxBlock.Z; z++)
                    {
                        if (isBlockSolid(x, y, z, minWorld, maxWorld))
                            return true;
                    }
                }
            }
            return false;
        }

        private bool isBlockSolid(int worldX, int worldY, int worldZ, Vector3 playerMin, Vector3 playerMax)
        {
            // Check world bounds
            if (worldY < 0) return true;
            if (worldY >= Constants.CHUNK_HEIGHT) return false;

            // Get block from chunk
            byte blockType = getBlockAt(worldX, worldY, worldZ);
            if (blockType == BlockIDs.Air || blockType == BlockIDs.YellowFlower)
                return false;

            // Calculate block bounds
            Vector3 blockMin = new Vector3(worldX, worldY, worldZ);
            Vector3 blockMax = new Vector3(worldX + 1, worldY + getBlockHeight(blockType), worldZ + 1);

            // Check AABB collision
            return (playerMin.X < blockMax.X && playerMax.X > blockMin.X) &&
                   (playerMin.Y < blockMax.Y && playerMax.Y > blockMin.Y) &&
                   (playerMin.Z < blockMax.Z && playerMax.Z > blockMin.Z);
        }

        private byte getBlockAt(int worldX, int worldY, int worldZ)
        {
            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(worldX / (float)Constants.CHUNK_SIZE),
                (int)Math.Floor(worldZ / (float)Constants.CHUNK_SIZE)
            );

            Chunk chunk = chunkManager.GetChunk(chunkPos);
            if (chunk == null) return BlockIDs.Air;

            Vector3i localPos = new Vector3i(
                worldX - chunkPos.X * Constants.CHUNK_SIZE,
                worldY,
                worldZ - chunkPos.Z * Constants.CHUNK_SIZE
            );

            return chunk.GetBlock(localPos);
        }

        private float getBlockHeight(byte blockType)
        {
            return blockType == BlockIDs.Slab ? 0.5f : 1.0f;
        }

        private void updateCamera()
        {
            _Camera.Position = mPosition + Vector3.UnitY * (mSize.Y * 0.5f);
        }

        private void updateChunkPosition()
        {
            VoxelGame.init.currentChunkPosition.X = (int)Math.Floor(mPosition.X / Constants.CHUNK_SIZE);
            VoxelGame.init.currentChunkPosition.Y = (int)Math.Floor(mPosition.Z / Constants.CHUNK_SIZE);
        }

        public void Jump()
        {
            if (mIsOnGround)
                mVelocity.Y = JUMP_FORCE;
        }

        public void ResetPosition()
        {
            mPosition = mSpawnPosition;
        }
    }
}