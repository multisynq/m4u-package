import { StartSession } from '@multisynq/m4u-package'
import { PluginsModelRoot, PluginsViewRoot } from './plugins/indexOfPlugins'
import { BUILD_IDENTIFIER } from './buildIdentifier'
StartSession(PluginsModelRoot, PluginsViewRoot, BUILD_IDENTIFIER)

/*
If the above is giving errors (because you are not using plugins), try one of these in place of it:

EITHER:  MyModelRoot importing code
-------------------------
import { StartSession, GameViewRoot } from "@multisynq/m4u-package";
import { MyModelRoot } from "./Models";
StartSession(MyModelRoot, GameViewRoot);

OR no-op code:
-------------------------
import { StartSession, GameViewRoot, GameModelRoot } from "@multisynq/m4u-package";
StartSession(GameModelRoot, GameViewRoot);

*/