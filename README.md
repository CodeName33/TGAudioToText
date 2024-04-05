# TGAudioToText
Convert Telegram Audio Messages To Text

# Intro
Ths app runs on your server and connects to your telegram account using Telegram Api (https://core.telegram.org/api/obtaining_api_id) and when you sends (in group or personal chats) or receives (in personal chats) voice messages it recognizes text and writes it in chat (as your messages with mark that bot recognized this text)

# Instruction
1. Download release or build it from sources (I used .Net Core 3.1 on Windows)
2. Install FFMPEG
   Linux:
     sudo apt-get install ffmpeg - in debian based
   Windows:
     Download binaries from https://ffmpeg.org/download.html#build-windows and put it to app's folder (ffmpeg.exe must be in same folder as TGAudioToText.exe, or just set PATH variable to ffmpeg folder)
3. Download VOSK models from https://alphacephei.com/vosk/models and extract model folder to app's folder
4. Run TGAudioToText and it will create config template (TGAudioToText.cfg) in app's folder
5. Fill this fields in config:
   ModelName - VOSK model folder name
   TelegramPhone - Telegram phone number
   TelegramApiId - Telegram Api id and ApiHash (https://core.telegram.org/api/obtaining_api_id)
   TelegramApiHash
6. Run app, at first time it will require confirmation code that will be sent to your telegram client. Enter it ant all done.

# Punctuation
In v1.1 EN/RU punctuation added from https://huggingface.co/kontur-ai/sbert_punc_case_ru using python

To enable punctuation:
1. Install lastest Python 3 (For Linux VENV, PIP modules must be installed too)
2. Change "PunctuationEnabled" parameter to True in TGAudioToText.cfg
3. "PunctuationServer" parameter is already has default value http://127.0.0.1:8018/ for punctuation server default port
4. In "punctuation" folder (in app's folder) run "punctuation-server-setup" (.cmd for Windows, .sh for Linux)
5. If all is ok, start "punctuation-server" (.cmd for Windows, .sh for Linux)
6. Run TGAudioToText not it can call punctuation server to punctuate text.
