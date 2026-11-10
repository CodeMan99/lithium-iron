FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0-noble

RUN apt-get -y update \
 && apt-get -y upgrade \
 && DEBIAN_FRONTEND=noninteractive apt-get -y install --no-install-recommends \
     bash-completion \
     ca-certificates \
     curl \
     dos2unix \
     file \
     fzf \
     inetutils-ping \
     moreutils \
     ripgrep \
 && rm -rf /var/lib/apt/lists/*
