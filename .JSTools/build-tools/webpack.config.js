const CopyPlugin = require('copy-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const RemovePlugin = require('remove-files-webpack-plugin');

const path = require('path');
const fs = require('fs');

class CustomHtmlPlugin {
    apply(compiler) {
        compiler.hooks.compilation.tap('CustomHtmlPlugin', (compilation) => {
            HtmlWebpackPlugin.getHooks(compilation).beforeEmit.tapAsync(
                'CustomHtmlPlugin',
                (data, cb) => {
                    // Get the actual filename from the generated assets
                    const generatedScripts = Object.keys(compilation.assets).filter(asset => asset.endsWith('.js'));
                    const indexScript = generatedScripts.find(asset => asset.startsWith('index-') && asset.endsWith('.js'));

                    if (indexScript) {
                        const scriptTag = `additionalScript.src = "${indexScript}";`;
                        data.html = data.html.replace('additionalScript.src = "index-[contenthash:8].js";', scriptTag);
                        const outPath = path.join(__dirname, `../../../WebGLTemplates/CroquetLoader/`);
                        fs.writeFileSync(path.join(outPath, 'index.html'), data.html);
                    }
                    cb(null, data);
                }
            );
        });
    }
}

module.exports = env => {
const webGLPath = path.join(__dirname, `../../../WebGLTemplates/CroquetLoader/`);
const nonWebGLPath = path.join(__dirname, `../../../StreamingAssets/${env.appName}/`);
const destination = env.useWebGL === 'true' ? webGLPath : nonWebGLPath;

return {
    // infrastructureLogging: {
    //     level: 'verbose',
    // },
    entry: () => {
        // if this is a node build, look for index-node.js in the app directory
        if (env.buildTarget === 'node') {
            try {
                const index = `../../${env.appName}/index-node.js`;
                require.resolve(index);
                return index;
            } catch (e) {/* fall through and try index.js next */}
        }
        // otherwise (or if index-node not found) assume there is an index.js
        const index = `../../${env.appName}/index.js`;
        require.resolve(index);
        return index;
    },
    output: {
        path: destination,
        pathinfo: false,
        filename: env.buildTarget === 'node' ? 'node-main.js' : 'index-[contenthash:8].js',
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
        fallback: { "crypto": false }
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
                        // in build dir, remove old hashed index.js files and their maps
                        folder: webGLPath,
                        method: absolutePath => new RegExp(/index-.+\.js/, 'm').test(absolutePath)
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
        env.buildTarget !== 'node' && new HtmlWebpackPlugin({
            template: './sources/index.html',
            filename: 'index.html',
            inject: env.useWebGL !== 'true'
        }),
        env.useWebGL === 'true' && new CustomHtmlPlugin(), // fills in the line that loads index-[hash].js
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
