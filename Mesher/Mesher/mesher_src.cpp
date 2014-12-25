#include "mesher.h"
#ifdef _BENCHMARK
#include <iostream>
#include <ctime>
#endif

int vtab(int x, int y, int z)
{
	return x + CS*y + CS*CS*z;
}

int vtab_safe(int x, int y, int z, int shf)
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

int vtab_safe(int* crd, int shf)
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

float max(float a, float b)
{
	if (a > b) return a;
	return b;
}

float min(float a, float b)
{
	if (a < b) return a;
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

//bool get_next_uv_rect(uint8_t *work, int *voxels, int dim, int side, int depth, int setbit, int &beginU, int &beginV, int &endU, int &endV)
//{
//	beginU = -1;
//	beginV = -1;
//	endU = -1;
//	endV = -1;
//	bool expandV = true;
//	bool expandU = true;
//
//	int u, v;
//
//	for (u = 0; u < CS; u++)
//	{
//		for (v = 0; v < CS; v++)
//		{
//			int crd[3] = { 0, 0, 0 };
//			int index, check;
//			uvd_conv(u, v, depth, dim, side, crd, index, check);
//
//			if (work_check(work, voxels, index, check, setbit))
//			{
//				beginU = u;
//				beginV = v;
//				endV = v;
//				endU = u;
//				goto loopexit;
//			}
//		}
//	}
//loopexit:
//
//	if (beginU != -1)
//	{
//		for (u = beginU; u < CS; u++)
//		{
//			int oldU = endU;
//			endU = u;
//			if (!uv_rect_check(work, voxels, dim, side, depth, setbit, beginU, beginV, endU, endV))
//			{
//				endU = oldU;
//				break;
//			}
//		}
//
//		for (v = beginV; v < CS; v++)
//		{
//			int oldV = endV;
//			endV = v;
//			if (!uv_rect_check(work, voxels, dim, side, depth, setbit, beginU, beginV, endU, endV))
//			{
//				endV = oldV;
//				break;
//			}
//		}
//
//		for (int u = beginU; u <= endU; u++)
//		{
//			for (int v = beginV; v <= endV; v++)
//			{
//				int crd[3] = { 0, 0, 0 };
//				int index, check;
//				uvd_conv(u, v, depth, dim, side, crd, index, check);
//
//				work[index] |= setbit;
//			}
//		}
//		return true;
//	}
//
//	return false;
//}

bool get_next_uv_rect(uint8_t *work, int *voxels, int dim, int side, int depth, int setbit, int &beginU, int &beginV, int &endU, int &endV)
{
	beginU = -1;
	beginV = -1;
	endU = 15;
	endV = 15;
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
				if (beginU == -1)
				{
					beginU = u;
					beginV = v;
				}
			}
			else
			{
				if (beginU != -1)
				{
					if (endV == 15 && u == beginU && v > beginV)
					{
						endV = v - 1;
					}
					if (v <= endV && u > beginU)
					{
						endU = u - 1;
						goto loopexit;
					}
				}
			}
		}
	}
