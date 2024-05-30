const CopyPlugin = require('copy-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const path = require('path');

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
                    }

                    cb(null, data);
                }
            );
        });
    }
}

module.exports = env => ({ 
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
        path: env.buildTarget === 'web'
            ? path.join(__dirname, `../../../WebGLTemplates/CroquetLoader/`)
            : path.join(__dirname, `../../../StreamingAssets/${env.appName}/`),
        pathinfo: false,
        filename: env.buildTarget === 'node' ? 'node-main.js' : 'index-[contenthash:8].js',
        chunkFilename: 'chunk-[contenthash:8].js',
        clean: false
    },
    cache: {
        type: 'filesystem',
        name: `${env.appName}-${env.buildTarget}`,
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
    experiments: {
        asyncWebAssembly: true,
    },
    module: {
        rules: [
            {
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
        ],
    },
    plugins: [
        new CopyPlugin({
            patterns: [
                { from: `../../${env.appName}/scene-definitions.txt`, to: 'scene-definitions.txt', noErrorOnMissing: true }
            ]
        })].concat(env.buildTarget === 'node' ? [] : [
            new HtmlWebpackPlugin({
                template: './sources/index.html',
                filename: 'index.html',
                inject: false
            }),
            new CustomHtmlPlugin(),
        ]),
    externals: env.buildTarget !== 'node' ? [] : [
        {
            'utf-8-validate': 'commonjs utf-8-validate',
            bufferutil: 'commonjs bufferutil',
        },
    ],
    target: env.buildTarget || 'web',
    experiments: {
        outputModule: true
    }
});
