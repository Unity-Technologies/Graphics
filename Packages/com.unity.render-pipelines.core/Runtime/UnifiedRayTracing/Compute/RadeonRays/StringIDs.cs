
namespace UnityEngine.Rendering.RadeonRays
{
    internal static class SID
    {
        public static readonly int g_vertices = Shader.PropertyToID("g_vertices");
        public static readonly int g_indices = Shader.PropertyToID("g_indices");
        public static readonly int g_scratch_buffer = Shader.PropertyToID("g_scratch_buffer");
        public static readonly int g_bvh = Shader.PropertyToID("g_bvh");
        public static readonly int g_bvh_leaves = Shader.PropertyToID("g_bvh_leaves");
        public static readonly int g_buffer = Shader.PropertyToID("g_buffer");
        public static readonly int g_primitive_refs_offset = Shader.PropertyToID("g_primitive_refs_offset");
        public static readonly int g_morton_codes_offset = Shader.PropertyToID("g_morton_codes_offset");
        public static readonly int g_constants_num_keys = Shader.PropertyToID("g_constants_num_keys");
        public static readonly int g_constants_input_keys_offset = Shader.PropertyToID("g_constants_input_keys_offset");
        public static readonly int g_constants_part_sums_offset = Shader.PropertyToID("g_constants_part_sums_offset");
        public static readonly int g_constants_output_keys_offset = Shader.PropertyToID("g_constants_output_keys_offset");
        public static readonly int g_constants_num_blocks = Shader.PropertyToID("g_constants_num_blocks");
        public static readonly int g_constants_bit_shift = Shader.PropertyToID("g_constants_bit_shift");
        public static readonly int g_input_keys_offset = Shader.PropertyToID("g_input_keys_offset");
        public static readonly int g_group_histograms_offset = Shader.PropertyToID("g_group_histograms_offset");
        public static readonly int g_output_keys_offset = Shader.PropertyToID("g_output_keys_offset");
        public static readonly int g_input_values_offset = Shader.PropertyToID("g_input_values_offset");
        public static readonly int g_output_values_offset = Shader.PropertyToID("g_output_values_offset");
        public static readonly int g_aabb_offset = Shader.PropertyToID("g_aabb_offset");
        public static readonly int g_constants_vertex_stride = Shader.PropertyToID("g_constants_vertex_stride");
        public static readonly int g_constants_triangle_count = Shader.PropertyToID("g_constants_triangle_count");
        public static readonly int g_constants_ray_count = Shader.PropertyToID("g_constants_ray_count");
        public static readonly int g_ray_count = Shader.PropertyToID("g_ray_count");
        public static readonly int g_rays = Shader.PropertyToID("g_rays");
        public static readonly int g_hits = Shader.PropertyToID("g_hits");
        public static readonly int g_constants_min_prims_per_treelet = Shader.PropertyToID("g_constants_min_prims_per_treelet");
        public static readonly int g_treelet_count_offset = Shader.PropertyToID("g_treelet_count_offset");
        public static readonly int g_treelet_roots_offset = Shader.PropertyToID("g_treelet_roots_offset");
        public static readonly int g_primitive_counts_offset = Shader.PropertyToID("g_primitive_counts_offset");
        public static readonly int g_treelet_dispatch_buffer = Shader.PropertyToID("g_treelet_dispatch_buffer");
        public static readonly int g_treelet_offset = Shader.PropertyToID("g_treelet_offset");
        public static readonly int g_remainder_treelets = Shader.PropertyToID("g_remainder_treelets");
        public static readonly int g_bvh_offset = Shader.PropertyToID("g_bvh_offset");
        public static readonly int g_bvh_leaves_offset = Shader.PropertyToID("g_bvh_leaves_offset");
        public static readonly int g_instance_infos = Shader.PropertyToID("g_instance_infos");
        public static readonly int g_bottom_bvhs = Shader.PropertyToID("g_bottom_bvhs");
        public static readonly int g_indices_offset = Shader.PropertyToID("g_indices_offset");
        public static readonly int g_base_index = Shader.PropertyToID("g_base_index");
        public static readonly int g_vertices_offset = Shader.PropertyToID("g_vertices_offset");
        public static readonly int g_bvh_node_count = Shader.PropertyToID("g_bvh_node_count");
        public static readonly int g_trace_index_buffer = Shader.PropertyToID("g_trace_index_buffer");
        public static readonly int g_trace_vertex_buffer = Shader.PropertyToID("g_trace_vertex_buffer");
        public static readonly int g_trace_vertex_stride = Shader.PropertyToID("g_trace_vertex_stride");
        public static readonly int g_sorted_prim_refs_offset = Shader.PropertyToID("g_sorted_prim_refs_offset");
        public static readonly int g_temp_indices_offset = Shader.PropertyToID("g_temp_indices_offset");
        public static readonly int g_internal_node_range_offset = Shader.PropertyToID("g_internal_node_range_offset");
        public static readonly int g_cluster_validity_offset = Shader.PropertyToID("g_cluster_validity_offset");
        public static readonly int g_cluster_range_offset = Shader.PropertyToID("g_cluster_range_offset");
        public static readonly int g_neighbor_offset = Shader.PropertyToID("g_neighbor_offset");
        public static readonly int g_cluster_to_node_offset = Shader.PropertyToID("g_cluster_to_node_offset");
        public static readonly int g_deltas_offset = Shader.PropertyToID("g_deltas_offset");
        public static readonly int g_leaf_parents_offset = Shader.PropertyToID("g_leaf_parents_offset");
    }
}
