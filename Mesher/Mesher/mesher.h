#include <stdint.h>
#include <cstring>
#include <cmath>

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
	);

int vtab(int x, int y, int z);

int vtab_safe(int x, int y, int z, int shf = 0);

int vtab_safe(int* crd, int shf = 0);

bool is_voxel_visible(int* voxels, int x, int y, int z);

void uvd2crd(int u, int v, int d, int dim, int* crd);

void uvd_conv(int u, int v, int d, int dim, int side, int* crd, int &index, int &check);

bool work_check(uint8_t* work, int *voxels, int index, int check, int setbit);

int max(int a, int b);

float max(float a, float b);

float min(float a, float b);

bool uv_rect_check(uint8_t *work, int *voxels, int dim, int side, int depth, int setbit, int beginU, int beginV, int endU, int endV);

bool get_next_uv_rect(uint8_t *work, int *voxels, int dim, int side, int depth, int setbit, int &beginU, int &beginV, int &endU, int &endV);

void spawn_face(vector3* vertices, vector3* normals, vector2* uvs, int* tris,
	int &curVert, int &curInd, int beginU, int beginV, int endU, int endV, int dim, int depth, int side, bool liquid);

void dump_types(int* voxels, vector3* vptr, vector3* nptr, int *tempPatch, int &w, int &h);

void make_texture(int* voxels, vector3* vertices, vector3* normals, vector2* uvs, int* tris, int vcount, int* tex, int* texw, int* texh);