using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IGenerable
{
    bool ConditionMet(int x, int y, int z, LibNoise.Unity.ModuleBase noise);

    void Generate(PlaceVoxelDelegate placeVoxel, int x, int y, int z);
}

