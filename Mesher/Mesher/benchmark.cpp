#include "mesher.h"
#include <iostream>
#include <ctime>
#include <random>

int main()
{
	std::cout << "Benchmark starting ..." << std::endl;

	srand(time(0));

	vector3* vertices = new vector3[4 * 16 * 16 * 16 * 6];
	vector3* normals = new vector3[4 * 16 * 16 * 16 * 6];
	vector2* uvs = new vector2[4 * 16 * 16 * 16 * 6];
	int* tris = new int[6 * 16 * 16 * 16 * 6];
	int vcount = 0;
	int icount = 0;
	int* tex = new int[256 * 256];
	int texw = 0;
	int texh = 0;
	uint64_t* coords = new uint64_t[16 * 16 * 16];
	uint32_t* types = new uint32_t[16 * 16 * 16];

	const int passes = 100;

	double processingTime = 0;
	for (int la = 0; la < passes; la++)
	{
		std::cout << "Pass " << (la+1) << " / " << passes << std::endl;
		int voxelCount = rand() % (16 * 16 * 16) + 800;
		if (voxelCount < 10)voxelCount = 10;
		if (voxelCount > 16 * 16 * 16) voxelCount = 16 * 16 * 16;
		for (int lb = 0; lb < voxelCount; lb++)
		{
			int16_t* crd = (int16_t*)(coords + lb);
			crd[0] = rand() % 16;
			crd[1] = rand() % 16;
			crd[2] = rand() % 16;
			types[lb] = (rand() % 4) + 1;
		}
		clock_t begin = std::clock();
		MeshVoxels(voxelCount, coords, types, vertices, normals, uvs, tris, &vcount, &icount, tex, &texw, &texh, false);
		clock_t end = std::clock();
		double elapsed = double(end - begin) / CLOCKS_PER_SEC;
		processingTime += elapsed;
		std::cout << "Pass result | voxels:" << voxelCount << " time(s):" << elapsed << " vps:" << voxelCount / elapsed << std::endl;
	}

	std::cout << "Benchmark time total(s): " << processingTime << " ; CPS: " << passes / processingTime << std::endl << std::endl;

	delete[] vertices;
	delete[] normals;
	delete[] uvs;
	delete[] tris;
	delete[] tex;
	system("pause");
}