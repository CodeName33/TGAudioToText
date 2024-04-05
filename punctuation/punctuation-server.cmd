@ECHO OFF
TITLE Punctuation Server
CD /D %~DP0

CALL ENV\Scripts\Activate.bat
python punctuation-server.py