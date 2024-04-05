@ECHO OFF
TITLE Punctuation Server Setup
CD /D %~DP0

python -m venv env
CALL env\scripts\activate.bat
pip install git+https://huggingface.co/kontur-ai/sbert_punc_case_ru
python punctuation-server-setup.py

ECHO Check if there are errors and if all is ok, start punctuation-server.cmd
PAUSE