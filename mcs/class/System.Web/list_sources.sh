#!/bin/sh

if [ $# != 2 ] 
then
  echo No enough parameters
  exit 1
fi

echo > $2
for file in $(find $1)
do
	if [ ! -d $file ]
	then
		echo $file >> $2
	fi

done
