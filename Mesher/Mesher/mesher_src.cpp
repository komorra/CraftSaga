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
	int voxels[CS * CS * CS];
	uint8_t work[CS * CS * CS];
	
	memset(voxels, 0, CS*CS*CS*sizeof(int));
	memset(work, 0, CS*CS*CS);

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
	bool expandU = false;

	for (int dim = 0; dim < 3; dim++)
	{
		int dimu = (dim + 1) % 3;
		int dimv = (dim + 2) % 3;
		for (int side = -1; side <= 1; side+=2)
		{
			int so = (side > 0) ? 0 : 1; //side offset
			for (int depth = 0; depth < CS; depth++)
			{
				beginU = -1;
				endU = -1;
				endV = -1;
				expandU = false;

				for (int u = 0; u < CS; u++)
				{
					for (int v = 0; v < CS; v++)
					{
						int crd[3] = { 0, 0, 0 };

						uvd2crd(u, v, depth, dim, crd);
						int index = vtab(crd[0], crd[1], crd[2]);
						crd[dim] += side;
						int check = vtab_safe(crd);
						crd[dim] -= side;
						
						if (((work[index] & 1) == 1) && ((work[check] & 1) == 0) && ((work[index] & setbit) != setbit))
						{
							if (beginU == -1)
							{
								beginU = u;
								beginV = v;
							}							
							else if (!expandU) //V expanding
							{
								endV = v;
							}
							else //U expanding
							{
								endU = u;
							}
							work[index] |= setbit;
						}
						else if (beginU != -1)
						{	
							if (endU != -1)
							{
								//TODO: Add face generation code
								// ...... Here .....
								if (v < endV)
								{
									endU--;
								}
								
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
								curVert += 4;
								if (side < 0)
								{
									tris[curInd + 0] = 0;
									tris[curInd + 1] = 1;
									tris[curInd + 2] = 2;

									tris[curInd + 3] = 1;
									tris[curInd + 4] = 2;
									tris[curInd + 5] = 3;
								}
								else
								{
									tris[curInd + 0] = 2;
									tris[curInd + 1] = 1;
									tris[curInd + 2] = 0;

									tris[curInd + 3] = 3;
									tris[curInd + 4] = 2;
									tris[curInd + 5] = 1;
								}
								curInd += 6;

								beginU = -1;
								endU = -1;
								endV = -1;
								expandU = false;

								v = endV;
								u = beginU;
							}
						}						
					}
					if (beginU != -1)
					{
						expandU = true;
					}
				}
			}
			setbit << 1;
		}
	}
}