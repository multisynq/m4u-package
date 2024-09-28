import { StartSession } from '@croquet/unity-bridge'
import { PluginsModelRoot, PluginsViewRoot } from './plugins/indexOfPlugins'
import { BUILD_IDENTIFIER } from './buildIdentifier'
StartSession(PluginsModelRoot, PluginsViewRoot, BUILD_IDENTIFIER)

/*
If the above is giving errors (because you are not using plugins), try one of these in place of it:

EITHER:  MyModelRoot importing code
-------------------------
import { StartSession, GameViewRoot } from "@croquet/unity-bridge";
import { MyModelRoot } from "./Models";
StartSession(MyModelRoot, GameViewRoot);

OR no-op code:
-------------------------
import { StartSession, GameViewRoot } from "@croquet/unity-bridge";
import { GameModelRoot } from "@croquet/game-models";
StartSession(GameModelRoot, GameViewRoot);

*/