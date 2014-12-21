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
	if (x < 0)return CS*CS*CS;
	if (y < 0)return CS*CS*CS;
	if (z < 0)return CS*CS*CS;

	if (x >= CS)return CS*CS*CS;
	if (y >= CS)return CS*CS*CS;
	if (z >= CS)return CS*CS*CS;

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

void uvd_conv(int u, int v, int d, int dim, int side, int* crd, int &index, int &check)
{
	uvd2crd(u, v, d, dim, crd);
	index = vtab(crd[0], crd[1], crd[2]);
	crd[dim] += side;
	check = vtab_safe(crd);
	crd[dim] -= side;
}

bool work_check(uint8_t* work, int *voxels, int index, int check, int setbit)
{
	//return ((work[index] & 1) == 1) && (voxels[check]==0) && ((work[index] & setbit) != setbit);
	return (voxels[index] != 0) && (voxels[check] == 0) && ((work[index] & setbit) != setbit);
}

int max(int a, int b)
{
	if (a > b) return a;
	return b;
}

bool uv_rect_check(uint8_t *work, int *voxels, int dim, int side, int depth, int setbit, int beginU, int beginV, int endU, int endV)
{
	for (int u = beginU; u <= endU; u++)
	{
		for (int v = beginV; v <= endV; v++)
		{
			int crd[3] = { 0, 0, 0 };
			int index, check;
			uvd_conv(u, v, depth, dim, side, crd, index, check);

			if (!work_check(work, voxels, index, check, setbit))
			{
				return false;
			}
		}
	}
	return true;
}

bool get_next_uv_rect(uint8_t *work, int *voxels, int dim, int side, int depth, int setbit, int &beginU, int &beginV, int &endU, int &endV)
{
	beginU = -1;
	beginV = -1;
	endU = -1;
	endV = -1;
	bool expandV = true;
	bool expandU = true;

	int u, v;

	for (u = 0; u < CS; u++)
	{
		for (v = 0; v < CS; v++)
		{
			int crd[3] = { 0, 0, 0 };
			int index, check;
			uvd_conv(u, v, depth, dim, side, crd, index, check);

			if (work_check(work, voxels, index, check, setbit))
			{
				beginU = u;
				beginV = v;
				endV = v;
				endU = u;
				goto loopexit;
			}
		}
	}
loopexit:	

	if (beginU != -1)
	{
		for (u = beginU; u < CS; u++)
		{
			int oldU = endU;
			endU = u;
			if (!uv_rect_check(work, voxels, dim, side, depth, setbit, beginU, beginV, endU, endV))
			{
				endU = oldU;
				break;
			}
		}

		for (v = beginV; v < CS; v++)
		{
			int oldV = endV;
			endV = v;
			if (!uv_rect_check(work, voxels, dim, side, depth, setbit, beginU, beginV, endU, endV))
			{
				endV = oldV;
				break;
			}
		}

		for (int u = beginU; u <= endU; u++)
		{
			for (int v = beginV; v <= endV; v++)
			{
				int crd[3] = { 0, 0, 0 };
				int index, check;
				uvd_conv(u, v, depth, dim, side, crd, index, check);

				work[index] |= setbit;
			}
		}
		return true;
	}

	return false;
}

void spawn_face(vector3* vertices, vector3* normals, vector2* uvs, int* tris, 
	int &curVert, int &curInd, int beginU, int beginV, int endU, int endV, int dim, int depth, int side)
{
	int dimu = (dim + 1) % 3;
	int dimv = (dim + 2) % 3;
	int so = (side > 0) ? 1 : 0; //side offset

	int rect[4] = { beginU, beginV, endU + 1, endV + 1 };
	for (int r = 0; r < 4; r++)
	{
		vertices[curVert + r].m[dim] = depth + so;
		vertices[curVert + r].m[dimu] = rect[(r / 2) * 2];
		vertices[curVert + r].m[dimv] = rect[(r % 2) * 2 + 1];
		normals[curVert + r].m[dim] = side;
		normals[curVert + r].m[dimu] = 0;
		normals[curVert + r].m[dimv] = 0;
	}

	if (side < 0)
	{
		tris[curInd + 0] = curVert + 0;
		tris[curInd + 1] = curVert + 1;
		tris[curInd + 2] = curVert + 2;

		tris[curInd + 3] = curVert + 3;
		tris[curInd + 4] = curVert + 2;
		tris[curInd + 5] = curVert + 1;
	}
	else
	{
		tris[curInd + 0] = curVert + 2;
		tris[curInd + 1] = curVert + 1;
		tris[curInd + 2] = curVert + 0;

		tris[curInd + 3] = curVert + 1;
		tris[curInd + 4] = curVert + 2;
		tris[curInd + 5] = curVert + 3;
	}
	curVert += 4;
	curInd += 6;
}