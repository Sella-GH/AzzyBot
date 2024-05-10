#!/bin/bash

dotnet $DLL

handle_sigint() {
  echo "Caught SIGINT, shutting down..."
  kill -INT -$!
}

trap handle_sigint() SIGINT

done
