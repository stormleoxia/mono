thisdir = class/System.Web.RegularExpressions
SUBDIRS =
include ../../build/rules.make

PROGRAM = System.Web.RegularExpressions-gen.exe

LIB_MCS_FLAGS = \
	-unsafe \
	-define:NET_3_5				\
	-r:$(corlib)	 			\
	-r:System.dll
	
include ../../build/executable.make

all:System.Web.RegularExpressions.dll
	echo "Remove generator"
	rm -f ../lib/net_4_5/System.Web.RegularExpressions-gen.exe ../lib/net_4_5/System.Web.RegularExpressions-gen.exe.mdb
	
System.Web.RegularExpressions.dll: all-local
	echo "Generate Library"
	../../../runtime/mono-wrapper ../lib/net_4_5/System.Web.RegularExpressions-gen.exe ../mono.snk
	echo "Sign Generated Library"
	../../../runtime/mono-wrapper ../lib/build/sn.exe -R System.Web.RegularExpressions.dll ../mono.snk
