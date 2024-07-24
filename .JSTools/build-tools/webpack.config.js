const CopyPlugin = require('copy-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const RemovePlugin = require('remove-files-webpack-plugin');
const webpack = require('webpack');


const path = require('path');
const fs = require('fs');

module.exports = env => {
const webGLPath = path.join(__dirname, `../../../WebGLTemplates/CroquetLoader/`);
const nonWebGLPath = path.join(__dirname, `../../../StreamingAssets/${env.appName}/`);
const destination = env.useWebGL === 'true' ? webGLPath : nonWebGLPath;

const lobbyDir = path.join(__dirname, `../../lobby`);
const withLobby = env.useWebGL === 'true' && fs.existsSync(lobbyDir);

// console.log(`Building for ${env.useWebGL==='true'?'WebGL': env.buildTarget}, ${withLobby?'with':'without'} lobby`);

return {
    // infrastructureLogging: {
    //     level: 'verbose',
    // },
    entry: () => {
        // if this is a node build, look for index-node.js in the app directory
        if (env.buildTarget === 'node') {
            try {
                const index = `../../${env.appName}/index-node.js`;
                require.resolve(index); // throws if not found
                return {index};
            } catch (e) {/* fall through and try index.js next */}
        }
        // otherwise (or if index-node not found) assume there is an index.js
        const index = `../../${env.appName}/index.js`;
        require.resolve(index);
        // if this is a WebGL build, and there is a lobby, use both
        if (withLobby) {
            try {
                const lobby = `../../lobby/lobby.js`;
                require.resolve(lobby);
                return { game: index, lobby };
            } catch (e) { /* fall through and use index.js */}
        }
        return {index};
    },
    output: {
        path: destination,
        pathinfo: false,
        filename: env.buildTarget === 'node' ? 'node-main.js' : '[name]-[contenthash:8].js',
        chunkFilename: 'chunk-[contenthash:8].js',
        clean: !env.useWebGL // RemovePlugin below handles index-####.js files in WebGL
    },
    cache: {
        type: 'filesystem',
        name: `${env.appName}-${env.buildTarget}${env.useWebGL?'-WebGL':''}`,
        buildDependencies: {
            config: [__filename],
        }
    },
    resolve: {
        modules: [path.resolve(__dirname, '../node_modules')],
        alias: {
            '@croquet/game-models$': path.resolve(__dirname, 'sources/game-support-models.js'),
            '@croquet/unity-bridge$': path.resolve(__dirname, 'sources/unity-bridge.js'),
        },
        fallback: {
            "crypto": false,
            ...(env.useWebGL === 'true' ? {
                "buffer": require.resolve("buffer/"),
                "stream": require.resolve("stream-browserify"),
                "assert": require.resolve("assert/"),
            } : {})
        }
    },
    module: {
        rules: [
            env.useWebGL==='true' && {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: {
                        presets: ['@babel/preset-env'],
                        plugins: ['@babel/plugin-transform-modules-commonjs']
                    }
                }
            },
            {
                test: /\.js$/,
                enforce: "pre",
                use: ["source-map-loader"],
            },
        ].filter(x=>x), // removes any undefined by the && predicate being false
    },
    plugins: [
        env.useWebGL === 'true' && new RemovePlugin({
            before: {
                allowRootAndOutside: true,
                test: [
                    {
                        // remove app-specific content from StreamingAssets
                        folder: nonWebGLPath,
                        method: _absolutePath => true,
                        recursive: true
                    },
                    {
                        // in build dir, remove old hashed .js files and their meta files
                        folder: webGLPath,
                        method: absolutePath => /(index|lobby|main|game)-.+\.js/m.test(absolutePath)
                            || (!withLobby && /game.html/m.test(absolutePath)),
                    }
                ],
                log: false
            }
        }),
        new CopyPlugin({
            patterns: [
                {
                    from: `../../${env.appName}/scene-definitions.txt`,
                    to: `${destination}/scene-definitions.txt`,
                    noErrorOnMissing: true
                }
            ]
        }),
        // build main html if not building for node
        env.buildTarget !== 'node' && new HtmlWebpackPlugin({
            template: './sources/index.html',
            filename: withLobby ? 'game.html' : 'index.html',
            chunks: ['index', 'game'], // main chunk is either index or game
            inject: 'body',
        }),
        withLobby && new HtmlWebpackPlugin({ // adds lobby.html to the build
            template: `${lobbyDir}/lobby.html`,
            filename: 'index.html',
            inject: 'body',
            chunks: ['lobby'],
        }),
        env.useWebGL === 'true' && new webpack.ProvidePlugin({
            Buffer: ['buffer', 'Buffer'],
        }),
        env.useWebGL === 'true' && new webpack.ProvidePlugin({
            process: 'process/browser',
        }),
    ].filter(x=>x), // removes any undefined by the && predicate being false
    externals: env.buildTarget !== 'node' ? [] : [
        {
            'utf-8-validate': 'commonjs utf-8-validate',
            bufferutil: 'commonjs bufferutil',
        },
    ],
    target: env.buildTarget,
    experiments: {
        outputModule: env.useWebGL==='true',
        asyncWebAssembly: true,
    }
};
};
