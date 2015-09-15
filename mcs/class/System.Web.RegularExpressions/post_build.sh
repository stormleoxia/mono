#!/bin/sh

ProjectDir=$(pwd)
TargetPath=System.Web.RegularExpressions-gen.exe

TargetDir=../lib/net_4_5

BUILDDIR=${ProjectDir}/../lib/build

SN=../../lib/build/sn.exe
MONOVM=${ProjectDir}/../../../runtime/mono-wrapper
KEYFILE=../../mono.snk

MONO_PATH="${BUILDDIR};${MONO_PATH}"

GENERATED=System.Web.RegularExpressions.dll

OUTPUT=${ProjectDir}/../lib/net_4_5

echo "Go in ${TargetDir} ..."

cd ${TargetDir}



echo "Sign the generator in mono environment (to be able to execute it}"

# Sign the generator in mono environment (to be able to execute it)
echo ${MONOVM} ${SN} -R ${TargetPath} ${KEYFILE}
${MONOVM} ${SN} -R ${TargetPath} ${KEYFILE}

if [ ! $? -eq 0 ]; then exit 1; fi

echo "Execute the generator"

# Execute the generator
${MONOVM} ${TargetPath} ${KEYFILE}

if [ ! $? -eq 0 ]; then exit 1; fi

echo "Sign assembly generated"

# Sign the generated assembly in mono environment
${MONOVM} ${SN} -R ${GENERATED} ${KEYFILE}

