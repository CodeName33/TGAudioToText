@ECHO OFF
TITLE Punctuation Server
CD /D %~DP0

CALL ENV\Scripts\Activate.bat
IF %errorlevel% NEQ  0 (
	ECHO Error activating environment
	PAUSE
	exit /B 0
)
python punctuation-server.py