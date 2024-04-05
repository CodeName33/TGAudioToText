#!/bin/bash
python -m venv ./env
if [ $? -ne 0 ];
then
	echo "Error creating environment";
	exit 1;
fi

source ./env/bin/activate
if [ $? -ne 0 ];
then
	echo "Error activate environment";
	exit 1;
fi

pip install git+https://huggingface.co/kontur-ai/sbert_punc_case_ru
if [ $? -ne 0 ];
then
	echo "Error downloading sbert_punc_case_ru";
	exit 1;
fi

python ./punctuation-server-setup.py
if [ $? -ne 0 ];
then
	echo "Error running script punctuation-server-setup.py";
	exit 1;
fi

echo "Check if there are errors and if all is ok, start punctuation-server.sh"