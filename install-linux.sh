#!/bin/bash

# Requires connectivity to:
# http://azure.archive.ubuntu.com
sudo apt install unzip

sudo mkdir -p /usr/local/bin

# Requires connectivity to:
# https://github.com/
# https://objects.githubusercontent.com/
wget "https://github.com/JanneMattila/webapp-network-tester/releases/download/v1.0.0/webappnetworktester-linux.zip" -O /tmp/webappnetworktester-linux.zip
unzip /tmp/webappnetworktester-linux.zip -d /tmp/webappnetworktester-linux
mv /tmp/webappnetworktester-linux/* /usr/local/bin/

rm -rf /tmp/webappnetworktester-linux.zip /tmp/webappnetworktester-linux

# Create as a service
sudo bash -c 'cat > /etc/systemd/system/webappnetworktester.service' << EOF
[Unit]
Description=WebApp Network Tester
After=network.target

[Service]
Type=simple
ExecStart=/usr/local/bin/webappnetworktester --urls http://*:80
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable webappnetworktester
sudo systemctl start webappnetworktester
