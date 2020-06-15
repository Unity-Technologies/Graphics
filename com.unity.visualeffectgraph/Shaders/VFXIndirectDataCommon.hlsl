// additional indirect data
#define INDIRECT_DATA_SPAWN_COUNT 0 // current spawn count 
#define INDIRECT_DATA_TOTAL_COUNT 1 // total count

uint GetIndirectDataIndex(uint data, uint index)
{
    return (index * 2 + data) << 2;
}

#define LOAD_INDIRECT_DATA(buffer, data, index) (buffer.Load(GetIndirectDataIndex(data, index)))
#define STORE_INDIRECT_DATA(buffer, data, index, value) (buffer.Store(GetIndirectDataIndex(data, index), value))