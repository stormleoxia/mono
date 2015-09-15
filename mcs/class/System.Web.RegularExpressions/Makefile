thisdir = class/System.Web.RegularExpressions
SUBDIRS =
include ../../build/rules.make

LIBRARY = System.Web.RegularExpressions-gen.exe

LIB_MCS_FLAGS = \
	-unsafe \
	-define:NET_3_5				\
	-r:$(corlib)	 			\
	-r:System.dll
	

include ../../build/library.make

