#include "mesher.h"



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
	int* texH
	)
{
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
					spawn_face(vertices, normals, uvs, tris, curVert, curInd, beginU, beginV, endU, endV, dim, depth, side);
				}
			}
			setbit <<= 1;
		}
	}
	*vertexCount = curVert;
	*indexCount = curInd;

	make_texture(voxels, vertices, normals, uvs, tris, *vertexCount, texture, texW, texH);
}


//if (v < endV)
//{
//	endU--;
//}
//
//int rect[4] = { beginU, beginV, endU + 1, endV + 1 };
//for (int r = 0; r < 4; r++)
//{
//	vertices[curVert + r].m[dim] = depth + so;
//	vertices[curVert + r].m[dimu] = rect[(r / 2) * 2];
//	vertices[curVert + r].m[dimv] = rect[(r % 2) * 2 + 1];
//	normals[curVert + r].m[dim] = -side;
//	normals[curVert + r].m[dimu] = 0;
//	normals[curVert + r].m[dimv] = 0;
//}
//
//if (side > 0)
//{
//	tris[curInd + 0] = curVert + 0;
//	tris[curInd + 1] = curVert + 1;
//	tris[curInd + 2] = curVert + 2;
//
//	tris[curInd + 3] = curVert + 3;
//	tris[curInd + 4] = curVert + 2;
//	tris[curInd + 5] = curVert + 1;
//}
//else
//{
//	tris[curInd + 0] = curVert + 2;
//	tris[curInd + 1] = curVert + 1;
//	tris[curInd + 2] = curVert + 0;
//
//	tris[curInd + 3] = curVert + 1;
//	tris[curInd + 4] = curVert + 2;
//	tris[curInd + 5] = curVert + 3;
//}
//curVert += 4;
//curInd += 6;
//
//v = endV;
//u = beginU;
//
//beginU = -1;
//endU = -1;
//endV = -1;
//expandU = false;