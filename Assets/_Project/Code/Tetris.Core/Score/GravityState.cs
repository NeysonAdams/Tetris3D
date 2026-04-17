namespace Tetris.Core.States
{
    public struct GravityState
    {
        public float AccumulateTime;
        public float CurrentInterval;
        public float LockDelayTimer;
        public bool IsSoftDropping;

        public GravityState(float initialInterval)
        {
            AccumulateTime = 0;
            CurrentInterval = initialInterval;
            LockDelayTimer = 0;
            IsSoftDropping = false;
        }
    }
}
