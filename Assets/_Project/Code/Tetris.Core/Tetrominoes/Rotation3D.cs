using System;

namespace Tetris.Core.Tetrominoes
{
    public readonly record struct Rotation3D(int RxSteps, int RySteps, int RzSteps)
    {
        public static Rotation3D Identity => default;

        public Rotation3D WithStep(RotationAxis axis, int direction)
        {
            return axis switch
            {
                RotationAxis.X => this with { RxSteps = Normalize(RxSteps + direction) },
                RotationAxis.Y => this with { RySteps = Normalize(RySteps + direction) },
                RotationAxis.Z => this with { RzSteps = Normalize(RzSteps + direction) },
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unknown rotation axis")
            };
        }

        private static int Normalize(int step) {
            var mod = step % 4;
            return mod < 0 ? mod + 4 : mod;
        }
    }
}