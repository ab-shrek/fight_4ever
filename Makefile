NVCC = nvcc
NVCC_FLAGS = -ptx -O3

.PHONY: all clean

all: kernels.ptx

kernels.ptx: kernels.cu
	$(NVCC) $(NVCC_FLAGS) -o $@ $<

clean:
	rm -f kernels.ptx 