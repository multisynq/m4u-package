:: @echo off
:: setlocal enabledelayedexpansion

:: CroquetBuilder.StartBuild script supplies us with 3 or 4 arguments:
:: 1. full path to the node engine
:: 2. app name - used in webpack.config to find the app source
:: 3. build target ('node' or 'webview' or 'webgl') also used in webpack.config
:: 4. full path to a temporary file to be used for watcher output (optional)

set nodepath=%1
set appname=%2
set target=%3

:: Set NODE_MODULES path
set NODE_MODULES=..\node_modules

:: Check if a logfile path is provided (4th argument)
if "%4" neq "" (
    set logfile=%4
    start /B %nodepath% %NODE_MODULES%\.bin\webpack --config webpack.config.js --watch --mode development --env appName=%appname% --env buildTarget=%target% --no-color > %logfile% 2>&1

    :: Get the Process ID of the started webpack process
    for /f "tokens=2 delims=," %%a in ('tasklist /fi "imagename eq node.exe" /fo csv /nh') do (
        set pid=%%a
        goto :break
    )
    :break

    :: Output the webpack process ID
    echo webpack=!pid!
) else (
    %nodepath% %NODE_MODULES%\.bin\webpack --config webpack.config.js --mode development --env appName=%appname% --env buildTarget=%target% --no-color

    :: Output the exit code
    echo webpack-exit=%errorlevel%
)

:: endlocal