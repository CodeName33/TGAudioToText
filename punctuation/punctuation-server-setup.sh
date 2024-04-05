#!/bin/bash -x
python -m venv ./env
source ./env/bin/activate
pip install git+https://huggingface.co/kontur-ai/sbert_punc_case_ru
python ./punctuation-server-setup.py

echo Check if there are errors and if all is ok, start punctuation-server.sh