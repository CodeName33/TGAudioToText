# TGAudioToText

Convert Telegram Audio Messages To Text

## Intro

Ths app runs on your server and connects to your telegram account using Telegram Api ([Telegram - Obtaining ApiId and ApiHash](https://core.telegram.org/api/obtaining_api_id)) and when you send or receive voice messages in group or personal chats, program recognizes text and writes it in chat as your messages with mark that bot recognized this text.

## Instruction

1. Download release or build it from sources with .Net Core 3.1
2. Install FFMPEG
   - Linux:
     `sudo apt-get install ffmpeg` - in debian based
   - Windows:
     Download binaries from [FFmpeg releases](https://ffmpeg.org/download.html#build-windows) and put it to app's folder (ffmpeg.exe must be in same folder as TGAudioToText.exe, or just set PATH variable to ffmpeg folder)
3. Download [VOSK Models](https://alphacephei.com/vosk/models) and extract model folder to app folder
4. Run TGAudioToText and it will create config template (TGAudioToText.cfg) in app folder
5. Fill this fields in config:
   ModelName - VOSK model folder name
   TelegramPhone - Telegram phone number
   TelegramApiId - Telegram Api id and ApiHash
   TelegramApiHash
   [Telegram - How to get ApiId and ApiHash](https://core.telegram.org/api/obtaining_api_id)
6. Run app, at first time it will require confirmation code that will be sent to your telegram client. Enter it and all done.

## Punctuation

In v1.1 EN/RU punctuation added from [huggingface - sbert_punc_case_ru](https://huggingface.co/kontur-ai/sbert_punc_case_ru) using python

To enable punctuation:

1. Install latest Python 3 (For Linux VENV, PIP modules must be installed too)
2. Change "PunctuationEnabled" parameter to True in TGAudioToText.cfg
3. "PunctuationServer" parameter is already has default value `http://127.0.0.1:8018/` for punctuation server default port
4. In "punctuation" folder (in app's folder) run "punctuation-server-setup" (.cmd for Windows, .sh for Linux)
5. If all is ok, start "punctuation-server" (.cmd for Windows, .sh for Linux)
6. Run TGAudioToText not it can call punctuation server to punctuate text.

## Docker

1. Clone this repository
2. cd into cloned folder and execute

    ```sh
    docker build . -t tgaudio2text
    ```

    It will take a while. make a tea while you wait.

3. Copy the example config file as `TGAudioToText.cfg` and modify it.

4. Create an empty `WTelegram.session`

5. Run docker container in interactive mode to login

    ```sh
    docker run \
    -v ./[your model name from config]:/app/[your model name from config] \
    -v ./TGAudioToText.cfg:/app/tgaudio/TGAudioToText.cfg \
    -v ./WTelegram_fux.session:/WTelegram.session \
    -it tgaudio2text sh -c "/app/tgaudio/TGAudioToText"
    ```

    When logged in, shutdown the container with ctrl + c

6. Run a container

    ```sh
    docker run \
    -v ./[your model name from config]:/app/[your model name from config] \
    -v ./TGAudioToText.cfg:/app/tgaudio/TGAudioToText.cfg \
    -v ./WTelegram.session:/WTelegram.session \
    -d --restart always tgaudio2text
    ```
