thisdir = class/System.Web.Entity
SUBDIRS =
include ../../build/rules.make

LIBRARY = System.Web.Entity.dll

LIB_MCS_FLAGS = \
	-unsafe \
	-define:NET_3_5			\
	-r:$(corlib)	 		\
	-r:System.dll 			\
	-r:System.Core.dll		\
	-r:System.Drawing.dll 		\
	-r:System.Data.dll		\
	-r:System.Data.Entity.dll		\
	-r:System.Xml.dll		\
	-r:System.Web.dll		\
	-r:System.Web.Extensions.dll		\
	-r:System.Web.Services.dll	\
	-r:System.Configuration.dll	\
	-r:System.Windows.Forms.dll

include ../../build/library.make

