#! /bin/bash

BUILDVERSION=${1:-./BuildVersion/BuildVersion.exe}

$BUILDVERSION \
        --verbose \
        --namespace org.herbal3d.transport \
        --version $(cat VERSION)
