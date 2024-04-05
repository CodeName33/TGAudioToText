#!/bin/bash

source ./env/bin/activate
if [ $? -ne 0 ];
then
	echo "Error activate environment";
	exit 1;
fi

python ./punctuation-server.py