loopexit:

	if (beginU != -1)
	{			
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
	int &curVert, int &curInd, int beginU, int beginV, int endU, int endV, int dim, int depth, int side, bool liquid)
{
	if (liquid)
	{
		if (dim != 1 || side < 0) return;
	}

	int dimu = (dim + 1) % 3;
	int dimv = (dim + 2) % 3;
	int so = (side > 0) ? 1 : 0; //side offset

	int rect[4] = { beginU, beginV, endU + 1, endV + 1 };
	for (int r = 0; r < 4; r++)
	{
		vertices[curVert + r].m[dim] = depth + so - (liquid ? 0.2 : 0);
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

void dump_types(int* voxels, vector3* vptr, vector3* nptr, int *tempPatch, int &w, int &h)
{
	int dim = 0;
	if (abs(nptr->m[1]) > 0.1) dim = 1;
	if (abs(nptr->m[2]) > 0.1) dim = 2;
	int side = nptr->m[dim] < 0 ? -1 : 1;
	int dimu = (dim + 1) % 3;
	int dimv = (dim + 2) % 3;
	int d = round(vptr->m[dim] - (side > 0 ? 1 : 0));

	float umin = vptr->m[dimu];
	float umax = vptr->m[dimu];
	float vmin = vptr->m[dimv];
	float vmax = vptr->m[dimv];

	for (int la = 0; la < 4; la++)
	{
		umin = min((vptr + la)->m[dimu], umin);
		umax = max((vptr + la)->m[dimu], umax);
		vmin = min((vptr + la)->m[dimv], vmin);
		vmax = max((vptr + la)->m[dimv], vmax);
	}

	int beginu = round(umin);
	int beginv = round(vmin);
	int endu = round(umax - 1);
	int endv = round(vmax - 1);

	w = endu - beginu + 1;
	h = endv - beginv + 1;

	bool result = true;
	int prevt = -1;
	for (int u = beginu; u <= endu; u++)
	{
		for (int v = beginv; v <= endv; v++)
		{
			int crd[3] = { 0, 0, 0 };
			uvd2crd(u, v, d, dim, crd);

			int x = u - beginu;
			int y = v - beginv;
			int t = voxels[vtab(crd[0], crd[1], crd[2])];
			if (prevt == -1)
			{
				prevt = t;
			}
			else
			{
				if (t != prevt)result = false;
				prevt = t;
			}
			tempPatch[x + y * w] = t;
		}
	}
	if (result)
	{
		w = 1;
		h = 1;
	}
}

void make_texture(int* voxels, vector3* vertices, vector3* normals, vector2* uvs, int* tris, int vcount, int* tex, int* texw, int* texh)
{
	const int tempSize = 256;

	int *tempTex = new int[tempSize*tempSize];
	int curh = 1;
	int curw = 1;
	uint8_t *reserved = new uint8_t[tempSize * tempSize];
	memset(reserved, 0, tempSize*tempSize);

	int tempPatch[CS*CS];
	int tempPatchW;
	int tempPatchH;
	int iter = 0;

	for (int lv = 0; lv < vcount; lv += 4)
	{
		dump_types(voxels, vertices + lv, normals + lv, tempPatch, tempPatchW, tempPatchH);
		while (curw < tempPatchW) curw <<= 1;
		while (curh < tempPatchH) curh <<= 1;
		bool match = true;

		int x = 0;
		int y = 0;
		do
		{
			match = false;
			for (int la = 0; la < curw - tempPatchW; la++)
			{
				for (int lb = 0; lb < curh - tempPatchH; lb++)
				{
					x = la;
					y = lb;
					match = true;
					for (int pa = 0; pa < tempPatchW; pa++)
					{
						for (int pb = 0; pb < tempPatchH; pb++)
						{
							int patchCol = tempPatch[pa + pb * tempPatchW];
							int texCol = tempTex[la + pa + (lb + pb) * tempSize];
							uint8_t resv = reserved[la + pa + (lb + pb) * tempSize];
							if (resv == 1 && patchCol != texCol)
							{
								match = false;
								goto patchexit;
							}
						}
					}
					if (match)goto texexit;
				patchexit:;
				}
			}
		texexit:
			if (!match)
			{
				/*if (iter % 2 == 0) curw <<= 1;
				else curh <<= 1;
				iter++;*/
				if (curw > curh) curh <<= 1;
				else curw <<= 1;
			}
		} while (!match);

		for (int pa = 0; pa < tempPatchW; pa++)
		{
			for (int pb = 0; pb < tempPatchH; pb++)
			{
				reserved[x + pa + (y + pb) * tempSize] = 1;
				tempTex[x + pa + (y + pb) * tempSize] = tempPatch[pa + pb * tempPatchW];
			}
		}

		/*uvs[lv + 0].m[0] = x;
		uvs[lv + 0].m[1] = y;

		uvs[lv + 2].m[0] = x + tempPatchW + 1;
		uvs[lv + 2].m[1] = y;

		uvs[lv + 1].m[0] = x;
		uvs[lv + 1].m[1] = y + tempPatchH + 1;

		uvs[lv + 3].m[0] = x + tempPatchW + 1;
		uvs[lv + 3].m[1] = y + tempPatchH + 1;*/

		int indbase = (lv / 4) * 6;
		for (int i = 0; i < 6; i++)
		{
			int v = tris[indbase + i];
			vector2* uv = uvs + v;
			v %= 4;
			if (v == 0)
			{
				uv->m[0] = x;
				uv->m[1] = y;
			}
			if (v == 1)
			{
				uv->m[0] = x;
				uv->m[1] = y + tempPatchH;
			}
			if (v == 2)
			{
				uv->m[0] = x + tempPatchW;
				uv->m[1] = y;
			}
			if (v == 3)
			{
				uv->m[0] = x + tempPatchW;
				uv->m[1] = y + tempPatchH;
			}
		}
	}

	*texw = curw;
	*texh = curh;

	for (int la = 0; la < curw; la++)
	{
		for (int lb = 0; lb < curh; lb++)
		{
			tex[la + lb * curw] = tempTex[la + lb * tempSize];
		}
	}

	for (int la = 0; la < vcount; la++)
	{
		uvs[la].m[0] = uvs[la].m[0] / (float)(curw);
		uvs[la].m[1] = uvs[la].m[1] / (float)(curh);
	}

	delete[] tempTex;
	delete[] reserved;
}



extern "C" __declspec(dllexport) void __stdcall MeshVoxels(
	int voxelCount, 
	uint64_t* coords,
	uint32_t* types,
	vector3* vertices,
	vector3* normals,
	vector2* uvs,
	int* tris,
	int* vertexCount,
	int* indexCount,
	int* texture,
	int* texW,
	int* texH,
	bool liquid
	)
{
#ifdef _BENCHMARK
	clock_t c1 = std::clock();
#endif

	int voxels[CS * CS * CS + 1];
	uint8_t work[CS * CS * CS + 1];
	
	memset(voxels, 0, (CS*CS*CS+1)*sizeof(int));
	memset(work, 0, (CS*CS*CS+1));

	for (int la = 0; la < voxelCount; la++)
	{
		int16_t* crd = (int16_t*)&coords[la];
		int index = vtab(crd[0], crd[1], crd[2]);
		voxels[index] = types[la];
	}
	for (int la = 0; la < voxelCount; la++)
	{
		int16_t* crd = (int16_t*)&coords[la];
		int index = vtab(crd[0], crd[1], crd[2]);
		work[index] |= (is_voxel_visible(voxels, crd[0], crd[1], crd[2]) ? 1 : 0);
	}

	int curVert = 0;
	int curInd = 0;
	int beginU, beginV, endU, endV;
	int setbit = 2;
	int expandPhase = 0; //0-expandV, 1-expandU, 2-finished

	for (int dim = 0; dim < 3; dim++)
	{
		int dimu = (dim + 1) % 3;
		int dimv = (dim + 2) % 3;
		for (int side = -1; side <= 1; side+=2)
		{			
			for (int depth = 0; depth < CS; depth++)
			{
				bool rectExist = false;
				int beginU, beginV, endU, endV;

				while ((rectExist = get_next_uv_rect(work, voxels, dim, side, depth, setbit, beginU, beginV, endU, endV)))
				{
					spawn_face(vertices, normals, uvs, tris, curVert, curInd, beginU, beginV, endU, endV, dim, depth, side, liquid);
				}
			}
			setbit <<= 1;
		}
	}
	*vertexCount = curVert;
	*indexCount = curInd;

#ifdef _BENCHMARK
	clock_t c2 = std::clock();
#endif
	make_texture(voxels, vertices, normals, uvs, tris, *vertexCount, texture, texW, texH);
#ifdef _BENCHMARK
	clock_t c3 = std::clock();
	double vtime = double(c2 - c1) / CLOCKS_PER_SEC;
	double ttime = double(c3 - c2) / CLOCKS_PER_SEC;
	double total = vtime + ttime;
	std::cout << "V/T Ratio: " << vtime/total << " / " << ttime/total << std::endl;
#endif
}

