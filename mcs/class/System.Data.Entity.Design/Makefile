thisdir = class/System.Data.Entity.Design
SUBDIRS = 
include ../../build/rules.make

LIBRARY = System.Data.Entity.Design.dll

LIB_MCS_FLAGS = -r:System.dll \
		-r:System.Core.dll -r:System.Data.dll \
		-r:System.Xml.dll -r:System.Xml.Linq.dll \
		-r:System.Transactions.dll \
		-r:System.Runtime.Serialization.dll \
		-r:System.ComponentModel.DataAnnotations.dll \
		-r:System.Configuration.dll \
		-r:Microsoft.Build.Utilities.v4.0.dll \
		-r:System.Web.dll \
		-r:System.Data.Entity.dll \
		-r:System.Data.DataSetExtensions.dll \
		-warn:2

RESOURCE_STRINGS = ../../../external/referencesource/System.Data.Entity.Design/System.Data.Entity.Design.txt

include ../../build/library.make
