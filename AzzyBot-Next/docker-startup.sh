#!/bin/bash

dotnet $DLL

trap "exit" SIGINT

wait $!
