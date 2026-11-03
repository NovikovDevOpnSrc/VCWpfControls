#!/bin/bash
if [ "$1" = "get" ]; then
  printf "protocol=https\nhost=github.com\n\n" | git credential-manager get
fi
