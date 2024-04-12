FROM mcr.microsoft.com/dotnet/sdk:3.1 as build

WORKDIR /src
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet build . -o /app
RUN /app/TGAudioToText

FROM ubuntu:focal as base

ENV TZ=Asia/Yekaterinburg \
    DEBIAN_FRONTEND=noninteractive
ENV DOTNET_ROOT=/dotnet 
ENV PATH=$PATH:/dotnet

WORKDIR /tmp
RUN apt-get update
RUN apt-get install libicu66 git wget ffmpeg python3 python3-pip python-is-python3 -yq --no-install-recommends
RUN wget https://download.visualstudio.microsoft.com/download/pr/e89c4f00-5cbb-4810-897d-f5300165ee60/027ace0fdcfb834ae0a13469f0b1a4c8/dotnet-sdk-3.1.426-linux-x64.tar.gz
RUN mkdir -p /dotnet && tar zxf ./dotnet-sdk-3.1.426-linux-x64.tar.gz -C /dotnet && rm ./dotnet-sdk-3.1.426-linux-x64.tar.gz

COPY --from=build /app /app/tgaudio
WORKDIR /app/tgaudio

WORKDIR /app
COPY ./docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x ./docker-entrypoint.sh
    
COPY ./punctuation /app/punctuation
RUN pip3 install git+https://huggingface.co/kontur-ai/sbert_punc_case_ru
RUN python ./punctuation/punctuation-server-setup.py

RUN apt remove wget git -yq

CMD ["./docker-entrypoint.sh"]