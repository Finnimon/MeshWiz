
#pragma OPENCL EXTENSION cl_khr_global_int32_base_atomics : enable
#pragma OPENCL EXTENSION cl_khr_local_int32_base_atomics  : enable

#define BIN_COUNT 16
#define LEAF_SIZE 4
#define FLT_MAX_VAL 3.402823466e+38f

__kernel void tri_bounds_soa(
    __global const int*   triIndices,   
    __global const float* verts,        

    __global float* minX,
    __global float* minY,
    __global float* minZ,

    __global float* maxX,
    __global float* maxY,
    __global float* maxZ,

    __global float* cx,
    __global float* cy,
    __global float* cz)
{
    int t = get_global_id(0);

    int i0 = triIndices[t*3+0];
    int i1 = triIndices[t*3+1];
    int i2 = triIndices[t*3+2];

    float3 a = vload3(i0, verts);
    float3 b = vload3(i1, verts);
    float3 c = vload3(i2, verts);

    float3 lo = fmin(fmin(a,b),c);
    float3 hi = fmax(fmax(a,b),c);

    minX[t] = lo.x; minY[t] = lo.y; minZ[t] = lo.z;
    maxX[t] = hi.x; maxY[t] = hi.y; maxZ[t] = hi.z;

    float3 cent = (a + b + c) * (1.0f/3.0f);
    cx[t] = cent.x;
    cy[t] = cent.y;
    cz[t] = cent.z;
}

__kernel void reduce_bounds_soa(
    __global const float* minX,
    __global const float* minY,
    __global const float* minZ,
    __global const float* maxX,
    __global const float* maxY,
    __global const float* maxZ,

    __global float* outMinX,
    __global float* outMinY,
    __global float* outMinZ,
    __global float* outMaxX,
    __global float* outMaxY,
    __global float* outMaxZ,

    int count)
{
    __local float lminX[256], lminY[256], lminZ[256];
    __local float lmaxX[256], lmaxY[256], lmaxZ[256];

    int lid = get_local_id(0);
    int gid = get_global_id(0);

    float mnx = FLT_MAX_VAL, mny = FLT_MAX_VAL, mnz = FLT_MAX_VAL;
    float mxx = -FLT_MAX_VAL, mxy = -FLT_MAX_VAL, mxz = -FLT_MAX_VAL;

    if (gid < count) {
        mnx = minX[gid]; mny = minY[gid]; mnz = minZ[gid];
        mxx = maxX[gid]; mxy = maxY[gid]; mxz = maxZ[gid];
    }

    lminX[lid] = mnx; lminY[lid] = mny; lminZ[lid] = mnz;
    lmaxX[lid] = mxx; lmaxY[lid] = mxy; lmaxZ[lid] = mxz;

    barrier(CLK_LOCAL_MEM_FENCE);

    for (int s = get_local_size(0)/2; s > 0; s >>= 1) {
        if (lid < s) {
            lminX[lid] = fmin(lminX[lid], lminX[lid+s]);
            lminY[lid] = fmin(lminY[lid], lminY[lid+s]);
            lminZ[lid] = fmin(lminZ[lid], lminZ[lid+s]);

            lmaxX[lid] = fmax(lmaxX[lid], lmaxX[lid+s]);
            lmaxY[lid] = fmax(lmaxY[lid], lmaxY[lid+s]);
            lmaxZ[lid] = fmax(lmaxZ[lid], lmaxZ[lid+s]);
        }
        barrier(CLK_LOCAL_MEM_FENCE);
    }

    if (lid == 0) {
        outMinX[get_group_id(0)] = lminX[0];
        outMinY[get_group_id(0)] = lminY[0];
        outMinZ[get_group_id(0)] = lminZ[0];
        outMaxX[get_group_id(0)] = lmaxX[0];
        outMaxY[get_group_id(0)] = lmaxY[0];
        outMaxZ[get_group_id(0)] = lmaxZ[0];
    }
}

