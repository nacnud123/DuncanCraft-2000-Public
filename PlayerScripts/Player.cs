using OpenTK.Mathematics;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.PlayerScripts
{
    public class Player
    {
        public const float MOVE_SPEED = 5f;
        public const float SPRINT_SPEED = 20f;

        public const float GRAVITY = 9.8f;
        public const float JUMP_FORCE = 5f;

        public Camera Camera;
        public InputManager inputManager;
        public ChunkManager cManager;
        public TerrainModifier terrainModifier;

        private bool isOnGround;
        private Vector3 velocity;
        private Vector3 position;

        private Vector3 size = new Vector3(.6f, 1.8f, .6f);
        private Vector3 offset = new Vector3(0f, 0f, 0f);

        private Vector3 spawnPosition = new Vector3(5, 130, 5);

        public Vector3 Velocity { get => velocity; set => velocity = value; }
        public Vector3 Position { get => position; set => position = value; }

        public Player(ChunkManager _world, Vector2i size)
        {
            Camera = new Camera(spawnPosition, (float)size.X / size.Y);
            position = spawnPosition;
            cManager = _world;
            inputManager = new InputManager(this);
            terrainModifier = new TerrainModifier(cManager);
        }

        public void Update(float deltaTime)
        {
            isOnGround = checkGroundCollision();

            if (!isOnGround)
            {
                velocity.Y -= GRAVITY * deltaTime;
            }
            else if (velocity.Y < 0)
            {
                velocity.Y = 0;
            }

            if (position.Y < -10)
            {
                position = spawnPosition;
                velocity = Vector3.Zero;
                return;
            }

            Vector3 newPosition = position + velocity * deltaTime;

            moveWithCollision(newPosition);

            Camera.Position = position + new Vector3(0.0f, size.Y * 0.5f, 0.0f);

            VoxelGame.init.currentChunkPosition.X = (int)Math.Floor(newPosition.X / (float)Constants.CHUNK_SIZE);
            VoxelGame.init.currentChunkPosition.Y = (int)Math.Floor(newPosition.Z / (float)Constants.CHUNK_SIZE);

            velocity.X = 0.0f;
            velocity.Z = 0.0f;
        }

        private void moveWithCollision(Vector3 targetPosition)
        {
            Vector3 movement = targetPosition - position;

            // Move X axis with collision
            Vector3 testPos = position + new Vector3(movement.X, 0, 0);
            if (!checkCollision(testPos))
            {
                position.X = testPos.X;
            }
            else
            {
                velocity.X = 0;
            }

            // Move Y axis with collision
            testPos = position + new Vector3(0, movement.Y, 0);
            if (!checkCollision(testPos))
            {
                position.Y = testPos.Y;
            }
            else
            {
                velocity.Y = 0;
            }

            // Move Z axis with collision
            testPos = position + new Vector3(0, 0, movement.Z);
            if (!checkCollision(testPos))
            {
                position.Z = testPos.Z;
            }
            else
            {
                velocity.Z = 0;
            }
        }

        private bool checkGroundCollision()
        {
            Vector3 groundCheckPos = position + new Vector3(0, -0.1f, 0);
            return checkCollision(groundCheckPos);
        }

        private bool checkCollision(Vector3 pos)
        {
            Vector3 halfSize = size * 0.5f;

            Vector3 minWorld = pos - halfSize + offset;
            Vector3 maxWorld = pos + halfSize + offset;

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
                        if (isBlockSolid(x, y, z))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool isBlockSolid(int worldX, int worldY, int worldZ)
        {
            if (worldY < 0 || worldY >= Constants.CHUNK_HEIGHT)
            {
                return worldY < 0;
            }

            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(worldX / (float)Constants.CHUNK_SIZE),
                (int)Math.Floor(worldZ / (float)Constants.CHUNK_SIZE)
            );

            Chunk chunk = cManager.GetChunk(chunkPos);
            if (chunk == null)
            {
                return false;
            }

            Vector3i localPos = new Vector3i(
                worldX - chunkPos.X * Constants.CHUNK_SIZE,
                worldY,
                worldZ - chunkPos.Z * Constants.CHUNK_SIZE
            );

            if (chunk.IsInBounds(localPos))
            {
                return chunk.GetBlock(localPos) != BlockIDs.Air && chunk.GetBlock(localPos) != BlockIDs.YellowFlower;
            }

            return false;
        }

        public void Jump()
        {
            if (isOnGround)
            {
                velocity.Y = JUMP_FORCE;
            }
        }

        public void ResetPosition()
        {
            position = spawnPosition;
        }
    }
}