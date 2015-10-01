#!/bin/sh

echo > System.Data.Entity.Design.dll.sources
for file in $(find ../../../external/referencesource/System.Data.Entity.Design)
do
	if [ ! -d $file ]
	then
		echo $file >> System.Data.Entity.Design.dll.sources
	fi

done
