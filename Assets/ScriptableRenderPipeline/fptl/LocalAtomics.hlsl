#ifndef __LOCALATOMICS_H__
#define __LOCALATOMICS_H__



#ifdef EMUL_LOCAL_ATOMICS


groupshared unsigned int tgbuffer[NR_THREADS];

#define InterlockedOR(data, val, threadid)	\
{	\
	{	\
	tgbuffer[threadid] = (uint) val;	\
	GroupMemoryBarrier();	\
	if((threadid&0x3)==0)	\
	{	\
		tgbuffer[threadid+0] |= (tgbuffer[threadid+1] | tgbuffer[threadid+2] | tgbuffer[threadid+3]);	\
	}	\
	GroupMemoryBarrier();	\
	if(threadid==0)	\
	{	\
		uint result = tgbuffer[0];	\
		for(int i=4; i<NR_THREADS; i+=0x4)  { result |= tgbuffer[i]; }	\
		data |= result; \
	}	\
	GroupMemoryBarrier();	\
	}	\
}

#define InterlockedMAX(data, val, threadid)	\
{	\
	{	\
	tgbuffer[threadid] = (uint) val;	\
	GroupMemoryBarrier();	\
	if((threadid&0x3)==0)	\
	{	\
		tgbuffer[threadid+0] = max( max(tgbuffer[threadid+0], tgbuffer[threadid+1]), max(tgbuffer[threadid+2],tgbuffer[threadid+3]) );	\
	}	\
	GroupMemoryBarrier();	\
	if(threadid==0)	\
	{	\
		uint result = tgbuffer[0];	\
		for(int i=4; i<NR_THREADS; i+=0x4)  { result = max(result, tgbuffer[i]); }	\
		data = max(data,result); \
	}	\
	GroupMemoryBarrier();	\
	}	\
}

#define InterlockedMIN(data, val, threadid)	\
{	\
	{	\
	tgbuffer[threadid] = (uint) val;	\
	GroupMemoryBarrier();	\
	if((threadid&0x3)==0)	\
	{	\
		tgbuffer[threadid+0] = min( min(tgbuffer[threadid+0], tgbuffer[threadid+1]), min(tgbuffer[threadid+2],tgbuffer[threadid+3]) );	\
	}	\
	GroupMemoryBarrier();	\
	if(threadid==0)	\
	{	\
		uint result = tgbuffer[0];	\
		for(int i=4; i<NR_THREADS; i+=0x4)  { result = min(result, tgbuffer[i]); }	\
		data = min(data,result); \
	}	\
	GroupMemoryBarrier();	\
	}	\
}

#define InterlockedADD(data, val, threadid, idx, nrIterations)	\
{	\
	{	\
	const int nrActiveThreads = min(NR_THREADS, nrIterations-(idx&(~(NR_THREADS-1)))); \
	tgbuffer[threadid] = (uint) val;	\
	GroupMemoryBarrier();	\
	if((threadid&0x3)==0)	\
	{	\
		uint val1 = (threadid+1)<nrActiveThreads ? tgbuffer[threadid+1] : 0;	\
		uint val2 = (threadid+2)<nrActiveThreads ? tgbuffer[threadid+2] : 0;	\
		uint val3 = (threadid+3)<nrActiveThreads ? tgbuffer[threadid+3] : 0;	\
		tgbuffer[threadid+0] += (val1+val2+val3);	\
	}	\
	GroupMemoryBarrier();	\
	if(threadid==0)	\
	{	\
		uint result = tgbuffer[0];	\
		for(int i=4; i<NR_THREADS; i+=0x4)	\
		{	\
			result += (i<nrActiveThreads ? tgbuffer[i] : 0);	\
		}	\
		data += result; \
	}	\
	GroupMemoryBarrier();	\
	}	\
}





#define InterlockedADDAndPrev(data, val, prevval, threadid, idx, nrIterations)	\
{	\
	{	\
	const int nrActiveThreads = min(NR_THREADS, nrIterations-(idx&(~(NR_THREADS-1)))); \
	tgbuffer[threadid] = (uint) val;	\
	GroupMemoryBarrier();	\
	if((threadid&0x3)==0)	\
	{	\
		for(int i=1; i<4; i++) tgbuffer[threadid+i] += tgbuffer[threadid+i-1];	\
	}	\
	GroupMemoryBarrier();	\
	if(threadid==0)	\
	{	\
		for(int i=0x7; i<NR_THREADS; i+=0x4) tgbuffer[i] += tgbuffer[i-0x4];	\
	}	\
	GroupMemoryBarrier();	\
	uint prevblock = tgbuffer[max(1,threadid)-1];	\
	GroupMemoryBarrier();	\
	if((threadid&0x3)==0 && threadid>0)	\
	{	\
		for(int i=0; i<3; i++) tgbuffer[threadid+i] += prevblock;	\
	}	\
	uint orgdata = data;	\
	GroupMemoryBarrier();	\
	prevval = (orgdata + tgbuffer[threadid]) - val;	\
	if(threadid==0) data = orgdata + tgbuffer[nrActiveThreads-1];	\
	GroupMemoryBarrier();	\
	}	\
}

#else

#define InterlockedOR(data, val, threadid)	InterlockedOr(data, val)
#define InterlockedMAX(data, val, threadid)	InterlockedMax(data, val)
#define InterlockedMIN(data, val, threadid)	InterlockedMin(data, val)
#define InterlockedADD(data, val, threadid, idx, nrIterations)	InterlockedAdd(data, val)
#define InterlockedADDAndPrev(data, val, prevval, threadid, idx, nrIterations)	InterlockedAdd(data, val, prevval)


#endif





#endif
