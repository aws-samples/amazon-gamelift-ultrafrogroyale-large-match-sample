# amazon-gamelift-largematch-sample-ultrafrogroyale

A small game built with Unity to demonstrate how to use the new Amazon GameLift large match features.

## Requirements

- An AWS account: <https://aws.amazon.com/getting-started/>
- Unity LTS Release 2018.4 (will not work on higher versions): <https://unity.com/>
- Amazon GameLift Server SDK: <https://aws.amazon.com/gamelift/getting-started/>
- AWS Mobile SDK for Unity: <https://docs.aws.amazon.com/mobile/sdkforunity/developerguide/what-is-unity-plugin.html>

### Contents

``` html
├── AWS                     # Lambda functions, IAM policies, rulesets, etc.
└── UltraFrogRoyale         # The root of the Unity project
    ├── Assets              # Editable assets, source files
    │   ├── Scenes          # Unity scene definition files
    │   ├── Prefabs         # Player prefab for networking
    │   └── Textures        # Images used by the game
    ├── Packages            # Unity packages folder
    └── ProjectSettings     # Unity project folder
```

## Building and using the sample

### Step 1: Build the Unity project

1. Open the project in Unity
2. Build and add the Amazon GameLift Server SDK to the project following the instructions here (specifically for .Net 4.5): <https://docs.aws.amazon.com/gamelift/latest/developerguide/integration-engines-unity-using.html>
3. Add the AWS Mobile SDK for Unity to the project. <https://docs.aws.amazon.com/mobile/sdkforunity/developerguide/what-is-unity-plugin.html>
4. Create a client and server build from Unity. A server build can be created by selecting "Server Build" from the Build Settings dialog.

### Step 2: Upload server build to GameLift

1. Make sure you have the latest AWS command line installed.
2. Upload the server build to GameLift
    - Use us-east-1 region as this is hardcoded in the demo
    - Example command:

``` html
aws gamelift upload-build --operating-system WINDOWS_2012 --build-root "C:\amazon-gamelift-largematch-sample-ultrafrogroyale\UltraFrogRoyale\ServerBuild" --name "UltraFrogRoyale" --build-version "build 1" --region us-east-1
```


### Step 3: Prepare GameLift

1. Create a new fleet
    - Use us-east-1 region as this is hardcoded in the demo
    - Select the build uploaded in step 2.
    - c5.large works well and is in the free tier
    - Fleet type: On-Demand
    - Binary type: Build
    - Set the launch configuration to call "UltraFrogBattleRoyale.exe" with 1 concurrent process
    - Add port range 7000-8000, protocol UDP, IP address range 0.0.0.0/0
    - Add port range 7000-8000, protocol TCP, IP address range 0.0.0.0/0
    - Don't set a scaling policy on the fleet
2. Create a game session placement queue
    - Use us-east-1 region as this is hardcoded in the demo
    - Adding the fleet just created as the only destination.
3. Create matchmaking ruleset using the file AWS/UltraFrogRoyaleFreeForAll-debug_MatchmakingRuleSet.json
    - Use us-east-1 region as this is hardcoded in the demo
4. Create matchmaking configuration
    - Use us-east-1 region as this is hardcoded in the demo
    - Matchmaking configuration must be named "UltraFrogRoyaleMatching" so the Lambda can invoke it
    - Use the ruleset and queues you just created
    - Ensure "acceptance required" is set to "no"

### Step 4: Create client service

Refer to the instructions found in step 2 of the article <https://aws.amazon.com/blogs/gametech/creating-servers-for-multiplayer-mobile-games-with-amazon-gamelift/> with the following differences:
    * Call the Lambda ConnectUltraFrogRoyaleClient
    * Use Node.js 8, the default is 10, but it doesn't support UUID.
    * Set the Lambda IAM role using the rules found in AWS/ConnectUltraFrogRoyaleClient_LambdaIAMRole.json (this differs from step 15-17, you can skip the action editor and just paste in the json)
    * Use the Lambda source code found in AWS/ConnectUltraFrogRoyaleClient_Lambda.js

### Step 5: Run the game

At this point, you'll be able to run the game client and hit the "Start A Match" button. Note that the way the matchmaking rules are configured, you'll need to connect at least 3 clients before you get a match. You can run these clients on the same machine.

## For more information or questions

- The steps in this file are condensed from the article found here: <https://aws.amazon.com/blogs/gametech/creating-a-battle-royale-game-using-unity-and-amazon-gamelift/>
- Please contact gametech@amazon.com for any comments or requests regarding this content

## License Summary

This sample code is made available under the Apache-2.0 license. See the LICENSE file.