__kernel void sah_bin_soa(
    __global const float* triMinX,
    __global const float* triMinY,
    __global const float* triMinZ,
    __global const float* triMaxX,
    __global const float* triMaxY,
    __global const float* triMaxZ,
    __global const float* triC,

    __global const int* rangeStart,
    __global const int* rangeCount,

    __global float* binMinX,
    __global float* binMinY,
    __global float* binMinZ,
    __global float* binMaxX,
    __global float* binMaxY,
    __global float* binMaxZ,
    __global int*   binCount,

    float invExtent)
{
    int lid = get_local_id(0);
    int rid = get_group_id(0);

    __local float lMinX[BIN_COUNT], lMinY[BIN_COUNT], lMinZ[BIN_COUNT];
    __local float lMaxX[BIN_COUNT], lMaxY[BIN_COUNT], lMaxZ[BIN_COUNT];
    __local int   lCnt[BIN_COUNT];

    if (lid < BIN_COUNT) {
        lMinX[lid] = lMinY[lid] = lMinZ[lid] = FLT_MAX_VAL;
        lMaxX[lid] = lMaxY[lid] = lMaxZ[lid] = -FLT_MAX_VAL;
        lCnt[lid]  = 0;
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int start = rangeStart[rid];
    int count = rangeCount[rid];

    for (int i = lid; i < count; i += get_local_size(0)) {
        int t = start + i;
        int b = clamp((int)(triC[t] * invExtent * BIN_COUNT), 0, BIN_COUNT-1);

        atomic_inc(&lCnt[b]);
        lMinX[b] = fmin(lMinX[b], triMinX[t]);
        lMinY[b] = fmin(lMinY[b], triMinY[t]);
        lMinZ[b] = fmin(lMinZ[b], triMinZ[t]);
        lMaxX[b] = fmax(lMaxX[b], triMaxX[t]);
        lMaxY[b] = fmax(lMaxY[b], triMaxY[t]);
        lMaxZ[b] = fmax(lMaxZ[b], triMaxZ[t]);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    if (lid < BIN_COUNT) {
        int o = rid * BIN_COUNT + lid;
        binMinX[o] = lMinX[lid];
        binMinY[o] = lMinY[lid];
        binMinZ[o] = lMinZ[lid];
        binMaxX[o] = lMaxX[lid];
        binMaxY[o] = lMaxY[lid];
        binMaxZ[o] = lMaxZ[lid];
        binCount[o] = lCnt[lid];
    }
}

__kernel void partition_soa(
    __global const float* triC,
    __global const int*   triIn,
    __global int*         triOut,

    int start,
    int count,
    float splitPos,

    __global int* counters)
{
    int i = get_global_id(0);
    if (i >= count) return;

    int t = start + i;
    int idx = triIn[t];

    int side = triC[idx] < splitPos;
    int off = atomic_inc(&counters[side]);

    triOut[
        side ? start + off
             : start + counters[0] + off
    ] = idx;
}
__kernel void emit_nodes_soa(
    __global const float* rangeMinX,
    __global const float* rangeMinY,
    __global const float* rangeMinZ,
    __global const float* rangeMaxX,
    __global const float* rangeMaxY,
    __global const float* rangeMaxZ,
    __global const int*   rangeStart,
    __global const int*   rangeCount,

    __global float* nodeMinX,
    __global float* nodeMinY,
    __global float* nodeMinZ,
    __global float* nodeMaxX,
    __global float* nodeMaxY,
    __global float* nodeMaxZ,
    __global int*   nodeFirst,
    __global int*   nodeSecond)
{
    int n = get_global_id(0);

    nodeMinX[n] = rangeMinX[n];
    nodeMinY[n] = rangeMinY[n];
    nodeMinZ[n] = rangeMinZ[n];
    nodeMaxX[n] = rangeMaxX[n];
    nodeMaxY[n] = rangeMaxY[n];
    nodeMaxZ[n] = rangeMaxZ[n];

    int count = rangeCount[n];
    if (count <= LEAF_SIZE) {
        nodeFirst[n]  = rangeStart[n];
        nodeSecond[n] = -count;
    }
}
