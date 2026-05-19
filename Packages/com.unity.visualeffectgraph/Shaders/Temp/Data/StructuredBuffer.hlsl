// File can be included many times, for different types
// Requires defining __VFX_STRUCTURED_BUFFER_TYPE before including this file.
// __VFX_STRUCTURED_BUFFER_TYPE will be undefined at the end

#ifdef __VFX_STRUCTURED_BUFFER_TYPE

#define __VFX_STRUCTURED_BUFFER(type) VFXStructuredBuffer_##type
#define __VFX_STRUCTURED_BUFFER_EMPTY(type) __VFXEmptyStructuredBuffer_##type
#define __VFX_STRUCTURED_BUFFER_RW_EMPTY(type) __VFXEmptyRWStructuredBuffer_##_type

StructuredBuffer<__VFX_STRUCTURED_BUFFER_TYPE> __VFX_STRUCTURED_BUFFER_EMPTY(__VFX_STRUCTURED_BUFFER_TYPE);
RWStructuredBuffer<__VFX_STRUCTURED_BUFFER_TYPE> __VFX_STRUCTURED_BUFFER_RW_EMPTY(__VFX_STRUCTURED_BUFFER_TYPE);

struct __VFX_STRUCTURED_BUFFER(__VFX_STRUCTURED_BUFFER_TYPE)
{
    StructuredBuffer<__VFX_STRUCTURED_BUFFER_TYPE> buffer;
    RWStructuredBuffer<__VFX_STRUCTURED_BUFFER_TYPE> bufferRW;
    uint offset;
    uint size;
    //TODO: STRIDE?
    bool readAccess;
    bool writeAccess;
    bool rangeCheck;

    void Init(StructuredBuffer<__VFX_STRUCTURED_BUFFER_TYPE> buffer, uint offset, uint size)
    {
        this.buffer = buffer;
        this.bufferRW = __VFX_STRUCTURED_BUFFER_RW_EMPTY(__VFX_STRUCTURED_BUFFER_TYPE);
        this.offset = offset;
        this.size = size;
        this.readAccess = true;
        this.writeAccess = false;
        this.rangeCheck = false;
    }

    void Init(RWStructuredBuffer<__VFX_STRUCTURED_BUFFER_TYPE> buffer, uint offset, uint size)
    {
        this.buffer = __VFX_STRUCTURED_BUFFER_EMPTY(__VFX_STRUCTURED_BUFFER_TYPE);
        this.bufferRW = buffer;
        this.offset = offset;
        this.size = size;
        this.readAccess = true;
        this.writeAccess = true;
        this.rangeCheck = false;
    }
	
	__VFX_STRUCTURED_BUFFER(__VFX_STRUCTURED_BUFFER_TYPE) GetRange(uint from, uint count)
	{
		from = min(from, size - 1);
		count = min(count, size - from);
		
		__VFX_STRUCTURED_BUFFER(__VFX_STRUCTURED_BUFFER_TYPE) rangeBuffer;
        rangeBuffer.buffer = buffer;
        rangeBuffer.bufferRW = bufferRW;
        rangeBuffer.offset = offset + from;
        rangeBuffer.size = count;
        rangeBuffer.readAccess = readAccess;
        rangeBuffer.writeAccess = writeAccess;
        rangeBuffer.rangeCheck = rangeCheck;
		
		return rangeBuffer;
	}

    bool LoadData(out __VFX_STRUCTURED_BUFFER_TYPE data, uint index)
    {
        bool valid = !rangeCheck || index < size;
        data = (__VFX_STRUCTURED_BUFFER_TYPE)0;
        if (valid && readAccess)
        {
            if (writeAccess)
            {
                data = bufferRW[offset + index];
            }
            else
            {
                data = buffer[offset + index];
            }
        }
        return valid;
    }

    bool StoreData(__VFX_STRUCTURED_BUFFER_TYPE data, uint index)
    {
        bool valid = !rangeCheck || index < size;
        if (valid && writeAccess)
        {
            bufferRW[offset + index] = data;
        }
        return valid;
    }

#if __VFX_STRUCTURED_BUFFER_TYPE == uint || __VFX_STRUCTURED_BUFFER_TYPE == int
    void DoInterlockedAdd(uint index, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedAdd(bufferRW[index], value, original);
        }
    }

    void DoInterlockedAnd(uint index, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedAnd(bufferRW[index], value, original);
        }
    }

    void DoInterlockedOr(uint index, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedOr(bufferRW[index], value, original);
        }
    }

    void DoInterlockedXor(uint index, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedXor(bufferRW[index], value, original);
        }
    }

    void DoInterlockedMin(uint index, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedMin(bufferRW[index], value, original);
        }
    }

    void DoInterlockedMax(uint index, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedMax(bufferRW[index], value, original);
        }
    }

    void DoInterlockedCompareStore(uint index, __VFX_STRUCTURED_BUFFER_TYPE compare, __VFX_STRUCTURED_BUFFER_TYPE value)
    {
        if (writeAccess)
        {
            InterlockedCompareStore(bufferRW[index], compare, value);
        }
    }

    void DoInterlockedCompareExchange(uint index, __VFX_STRUCTURED_BUFFER_TYPE compare, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedCompareExchange(bufferRW[index], compare, value, original);
        }
    }
#endif

#ifdef __VFX_STRUCTURED_BUFFER_EXCHANGE
    void DoInterlockedExchange(uint index, __VFX_STRUCTURED_BUFFER_TYPE compare, __VFX_STRUCTURED_BUFFER_TYPE value, out __VFX_STRUCTURED_BUFFER_TYPE original)
    {
        original = 0;
        if (writeAccess)
        {
            InterlockedExchange(bufferRW[index], compare, value);
        }
    }
#endif
};

#undef __VFX_STRUCTURED_BUFFER
#undef __VFX_STRUCTURED_BUFFER_EMPTY
#undef __VFX_STRUCTURED_BUFFER_RW_EMPTY
#undef __VFX_STRUCTURED_BUFFER_EXCHANGE

#undef __VFX_STRUCTURED_BUFFER_TYPE
#endif // __VFX_STRUCTURED_BUFFER_TYPE
