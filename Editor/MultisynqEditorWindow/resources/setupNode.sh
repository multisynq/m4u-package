#!/bin/bash

# Path to node on a MacOS:
# usr/local/bin/node

echo "(which node)      = $(which node)"

echo "EXIT=1"

# if [ -x "$(command -v node)" ]; then
#   echo "Great news! Node is already installed!"
#   echo $(which node)
#   echo "EXIT=0"
#   exit 0
# else
#   echo "Could not find node... Please install it and then run me again!"
#   echo "EXIT=1"
#   exit 1
# fi


# if [ -x "$(command -v node)" ]; then
#   echo $(which node)
# else
#   echo "Could not find node... We'll install it for you!"

#   echo "Downloading and Installing nvm (Node Version Manager)"
#   curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.7/install.sh | bash

#   echo "Downloading and Installing Node v20"
#   nvm install 20

#   # Wire up nvm to the current shell so we can use it without having to restart
#   export NVM_DIR="$HOME/.nvm"
#   [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"  # This loads nvm
#   [ -s "$NVM_DIR/bash_completion" ] && \. "$NVM_DIR/bash_completion"  # This loads nvm bash_completion

#   # Verifies the right Node.js version is in the environment
#   echo "Verifying Node v20 is installed"
#   node -v # should print `v20.14.0`

#   # verifies the right NPM version is in the environment
#   echo "Verifying NPM is installed"
#   npm -v # should print `10.7.0`

#   echo "Node and NPM should now be installed!"
#   echo $(which node)
# fi
