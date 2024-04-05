@ECHO OFF
TITLE Punctuation Server Setup
CD /D %~DP0

python -m venv env
IF %errorlevel% NEQ  0 (
	ECHO Error creating environment
	PAUSE
	exit /B 0
)


CALL env\scripts\activate.bat
IF %errorlevel% NEQ  0 (
	ECHO Error activating environment
	PAUSE
	exit /B 0
)


pip install git+https://huggingface.co/kontur-ai/sbert_punc_case_ru
IF %errorlevel% NEQ  0 (
	ECHO Error downloading sbert_punc_case_ru
	PAUSE
	exit /B 0
)

python punctuation-server-setup.py
IF %errorlevel% NEQ  0 (
	ECHO Error running script punctuation-server-setup.py
	PAUSE
	exit /B 0
)

ECHO Check if there are errors and if all is ok, start punctuation-server.cmd
PAUSE