#define DEFINE_PACKED_AABB_KERNEL(T, N, PTS) \
__kernel void aabb_packed( \
    __global const T* verts, \
    __global T*       result) \
{ \
    const int gid = get_global_id(0); \
    const int base_idx = gid * PTS; \
    \
    T##N minv = vload##N(base_idx + 0, verts); \
    T##N maxv = minv; \
    \
    for (int offset = 1; offset < PTS; offset++) { \
        T##N cur = vload##N(base_idx + offset, verts); \
        minv = fmin(minv, cur); \
        maxv = fmax(maxv, cur); \
    } \
    \
    vstore##N(minv, 2 * gid + 0, result); \
    vstore##N(maxv, 2 * gid + 1, result); \
}


#define DEFINE_INDEXED_AABB_KERNEL(T, N, PTS) \
__kernel void aabb_indexed( \
    __global const int*   indices, \
    __global const T*     verts, \
    __global T*           result) \
{ \
    const int gid = get_global_id(0); \
    const int base_idx = gid * PTS; \
    \
    T##N minv = vload##N(indices[base_idx], verts); \
    T##N maxv = minv; \
    \
    for (int offset = 1; offset < PTS; offset++) { \
        T##N cur = vload##N(indices[base_idx + offset], verts); \
        minv = fmin(minv, cur); \
        maxv = fmax(maxv, cur); \
    } \
    \
    vstore##N(minv, 2 * gid + 0, result); \
    vstore##N(maxv, 2 * gid + 1, result); \
}

