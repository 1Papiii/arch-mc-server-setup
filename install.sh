#!/usr/bin/env bash
# ╔═══════════════════════════════════════════════════════════════════════╗
# ║        ARCH MC SERVER SETUP (C#) - INSTALLER                          ║
# ║        github.com/1Papiii/arch-mc-server-setup                        ║
# ╚═══════════════════════════════════════════════════════════════════════╝

set -euo pipefail

R="\033[0m"
G="\033[32m"
C="\033[36m"
RED="\033[31m"

clear
printf "${C}══ Downloading & Installing Minecraft Server Setup ══${R}\n\n"

if [ "$EUID" -ne 0 ]; then
  printf "${RED}[FATAL]${R} Please run as root: sudo bash install_setup.sh\n"
  exit 1
fi
printf "${G}[INFO]${R} Installing .NET 10 and git via pacman...\n"

pacman -Sy --noconfirm dotnet-runtime dotnet-sdk git

INSTALL_DIR="/opt/arch-mc-server-setup"
printf "\n${G}[INFO]${R} Cloning repository into ${INSTALL_DIR}...\n"

if [ -d "$INSTALL_DIR" ]; then
    printf "${RED}[WARN]${R} Directory already exists. Cleaning up old files...\n"
    rm -rf "$INSTALL_DIR"
fi

git clone https://github.com/1Papiii/arch-mc-server-setup.git "$INSTALL_DIR"

chown -R "${SUDO_USER:-root}:${SUDO_USER:-root}" "$INSTALL_DIR"
cd "$INSTALL_DIR"

printf "\n${G}████████████████████████████████████████████████████████████████${R}\n"
printf "${G}[ OK ] INSTALLATION COMPLETE!${R}\n"
