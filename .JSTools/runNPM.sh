#!/bin/bash
# cd `dirname "$0"`

export PATH="$PATH:$1"
npx npm ci
