#!/bin/bash

# the CroquetBuilder.StartBuild script supplies us with 3 or 4 arguments:
# 1. full path to the platform-relevant node engine
# 2. app name - used in webpack.config to find the app source
# 3. build target ('node' or 'web' or 'webgl') - forwarded to webpack.config as buildTarget ('node'/'web') and useWebGL (true/false - only relevant for 'web')
# 4. full path to a temporary file to be used for watcher output (if not provided,
#    that means we should perform a one-time build)

# in preparation for packing, we set up either the webgl or non-webgl index.html

DIR=`dirname "$0"`
cd "$DIR"

NODE=$1
APPNAME=$2
TARGET=$3
if [ "$TARGET" == "webgl" ]; then
	TARGET="web"
	WEBGL="true"
	cp ./sources/index-webgl.html ./sources/index.html
else
	cp ./sources/index-webview_or_node.html ./sources/index.html
	WEBGL="false"
fi

# node_modules in the CroquetJS/.js-build folder, one above here
NODE_MODULES=../node_modules

if [ $# -eq 4 ]; then
	LOGFILE=$4
	"$NODE" $NODE_MODULES/.bin/webpack --config webpack.config.js --watch --mode development --env appName=$APPNAME --env buildTarget=$TARGET --no-color > $LOGFILE 2>&1 &

	# this output will be read by CroquetBuilder, to keep a record of the webpack process id
	echo "webpack=$!"
else
	"$NODE" $NODE_MODULES/.bin/webpack --config webpack.config.js --mode development --env appName=$APPNAME --env buildTarget=$TARGET --env useWebGL=$WEBGL --no-color

	echo "webpack-exit=$?"
fi
