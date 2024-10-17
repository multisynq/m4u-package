import { StartSession } from '@multisynq/unity-js'
import { PluginsModelRoot, PluginsViewRoot } from './plugins/indexOfPlugins'
import { BUILD_IDENTIFIER } from './buildIdentifier'
StartSession(PluginsModelRoot, PluginsViewRoot, BUILD_IDENTIFIER)

/*
If the above is giving errors (because you are not using plugins), try one of these in place of it:

EITHER:  MyModelRoot importing code
-------------------------
import { StartSession, GameViewRoot } from "@multisynq/unity-js";
import { MyModelRoot } from "./Models";
StartSession(MyModelRoot, GameViewRoot);

OR no-op code:
-------------------------
import { StartSession, GameViewRoot, GameModelRoot } from "@multisynq/unity-js";
StartSession(GameModelRoot, GameViewRoot);

*/