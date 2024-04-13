#!/bin/bash

# Start the first process
python ./punctuation/punctuation-server.py &

# Start the second process
./tgaudio/TGAudioToText &

# Wait for any process to exit
wait -n

# Exit with status of process that exited first
exit $?
