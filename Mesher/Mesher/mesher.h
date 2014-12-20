#include <stdint.h>
#include <cstring>

//Chunk Size
#define CS 16

struct vector3
{
	float m[3];
};

struct vector2
{
	float m[2];
};

int vtab(int x, int y, int z)
{
	return x + CS*y + CS*CS*z;
}

int vtab_safe(int x, int y, int z, int shf = 0)
{
	if (x < 0)return 0;
	if (y < 0)return 0;
	if (z < 0)return 0;

	if (x >= CS)return 0;
	if (y >= CS)return 0;
	if (z >= CS)return 0;

	if (shf == 1)
	{
		return vtab(y, z, x);
	}
	else if (shf == 2)
	{
		return vtab(z, x, y);
	}
	return vtab(x, y, z);
}

int vtab_safe(int* crd, int shf = 0)
{
	return vtab_safe(crd[0], crd[1], crd[2], shf);
}

bool is_voxel_visible(int* voxels, int x, int y, int z)
{
	if (voxels[vtab_safe(x - 1, y, z)] == 0)return true;
	if (voxels[vtab_safe(x + 1, y, z)] == 0)return true;

	if (voxels[vtab_safe(x, y - 1, z)] == 0)return true;
	if (voxels[vtab_safe(x, y + 1, z)] == 0)return true;

	if (voxels[vtab_safe(x, y, z - 1)] == 0)return true;
	if (voxels[vtab_safe(x, y, z + 1)] == 0)return true;
	return false;
}

void uvd2crd(int u, int v, int d, int dim, int* crd)
{
	crd[dim] = d;
	crd[(dim + 1) % 3] = u;
	crd[(dim + 2) % 3] = v;
}